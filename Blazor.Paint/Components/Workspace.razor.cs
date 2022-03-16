using Blazor.Canvas;
using Blazor.Canvas.Contexts;
using Blazor.Canvas.Extensions;
using Blazor.Paint.Core;
using Blazor.Paint.Core.Enums;
using Blazor.Paint.Core.Records;
using Blazor.Paint.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Collections.Immutable;
using System.Drawing;
using System.Xml;
using System.Xml.Serialization;

namespace Blazor.Paint.Components;

public partial class Workspace : ComponentBase
{
	#region Properties / Fields

	private const int UNDO_STACK_SIZE = 16;

	[Inject] protected IJSRuntime JS { get; set; } = null!;

	/// <summary>
	/// Currently modified document.
	/// </summary>
	[Parameter, EditorRequired] public Document Document { get; init; } = null!;

	public Size CanvasSize => Document.CanvasSize;

	/// <summary>
	/// A collection of layer information entities, which are used to create <see cref="Layer"/> components, which are then rendered by blazor.
	/// <para><see cref="LayerInfo"/> entities cannot be rendered on their own and are only used to cache layer data (such as pixels) when layers are re-rendered due to state changes.</para>
	/// </summary>
	public List<LayerInfo> LayerInfos => Document.LayerInfos;

	/// <summary>
	/// A collection of references of rendered <see cref="Layer"/> components.
	/// </summary>
	public List<Layer> RenderedLayers { get; private set; } = new();

	/// <summary>
	/// A collection of active / selected <see cref="Layer"/> component references.
	/// </summary>
	public IEnumerable<Layer> ActiveLayers { get; private set; } = new List<Layer>();

	/// <summary>
	/// A collection of information and properties of active / selected layers.
	/// </summary>
	public IEnumerable<LayerInfo> ActiveLayerInfos
		=> LayerInfos.Where(layerInfo
			=> ActiveLayers.Any(activeLayer
				=> activeLayer.LayerId == layerInfo.LayerId));

	public int ActiveLayerOpacity
	{
		get
		{
			LayerInfo? activeLayerInfo = ActiveLayerInfos.FirstOrDefault();
			return activeLayerInfo is null ? 100 : activeLayerInfo.Opacity;
		}
		set
		{
			LayerInfo? activeLayerInfo = ActiveLayerInfos.FirstOrDefault();

			if (activeLayerInfo is null)
				return;

			activeLayerInfo.Opacity = value;
		}
	}

	/// <summary>
	/// Currently used tool. (e.g. 'Transform', 'Select', 'Brush', 'Eraser' etc.)
	/// </summary>
	public ToolType ActiveTool { get; private set; }

	/// <summary>
	/// HTML value of the primary color. (e.g. '#FF0000' or 'red')
	/// </summary>
	public string PrimaryColor { get; set; } = "#000000";

	/// <summary>
	/// HTML value of the secondary color. (e.g. '#0000FF' or 'blue')
	/// </summary>
	public string SecondaryColor { get; set; } = "#FFFFFF";

	/// <summary>
	/// HTML value of the color which is used for the current pointer action. This property changes depending on which button was pressed (left or right).
	/// </summary>
	public string CurrentColor { get; set; } = "#000000";

	/// <summary>
	/// Width of the stroke which is used for tools like 'Brush', 'Eraser', 'Line', 'Rectangle' etc.
	/// </summary>
	public int StrokeWidth { get; set; } = 12;

	public int BrushOpacity { get; set; } = 100;

	public LineCap LineCap { get; set; }

	public ShapeDrawingMode ShapeDrawingMode { get; set; }

	/// <summary>
	/// Clipboard stack of copied image data from active layers.
	/// </summary>
	public Stack<ImageSnapshot> Clipboard { get; private set; } = new();

	public List<WorkspaceAction> UndoStack { get; private set; } = new();
	public List<WorkspaceAction> RedoStack { get; private set; } = new();

	/// <summary>
	/// Current CSS cursor style, which changes depending on the currently set <c><see cref="ActiveTool"/></c>
	/// </summary>
	public string CursorStyle
	{
		get
		{
			return ActiveTool switch
			{
				ToolType.Transform => _transformationCursorStyle,

				ToolType.BoxSelection
					or ToolType.PaintBucket
					or ToolType.Line
					or ToolType.Rectangle
					or ToolType.Ellipse => "crosshair",

				_ => "none",
			};
		}
	}

	/// <summary>
	/// Bounds relative to the entire HTML page.
	/// </summary>
	public Rectangle Bounds { get; private set; }

	/// <summary>
	/// Editing area bounds relative to the entire HTML page.
	/// </summary>
	public Rectangle EditingAreaBounds { get; private set; }

	/// <summary>
	/// Selection bounds.
	/// </summary>
	public Rectangle Selection { get; private set; }

	/// <summary>
	/// Determines whether there's a pointer action being performed. (such as using the 'Brush' or 'Select' tool)
	/// </summary>
	public bool IsPerformingPointerAction { get; private set; }

	private bool _isInTransformActionPreviewMode;

	/// <summary>
	/// Determines whether the currently transformed selection is still a preview (not applied yet) or has already been applied (dry pixels).
	/// </summary>
	public bool IsInTransformActionPreviewMode
	{
		get
		{
			if (ActiveTool is not ToolType.Transform)
				_isInTransformActionPreviewMode = false;

			return _isInTransformActionPreviewMode;
		}
		private set => _isInTransformActionPreviewMode = value;
	}

	/* ------------------ */
	/* Visible Components */
	/* ------------------ */

	/// <summary>
	/// A canvas which renders the cursor for previewing stroke widths.
	/// </summary>
	private BlazorCanvas? cursorCanvas;

	/// <summary>
	/// A canvas which renders the transformation anchors.
	/// </summary>
	private BlazorCanvas? transformationAnchorsCanvas;

	/// <summary>
	/// A canvas which renders the current selection shape. (e.g. Box Selection rectangle)
	/// </summary>
	private BlazorCanvas? selectionPreviewCanvas;

	/// <summary>
	/// Root <c>&lt;div&gt;</c> reference of the entire <see cref="Workspace"/> component.
	/// </summary>
	private ElementReference? rootDiv;

	/// <summary>
	/// <c>&lt;div&gt;</c> reference of the editing area.
	/// </summary>
	private ElementReference? editingAreaDiv;

	/* -------------------- */
	/* Invisible components */
	/* -------------------- */

	/// <summary>
	/// An invisible component used to convert and process HTML/JS image data. (e.g. converting from CanvasData to DataUrl and vice versa)
	/// </summary>
	private ImageProcessor? imageProcessor;

	/* -------- */
	/* Wrappers */
	/* -------- */

	/// <summary>
	/// A wrapper used to add <see cref="Layer"/> component references into the <c><see cref="RenderedLayers"/></c> list.
	/// </summary>
	private Layer? LayerReferenceWrapper
	{
		set
		{
			if (value is not null)
				RenderedLayers.Add(value);
		}
	}

	/* ------------ */
	/* Other fields */
	/* ------------ */

	private Point _lastClickLocation;
	private Point _lastPointerLocation;

	public TransformationAnchor _transformationAnchor;
	private string _transformationCursorStyle = "auto";

	private readonly Lazy<Task<Context2D>> cursorCanvasContextTask;
	private readonly Lazy<Task<Context2D>> transformationAnchorsCanvasContextTask;
	private readonly Lazy<Task<Context2D>> selectionPreviewCanvasContextTask;

	#endregion Properties / Fields

	public Workspace()
	{
		cursorCanvasContextTask = new(() => cursorCanvas!.GetContext2DAsync().AsTask());
		transformationAnchorsCanvasContextTask = new(() => transformationAnchorsCanvas!.GetContext2DAsync().AsTask());
		selectionPreviewCanvasContextTask = new(() => selectionPreviewCanvas!.GetContext2DAsync().AsTask());
	}

	#region Event handling methods

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && LayerInfos.Count > 0)
			await PerformInitializeDocumentAsync();

		await base.OnAfterRenderAsync(firstRender);
	}

	private async Task UpdateBoundsAsync()
	{
		BoundingClientRect rootRect = await JS.InvokeAsync<BoundingClientRect>("getBoundingClientRect", rootDiv);
		BoundingClientRect editingAreaRect = await JS.InvokeAsync<BoundingClientRect>("getBoundingClientRect", editingAreaDiv);

		double scrollWidth = await JS.InvokeAsync<double>("getScrollWidth", rootDiv);
		double scrollHeight = await JS.InvokeAsync<double>("getScrollHeight", rootDiv);

		Bounds = new Rectangle((int)rootRect.X, (int)rootRect.Y, (int)scrollWidth, (int)scrollHeight);
		EditingAreaBounds = editingAreaRect.GetBounds();
	}

	private async Task EditingArea_MouseDownAsync(MouseEventArgs e)
	{
		if (ActiveLayerInfos.Any())
		{
			_lastClickLocation = _lastPointerLocation = GetAbsoluteEditingAreaPointerLocation(e);

			const int MOUSEBUTTON_RIGHT = 2;
			CurrentColor = e.Button switch { MOUSEBUTTON_RIGHT => SecondaryColor, _ => PrimaryColor };

			switch (ActiveTool)
			{
				case ToolType.PaintBucket: await PerformPaintBucketAsync(); break;
				default: await StartPointerActionSequenceAsync(_lastClickLocation); break;
			}
		}
	}

	private async Task EditingArea_MouseMoveAsync(MouseEventArgs e)
	{
		var pointerLocation = new Point((int)e.OffsetX, (int)e.OffsetY);
		await DrawCursorAsync(pointerLocation);

		Point absolutePointerLocation = GetAbsoluteEditingAreaPointerLocation(e);

		if (IsPerformingPointerAction)
		{
			await DoPointerActionSequenceAsync(absolutePointerLocation);
			_lastPointerLocation = absolutePointerLocation;
		}
		else
		{
			_transformationAnchor = Utils.GetTransformationAnchorFromPoint(Selection, absolutePointerLocation);
			_transformationCursorStyle = Utils.GetTransformationCursorStyle(_transformationAnchor);
		}
	}

	private async Task EditingArea_MouseUpAsync(MouseEventArgs e)
	{
		if (IsPerformingPointerAction)
		{
			Point releaseLocation = GetAbsoluteEditingAreaPointerLocation(e);
			await FinalizePointerActionSequenceAsync(releaseLocation);
		}
	}

	#endregion Event handling methods

	#region Actions (Does NOT yield undo-able action)

	/* --------------- */
	/* General actions */
	/* --------------- */

	public async Task SetActiveToolAsync(ToolType tool)
	{
		await ApplyTransformationAsync();
		ActiveTool = tool;
	}

	public async Task ResizeCanvasAsync(Size newSize, CanvasAnchor canvasAnchor)
	{
		await RefreshLayerInfosAsync();
		LayerInfos.ForEach(layerInfo => layerInfo.ResizeCanvas(newSize, canvasAnchor));
		ForceLayersToRerender();
	}

	/* ------------- */
	/* Layer actions */
	/* ------------- */

	public void SetActiveLayers(params Guid[] layerIds)
		=> ActiveLayers = RenderedLayers.Where(layer => layerIds.Contains(layer.LayerId));

	public async Task InsertLayersAsync(int startIndex, params LayerInfo[] layerInfos)
	{
		await RefreshLayerInfosAsync();

		foreach (LayerInfo layerInfo in layerInfos)
		{
			layerInfo.VisibilityChanged += delegate { StateHasChanged(); };
			LayerInfos.Insert(startIndex, layerInfo);
		}

		ForceLayersToRerender();

		SetActiveLayers(layerInfos.Select(x => x.LayerId).ToArray());
	}

	public async Task RemoveLayersAsync(params Guid[] layerIds)
	{
		await RefreshLayerInfosAsync();

		LayerInfos.RemoveAll(x => layerIds.Contains(x.LayerId));

		ForceLayersToRerender();
	}

	public async Task FillLayerAsync(Guid layerId, string fillColor)
	{
		Layer? targetLayer = RenderedLayers.Find(layer => layer.LayerId == layerId);

		if (targetLayer is null)
			return;

		await targetLayer.FillCanvasAsync(fillColor);
	}

	/* ----------------- */
	/* Selection actions */
	/* ----------------- */

	public async Task SelectAsync(Point selectionStart, Size selectionSize)
		=> await SelectAsync(new Rectangle(selectionStart, selectionSize));

	public async Task SelectAsync(Rectangle selection)
	{
		Selection = selection;

		if (selectionPreviewCanvas is null)
			return;

		Context2D selectionPreviewCanvasContext = await selectionPreviewCanvasContextTask.Value;
		await selectionPreviewCanvasContext.ClearAllAsync();

		if (transformationAnchorsCanvas is null)
			return;

		Context2D transformationAnchorsCanvasContext = await transformationAnchorsCanvasContextTask.Value;
		await transformationAnchorsCanvasContext.ClearAllAsync();

		if (Selection.Width == 0 || Selection.Height == 0)
			return;

		await selectionPreviewCanvasContext.DrawSelectionRectangleAsync(Selection);

		if (IsInTransformActionPreviewMode)
		{
			Rectangle relativeSelection = GetSelectionRelativeToRoot();
			await transformationAnchorsCanvasContext.DrawTransformationAnchorsAsync(relativeSelection);
		}
	}

	public async Task SelectAllAsync()
	{
		IsInTransformActionPreviewMode = false;
		await SelectAsync(new Point(0, 0), CanvasSize);
	}

	public async Task ClearSelectionAsync()
	{
		IsInTransformActionPreviewMode = false;
		await SelectAsync(new Point(0, 0), new Size(0, 0));
	}

	public async Task StartTransformationAsync(string dataUrl)
	{
		Layer? topLayer = ActiveLayers.FirstOrDefault();

		if (topLayer is null)
			return;

		topLayer.CachedSelectionDataUrl = dataUrl;
		string variableName = await imageProcessor!.UrlToImgElementAsync(topLayer.CachedSelectionDataUrl);

		double width = await JS.InvokeAsync<double>("getImageWidth", variableName);
		double height = await JS.InvokeAsync<double>("getImageHeight", variableName);

		var selection = new Rectangle(Point.Empty, new Size((int)width, (int)height));

		ActiveTool = ToolType.Transform;
		IsInTransformActionPreviewMode = true;
		await SelectAsync(selection);

		await topLayer.PreviewTransformSelection(variableName, selection.Location, selection.Size);
	}

	public async Task ApplyTransformationAsync()
	{
		await ActiveLayers.Batch_ApplyTransformationAsync(Selection.Location, Selection.Size);

		IsInTransformActionPreviewMode = false;
		await SelectAsync(Selection);
	}

	/* ------------------ */
	/* Cut / Copy / Paste */
	/* ------------------ */

	public async Task CutAsync()
	{
		await CopyAsync();

		foreach (Layer layer in ActiveLayers.OrderBy(x => x.ZIndex))
		{
			if (!layer.IsValidForEditing)
				continue;

			Context2D context = await layer.GetDrawingCanvasContext2D();
			await context.ClearRectAsync(Selection);
		}
	}

	public async Task CopyAsync()
	{
		if (imageProcessor is null)
			return;

		Clipboard.Clear();

		foreach (Layer layer in ActiveLayers.OrderBy(x => x.ZIndex))
		{
			if (!layer.IsValidForEditing)
				continue;

			Context2D layer2DContext = await layer.GetDrawingCanvasContext2D();
			ImageData imageData = await layer2DContext.GetImageDataAsync(Selection);
			string dataUrl = await imageProcessor.ImageDataToUrlAsync(imageData);

			var item = new ImageSnapshot(dataUrl, Selection.Size, Selection.Location);
			Clipboard.Push(item);
		}
	}

	public async Task PasteAsync()
	{
		var newLayers = new List<LayerInfo>();

		foreach (ImageSnapshot imageData in Clipboard.Reverse<ImageSnapshot>())
			newLayers.Add(new LayerInfo(
				$"Layer {LayerInfos.Count + newLayers.Count + 1}",
				CanvasSize, imageData));

		await InsertLayersAsync(0, newLayers.ToArray());

		ImageSnapshot topElement = Clipboard.Peek();
		await SelectAsync(topElement.CanvasPlacement, topElement.Size);
	}

	#endregion Actions (Does NOT yield undo-able action)

	#region Perform... (Undo-able actions)

	public async Task PerformResizeCanvasAsync(Size newSize, CanvasAnchor canvasAnchor)
	{
		await ResizeCanvasAsync(newSize, canvasAnchor);

		string actionLabel = canvasAnchor is CanvasAnchor.Stretched ? "Resize Document" : "Resize Canvas";
		await RegisterUndoableActionAsync(actionLabel);
	}

	/* ------------- */
	/* Layer actions */
	/* ------------- */

	public async Task PerformSetActiveLayersAsync(params Guid[] layerIds)
	{
		SetActiveLayers(layerIds);
		await RegisterUndoableActionAsync("Change Active Layers");
	}

	public async Task PerformRemoveSelectedLayersAsync()
	{
		await RemoveLayersAsync(ActiveLayerInfos.Select(layerInfo => layerInfo.LayerId).ToArray());
		await RegisterUndoableActionAsync("Remove Selected Layers");
	}

	public async Task PerformRenameLayerAsync(Guid layerId, string newName)
	{
		await RefreshLayerInfosAsync();

		LayerInfo? target = LayerInfos.Find(x => x.LayerId == layerId);

		if (target is not null)
		{
			target.Name = newName;
			await RegisterUndoableActionAsync("Rename Layer");
		}

		ForceLayersToRerender();
	}

	public async Task PerformRenameSelectedLayerAsync(string newName)
	{
		Layer? firstSelected = ActiveLayers.FirstOrDefault();

		if (firstSelected is null || firstSelected.ZIndex + 1 >= RenderedLayers.Count)
			return;

		await PerformRenameLayerAsync(firstSelected.LayerId, newName);
	}

	public async Task PerformMoveLayerAsync(Guid layerId, int newIndex)
	{
		await RefreshLayerInfosAsync();

		LayerInfo? target = LayerInfos.Find(x => x.LayerId == layerId);

		if (target is not null)
		{
			LayerInfos.Remove(target);
			LayerInfos.Insert(LayerInfos.Count - newIndex, target);

			await RegisterUndoableActionAsync("Change Layer Order");
		}

		ForceLayersToRerender();
	}

	public async Task PerformMoveSelectedLayerUpAsync()
	{
		Layer? firstSelected = ActiveLayers.FirstOrDefault();

		if (firstSelected is null || firstSelected.ZIndex + 1 >= RenderedLayers.Count)
			return;

		await PerformMoveLayerAsync(firstSelected.LayerId, firstSelected.ZIndex + 1);
	}

	public async Task PerformMoveSelectedLayerDownAsync()
	{
		Layer? firstSelected = ActiveLayers.FirstOrDefault();

		if (firstSelected is null || firstSelected.ZIndex - 1 < 0)
			return;

		await PerformMoveLayerAsync(firstSelected.LayerId, firstSelected.ZIndex - 1);
	}

	public async Task PerformMergeLayersAsync(params Guid[] layerIds)
	{
		if (imageProcessor is null)
			return;

		await RefreshLayerInfosAsync();

		IEnumerable<Layer> matchingLayers = RenderedLayers
			.Where(x => layerIds.Contains(x.LayerId));

		if (!matchingLayers.Any())
			return;

		Layer root = matchingLayers.First();

		if (root.DrawingCanvas is null)
			return;

		Context2D root2DContext = await root.GetDrawingCanvasContext2D();

		foreach (Layer layer in matchingLayers.Skip(1))
		{
			if (layer.DrawingCanvas is not null)
			{
				await root2DContext.SaveAsync();
				await root2DContext.GlobalAlphaAsync(layer.Opacity / 100.0);

				string dataUrl = await layer.DrawingCanvas.ToDataURLAsync();
				string variableName = await imageProcessor.UrlToImgElementAsync(dataUrl);
				await root2DContext.DrawImageAsync(variableName, 0, 0);

				await root2DContext.RestoreAsync();
			}

			LayerInfos.RemoveAll(x => x.LayerId == layer.LayerId);
		}

		await RegisterUndoableActionAsync("Merge Selected Layers");

		ForceLayersToRerender();
	}

	/* ----------------- */
	/* Selection actions */
	/* ----------------- */

	public async Task PerformSelectAllAsync()
	{
		await SelectAllAsync();
		await RegisterUndoableActionAsync("Select All");
	}

	public async Task PerformClearSelectionAsync()
	{
		await ClearSelectionAsync();
		await RegisterUndoableActionAsync("Clear Selection");
	}

	/* ----------------- */
	/* Clipboard actions */
	/* ----------------- */

	public async Task PerformCutAsync()
	{
		await CutAsync();
		await RegisterUndoableActionAsync("Cut");
	}

	public async Task PerformPasteAsync()
	{
		await PasteAsync();
		await RegisterUndoableActionAsync("Paste");
	}

	/* --------------- */
	/* Private actions */
	/* --------------- */

	private async Task PerformInitializeDocumentAsync()
	{
		SetActiveLayers(LayerInfos[0].LayerId);
		await RegisterUndoableActionAsync("Initialize Document");
	}

	private async Task PerformPaintBucketAsync()
	{
		await ActiveLayers.Batch_DoPaintBucketAsync(_lastClickLocation, CurrentColor);
		await RegisterUndoableActionAsync("Paint Bucket Fill");
	}

	#endregion Perform... (Undo-able actions)

	#region Pointer action sequence (Undo-able sequences)

	/* --------- */
	/* Transform */
	/* --------- */

	private async Task InitializeTransformAsync()
	{
		if (Selection.Size.IsEmpty)
			Selection = new Rectangle(Point.Empty, CanvasSize);

		bool cachedBool = IsInTransformActionPreviewMode;
		IsInTransformActionPreviewMode = Selection.Contains(_lastClickLocation) || _transformationAnchor != TransformationAnchor.None;
		bool hasChanged = cachedBool != IsInTransformActionPreviewMode;

		if (!hasChanged)
			return;

		if (IsInTransformActionPreviewMode)
		{
			foreach (Layer layer in ActiveLayers)
			{
				if (!layer.IsValidForEditing)
					continue;

				await layer.CacheCanvasSelection(Selection);

				Context2D context = await layer.GetDrawingCanvasContext2D();
				await context.ClearRectAsync(Selection);
			}
		}
		else
		{
			Context2D transformationAnchorsCanvasContext = await transformationAnchorsCanvasContextTask.Value;
			await transformationAnchorsCanvasContext.ClearAllAsync();
		}
	}

	private async Task DoTransformStepAsync(Point currentPointerLocation)
	{
		if (!IsInTransformActionPreviewMode)
			return;

		int deltaX = currentPointerLocation.X - _lastPointerLocation.X;
		int deltaY = currentPointerLocation.Y - _lastPointerLocation.Y;

		Point newLocation = _transformationAnchor switch
		{
			TransformationAnchor.None or TransformationAnchor.TopLeft => new Point(Selection.X + deltaX, Selection.Y + deltaY),
			TransformationAnchor.Top or TransformationAnchor.TopRight => new Point(Selection.X, Selection.Y + deltaY),
			TransformationAnchor.Left or TransformationAnchor.BottomLeft => new Point(Selection.X + deltaX, Selection.Y),
			_ => Selection.Location
		};

		Size newSize = _transformationAnchor switch
		{
			TransformationAnchor.TopLeft => new Size(Selection.Width - deltaX, Selection.Height - deltaY),
			TransformationAnchor.Top => new Size(Selection.Width, Selection.Height - deltaY),
			TransformationAnchor.TopRight => new Size(Selection.Width + deltaX, Selection.Height - deltaY),

			TransformationAnchor.Left => new Size(Selection.Width - deltaX, Selection.Height),
			TransformationAnchor.Right => new Size(Selection.Width + deltaX, Selection.Height),

			TransformationAnchor.BottomLeft => new Size(Selection.Width - deltaX, Selection.Height + deltaY),
			TransformationAnchor.Bottom => new Size(Selection.Width, Selection.Height + deltaY),
			TransformationAnchor.BottomRight => new Size(Selection.Width + deltaX, Selection.Height + deltaY),

			_ => Selection.Size
		};

		await ActiveLayers.Batch_PreviewTransformSelectionAsync(newLocation, newSize);
		await SelectAsync(newLocation, newSize);
	}

	private async Task FinalizeTransformAsync()
	{
		if (IsInTransformActionPreviewMode)
			return;

		await ActiveLayers.Batch_ApplyTransformationAsync(Selection.Location, Selection.Size);
	}

	/* ------------- */
	/* Box Selection */
	/* ------------- */

	private async Task DoBoxSelectionStepAsync(Point currentPointerLocation)
	{
		int x, y, width, height;

		x = Math.Min(currentPointerLocation.X, _lastClickLocation.X);
		x = x < 0 ? 0 : x; // Set 0 if less than 0
		x = x > CanvasSize.Width ? CanvasSize.Width : x; // Set MAX if more than MAX

		y = Math.Min(currentPointerLocation.Y, _lastClickLocation.Y);
		y = y < 0 ? 0 : y; // Set 0 if less than 0
		y = y > CanvasSize.Height ? CanvasSize.Height : y; // Set MAX if more than MAX

		int maxX = Math.Max(currentPointerLocation.X, _lastClickLocation.X);
		maxX = maxX < 0 ? 0 : maxX;

		width = Math.Abs(x - maxX);
		width = width < 0 ? 0 : width;
		width = x + width > CanvasSize.Width ? CanvasSize.Width - x : width;

		int maxY = Math.Max(currentPointerLocation.Y, _lastClickLocation.Y);
		maxY = maxY < 0 ? 0 : maxY;

		height = Math.Abs(y - maxY);
		height = height < 0 ? 0 : height;
		height = y + height > CanvasSize.Height ? CanvasSize.Height - y : height;

		var start = new Point(x, y);
		var size = new Size(width, height);

		await SelectAsync(start, size);
	}

	/* ------ */
	/* Shapes */
	/* ------ */

	private async Task InitializeShapeAsync(string name)
	{
		await ClearSelectionAsync();

		var newLayer = new LayerInfo(name, CanvasSize);
		await InsertLayersAsync(0, new[] { newLayer });

		SetActiveLayers(newLayer.LayerId);

		await ActiveLayers.First().InitializeShapeAsync(StrokeWidth, PrimaryColor, SecondaryColor, BrushOpacity);
	}

	private async Task DoRectangleStepAsync(Point currentPointerLocation)
	{
		Layer? topLayer = ActiveLayers.FirstOrDefault();

		if (topLayer is null)
			return;

		Point start = GetShapeStartLocation(currentPointerLocation);
		Size size = GetShapeSize(currentPointerLocation);

		await topLayer.DrawRectanglePreviewAsync(start, size, ShapeDrawingMode);
	}

	private async Task DoEllipseStepAsync(Point currentPointerLocation)
	{
		Layer? topLayer = ActiveLayers.FirstOrDefault();

		if (topLayer is null)
			return;

		Point start = GetShapeStartLocation(currentPointerLocation);
		Size size = GetShapeSize(currentPointerLocation);

		await topLayer.DrawEllipsePreviewAsync(start, size, ShapeDrawingMode);
	}

	private async Task FinalizeShapeAsync(Point endLocation)
	{
		Layer? topLayer = ActiveLayers.FirstOrDefault();

		if (topLayer is null)
			return;

		await topLayer.ApplyShapeAsync();

		int startX = Math.Min(_lastClickLocation.X, endLocation.X) - (StrokeWidth / 2);
		int startY = Math.Min(_lastClickLocation.Y, endLocation.Y) - (StrokeWidth / 2);

		var selectionStart = new Point(startX - 1, startY - 1);

		int width = Math.Abs(endLocation.X - _lastClickLocation.X) + StrokeWidth + 2;
		int height = Math.Abs(endLocation.Y - _lastClickLocation.Y) + StrokeWidth + 2;

		var selectionSize = new Size(width, height);

		await SelectAsync(selectionStart, selectionSize);
	}

	/* -------- */
	/* Sequence */
	/* -------- */

	private async Task StartPointerActionSequenceAsync(Point currentPointerLocation)
	{
		switch (ActiveTool)
		{
			case ToolType.Transform: await InitializeTransformAsync(); break;
			case ToolType.Brush: await ActiveLayers.Batch_InitializeStrokeActionAsync(StrokeWidth, CurrentColor, BrushOpacity, LineCap.Round); break;
			case ToolType.Line: await ActiveLayers.Batch_InitializeStrokeActionAsync(StrokeWidth, CurrentColor, BrushOpacity, LineCap); break;
			case ToolType.Rectangle: await InitializeShapeAsync("Rectangle"); break;
			case ToolType.Ellipse: await InitializeShapeAsync("Ellipse"); break;
		}

		IsPerformingPointerAction = true;

		await DoPointerActionSequenceAsync(currentPointerLocation);
	}

	private async Task DoPointerActionSequenceAsync(Point currentPointerLocation)
	{
		switch (ActiveTool)
		{
			case ToolType.Transform: await DoTransformStepAsync(currentPointerLocation); break;
			case ToolType.BoxSelection: await DoBoxSelectionStepAsync(currentPointerLocation); break;
			case ToolType.Brush: await ActiveLayers.Batch_DoBrushStrokeStepAsync(_lastPointerLocation, currentPointerLocation); break;
			case ToolType.Eraser: await ActiveLayers.Batch_DoEraserStepAsync(_lastPointerLocation, currentPointerLocation, StrokeWidth); break;
			case ToolType.Line: await ActiveLayers.Batch_DrawLinePreviewAsync(_lastClickLocation, currentPointerLocation); break;
			case ToolType.Rectangle: await DoRectangleStepAsync(currentPointerLocation); break;
			case ToolType.Ellipse: await DoEllipseStepAsync(currentPointerLocation); break;
		}
	}

	private async Task FinalizePointerActionSequenceAsync(Point releaseLocation)
	{
		switch (ActiveTool)
		{
			case ToolType.Transform: await FinalizeTransformAsync(); break;
			case ToolType.Brush: await ActiveLayers.Batch_ApplyBrushStrokeAsync(); break;
			case ToolType.Line: await ActiveLayers.Batch_ApplyShapeAsync(); break;

			case ToolType.Rectangle:
			case ToolType.Ellipse:
				await FinalizeShapeAsync(releaseLocation);
				break;
		}

		string actionLabel = Utils.GetActionLabelForTool(ActiveTool);
		await RegisterUndoableActionAsync(actionLabel);

		IsPerformingPointerAction = false;
	}

	#endregion Pointer action sequence (Undo-able sequences)

	#region Undo / Redo

	public async Task RegisterUndoableActionAsync(string actionLabel)
	{
		WorkspaceSnapshot snapshot = await CreateSnapshotAsync();

		UndoStack.Add(new WorkspaceAction(actionLabel, snapshot));
		RedoStack.Clear();

		if (UndoStack.Count > UNDO_STACK_SIZE)
		{
			for (int i = 0; i < UndoStack.Count - UNDO_STACK_SIZE; i++)
				UndoStack.RemoveAt(0);
		}
	}

	public async Task UndoAsync(int stepCount = 1)
	{
		if (stepCount < 1 || UndoStack.Count == 1)
			return;

		WorkspaceAction? workspaceAction = null;

		for (int i = 0; i <= stepCount; i++)
		{
			workspaceAction = UndoStack.Last();

			if (i < stepCount)
			{
				RedoStack.Add(workspaceAction);
				UndoStack.RemoveAt(UndoStack.Count - 1);
			}
		}

		if (workspaceAction is null)
			return;

		ActiveTool = workspaceAction.ResultingWorkspaceSnapshot.UsedToolType;

		LayerInfos.Clear();

		var layerInfoClones = new List<LayerInfo>();

		foreach (LayerInfo layerInfo in workspaceAction.ResultingWorkspaceSnapshot.LayerInfos)
			layerInfoClones.Add(layerInfo.Clone());

		LayerInfos.AddRange(layerInfoClones);

		ForceLayersToRerender();

		SetActiveLayers(workspaceAction.ResultingWorkspaceSnapshot.SelectedLayerIds.ToArray());

		IsInTransformActionPreviewMode = workspaceAction.ResultingWorkspaceSnapshot.TransformedDataUrls.Count > 0;
		await SelectAsync(workspaceAction.ResultingWorkspaceSnapshot.Selection);

		foreach (KeyValuePair<Guid, string?> entry in workspaceAction.ResultingWorkspaceSnapshot.TransformedDataUrls)
		{
			Layer? targetLayer = ActiveLayers.FirstOrDefault(layer => layer.LayerId == entry.Key);

			if (targetLayer is null || entry.Value is null)
				continue;

			await targetLayer.RestoreTransformationData(Selection, entry.Value);
		}
	}

	public async Task RedoAsync(int stepCount = 1)
	{
		if (stepCount < 1 || RedoStack.Count == 0)
			return;

		WorkspaceAction? workspaceAction = null;

		for (int i = 0; i < stepCount; i++)
		{
			workspaceAction = RedoStack.Last();
			UndoStack.Add(workspaceAction);

			RedoStack.RemoveAt(RedoStack.Count - 1);
		}

		if (workspaceAction is null)
			return;

		ActiveTool = workspaceAction.ResultingWorkspaceSnapshot.UsedToolType;

		LayerInfos.Clear();

		var layerInfoClones = new List<LayerInfo>();

		foreach (LayerInfo layerInfo in workspaceAction.ResultingWorkspaceSnapshot.LayerInfos)
			layerInfoClones.Add(layerInfo.Clone());

		LayerInfos.AddRange(layerInfoClones);

		ForceLayersToRerender();

		SetActiveLayers(workspaceAction.ResultingWorkspaceSnapshot.SelectedLayerIds.ToArray());

		IsInTransformActionPreviewMode = workspaceAction.ResultingWorkspaceSnapshot.TransformedDataUrls.Count > 0;
		await SelectAsync(workspaceAction.ResultingWorkspaceSnapshot.Selection);

		foreach (KeyValuePair<Guid, string?> entry in workspaceAction.ResultingWorkspaceSnapshot.TransformedDataUrls)
		{
			Layer? targetLayer = ActiveLayers.FirstOrDefault(layer => layer.LayerId == entry.Key);

			if (targetLayer is null || entry.Value is null)
				continue;

			await targetLayer.RestoreTransformationData(workspaceAction.ResultingWorkspaceSnapshot.Selection, entry.Value);
		}
	}

	#endregion Undo / Redo

	#region I/O

	public async Task SaveAsync(string fileName)
	{
		await UpdateImageDatasAsync();

		Document.FileName = Path.GetFileNameWithoutExtension(fileName) + ".blp";

		var settings = new XmlWriterSettings
		{
			Indent = true,
			IndentChars = "\t"
		};

		using var stream = new MemoryStream();
		using var writer = XmlWriter.Create(stream, settings);

		var serializer = new XmlSerializer(typeof(Document));
		serializer.Serialize(writer, Document);

		stream.Position = 0;

		using var reader = new StreamReader(stream);
		string text = await reader.ReadToEndAsync();

		using var streamRef = new DotNetStreamReference(stream);
		await JS.InvokeVoidAsync("downloadTextFile", Document.FileName, text);
	}

	public async Task ExportAsync(string fileName, FileFormat format)
	{
		if (imageProcessor is null)
			return;

		string dataUrl = await imageProcessor.FlattenLayersToUrlAsync(RenderedLayers, CanvasSize, Document.IsTransparentBackground, format);
		await JS.InvokeVoidAsync("triggerFileDownload", Path.GetFileNameWithoutExtension(fileName), dataUrl);
	}

	#endregion I/O

	#region Private methods

	/* ------ */
	/* Cursor */
	/* ------ */

	private async Task DrawCursorAsync(Point pointerLocation)
	{
		if (CursorStyle != "none" || cursorCanvas is null)
			return;

		Context2D cursorCanvasContext = await cursorCanvasContextTask.Value;
		await cursorCanvasContext.ClearAllAsync();
		await cursorCanvasContext.DrawStrokePreviewCursorAsync(pointerLocation, StrokeWidth);
	}

	private async Task ClearCursorAsync()
	{
		if (cursorCanvas is null)
			return;

		Context2D cursorCanvasContext = await cursorCanvasContextTask.Value;
		await cursorCanvasContext.ClearAllAsync();
	}

	/* --------- */
	/* Snapshots */
	/* --------- */

	private async ValueTask<WorkspaceSnapshot> CreateSnapshotAsync()
	{
		await UpdateImageDatasAsync();

		var layerClones = new List<LayerInfo>();
		var selectedLayerIdsCache = new List<Guid>();
		var transformedDataUrls = new Dictionary<Guid, string?>();

		foreach (LayerInfo layerInfo in LayerInfos)
		{
			LayerInfo layerClone = layerInfo.Clone();
			layerClones.Add(layerClone);

			bool isSelected = ActiveLayerInfos.Any(x => x.LayerId == layerInfo.LayerId);

			if (isSelected)
				selectedLayerIdsCache.Add(layerClone.LayerId);
		}

		foreach (Layer layer in ActiveLayers)
		{
			if (layer.IsValidForEditing && layer.CachedSelectionDataUrl is not null)
				transformedDataUrls.Add(layer.LayerId, layer.CachedSelectionDataUrl);
		}

		return new WorkspaceSnapshot(
			ActiveTool,
			layerClones,
			selectedLayerIdsCache,
			Selection,
			transformedDataUrls
		);
	}

	/* ------------------- */
	/* Misc. layer methods */
	/* ------------------- */

	private async Task UpdateImageDatasAsync()
	{
		foreach (LayerInfo layerInfo in LayerInfos)
		{
			Layer? ownerLayer = RenderedLayers.Find(x => x.LayerId == layerInfo.LayerId);

			if (ownerLayer is null || ownerLayer.DrawingCanvas is null || ownerLayer.IsWaitingForFirstRender)
				continue;

			string dataURL = await ownerLayer.DrawingCanvas.ToDataURLAsync();
			layerInfo.UpdateImageData(dataURL);
		}
	}

	private async Task RefreshLayerInfosAsync()
	{
		await UpdateImageDatasAsync();

		var newList = new List<LayerInfo>();

		foreach (LayerInfo layerInfo in LayerInfos)
		{
			LayerInfo newLayerInfo = layerInfo.Clone();
			newLayerInfo.VisibilityChanged += delegate { StateHasChanged(); };

			newList.Add(newLayerInfo);
		}

		LayerInfos.Clear();
		LayerInfos.AddRange(newList);
	}

	private void ForceLayersToRerender()
	{
		var selectedLayerIdsCache = new List<Guid>();

		foreach (LayerInfo layerInfo in ActiveLayerInfos)
			selectedLayerIdsCache.Add(layerInfo.Clone().LayerId);

		RenderedLayers.Clear();
		StateHasChanged();

		SetActiveLayers(selectedLayerIdsCache.ToArray());
	}

	/* ----------- */
	/* Conversions */
	/* ----------- */

	private Point GetAbsoluteEditingAreaPointerLocation(MouseEventArgs e)
	{
		int x = (int)e.OffsetX - EditingAreaBounds.X + Bounds.X;
		int y = (int)e.OffsetY - EditingAreaBounds.Y + Bounds.Y;

		return new Point(x, y);
	}

	private Rectangle GetSelectionRelativeToRoot()
	{
		int x = Selection.X + EditingAreaBounds.X - Bounds.X;
		int y = Selection.Y + EditingAreaBounds.Y - Bounds.Y;

		return new Rectangle(x, y, Selection.Width, Selection.Height);
	}

	private Point GetShapeStartLocation(Point currentPointerLocation)
		=> new(Math.Min(currentPointerLocation.X, _lastClickLocation.X), Math.Min(currentPointerLocation.Y, _lastClickLocation.Y));

	private Size GetShapeSize(Point currentPointerLocation)
		=> new(Math.Abs(currentPointerLocation.X - _lastClickLocation.X), Math.Abs(currentPointerLocation.Y - _lastClickLocation.Y));

	#endregion Private methods
}
