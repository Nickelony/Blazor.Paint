using Blazor.Canvas;
using Blazor.Paint.Components;
using Blazor.Paint.Core;
using Blazor.Paint.Core.Enums;
using KristofferStrube.Blazor.FileAPI;
using KristofferStrube.Blazor.FileSystemAccess;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Fast.Components.FluentUI;
using Microsoft.JSInterop;
using System.Buffers;
using System.Drawing;
using System.Xml.Serialization;
using File = KristofferStrube.Blazor.FileSystemAccess.File;

namespace Blazor.Paint;

public partial class MainView : ComponentBase
{
	[Inject] protected IJSRuntime JS { get; set; } = null!;
	[Inject] protected IURLService URL { get; set; } = null!;

	private string _tempFileName = string.Empty;
	private FileFormat _tempFileFormat;

	private string LineCapString
	{
		get
		{
			if (workspace is null)
				return "Butt";

			return Enum.GetName(workspace.LineCap)!;
		}
		set
		{
			if (workspace is null)
				return;

			workspace.LineCap = Enum.Parse<LineCap>(value);
		}
	}

	private string DrawingModeString
	{
		get
		{
			if (workspace is null)
				return "Stroke";

			return Enum.GetName(workspace.ShapeDrawingMode)!;
		}
		set
		{
			if (workspace is null)
				return;

			workspace.ShapeDrawingMode = Enum.Parse<ShapeDrawingMode>(value);
		}
	}

	private Document? currentDocument;
	private Workspace? workspace;

	private ElementReference? MainElement;

	private bool isFileMenuVisible;
	private bool isEditMenuVisible;
	private bool isSelectMenuVisible;
	private bool isDocumentMenuVisible;
	private bool isLayerMenuVisible;
	private bool isLayerItemContextMenuVisible;

	private string layerContextMenuStyle = string.Empty;

	private bool isNewMode;
	private bool isResizeDocumentMode;
	private bool isResizeCanvasMode;
	private bool isRenameLayerMode;
	private bool isSaveAsMode;
	private bool isExportAsMode;

	private int cachedWidth = 800;
	private int cachedHeight = 600;
	private CanvasAnchor cachedCanvasPlacement;
	private bool cachedIsTransparentBackground;
	private string? cachedLayerName;

	private bool isBatchSelectMode;

	private void BeginSaveAs()
	{
		CloseAllMenus();

		if (workspace is null)
			return;

		_tempFileName = Path.GetFileNameWithoutExtension(workspace.Document.FileName);
		isSaveAsMode = true;
	}

	private void BeginExport()
	{
		CloseAllMenus();

		if (workspace is null)
			return;

		_tempFileName = Path.GetFileNameWithoutExtension(workspace.Document.FileName);
		isExportAsMode = true;
	}

	private void BeginRenameLayer()
	{
		CloseAllMenus();

		if (workspace is null || workspace.ActiveLayerInfos.Count() != 1)
			return;

		cachedLayerName = workspace.ActiveLayerInfos.First().Name;
		isRenameLayerMode = true;
	}

	private async Task ApplyLayerRenameAsync()
	{
		await ExecuteCommandAsync(EditorCommand.RenameLayer);
		isRenameLayerMode = false;
	}

	private void CancelAll()
	{
		isNewMode =
		isResizeDocumentMode =
		isResizeCanvasMode =
		isRenameLayerMode =
		isSaveAsMode =
		isExportAsMode = false;
	}

	private async Task TryOpenLayerItemContextMenu(MouseEventArgs e, Guid layerId)
	{
		const int MOUSEBUTTON_RIGHT = 2;

		if (workspace is null || e.Button != MOUSEBUTTON_RIGHT)
			return;

		if (!workspace.ActiveLayerInfos.Any(x => x.LayerId == layerId))
			workspace.SetActiveLayers(layerId);

		BoundingClientRect bounds = await JS.InvokeAsync<BoundingClientRect>("getBoundingClientRect", MainElement);

		const int CONTEXT_MENU_WIDTH = 170;
		int x = e.PageX + CONTEXT_MENU_WIDTH > bounds.Right ? (int)bounds.Right - CONTEXT_MENU_WIDTH : (int)e.PageX;
		int y = (int)e.PageY;

		layerContextMenuStyle = $"position: fixed; top: {y}px; left: {x}px;";
		isLayerItemContextMenuVisible = true;
	}

	private async Task OpenDocumentAsync()
	{
		CloseAllMenus();

		if (workspace is null)
			return;

		FileSystemFileHandle? fileHandle = null;

		try
		{
			var acceptType = new FilePickerAcceptType
			{
				Description = "Blazor Paint File",
				Accept = new Dictionary<string, string[]>()
			};

			acceptType.Accept.Add("text/xml", new[] { ".blp" });

			OpenFilePickerOptionsStartInWellKnownDirectory options = new()
			{
				Multiple = false,
				StartIn = WellKnownDirectory.Downloads,
				Types = new[] { acceptType }
			};

			var service = new FileSystemAccessService(JS);
			FileSystemFileHandle[] fileHandles = await service.ShowOpenFilePickerAsync(options);

			fileHandle = fileHandles.Single();
		}
		catch (JSException ex)
		{
			Console.WriteLine(ex);
		}
		finally
		{
			if (fileHandle is not null)
			{
				File file = await fileHandle.GetFileAsync();
				byte[] bytes = await file.ArrayBufferAsync();
				var stream = new MemoryStream(bytes);

				currentDocument = new Document();
				StateHasChanged();

				var serializer = new XmlSerializer(typeof(Document));
				var openedDocument = serializer.Deserialize(stream) as Document;

				if (openedDocument is not null)
				{
					currentDocument.LayerInfos.AddRange(openedDocument.LayerInfos);
					currentDocument.FileName = openedDocument.FileName;
					currentDocument.IsTransparentBackground = openedDocument.IsTransparentBackground;
				}

				StateHasChanged();
			}
		}
	}

	private async Task ImportImageAsync()
	{
		CloseAllMenus();

		if (workspace is null)
			return;

		FileSystemFileHandle? fileHandle = null;

		try
		{
			var acceptType = new FilePickerAcceptType
			{
				Description = "Image File",
				Accept = new Dictionary<string, string[]>()
			};

			acceptType.Accept.Add("image/*", new[] { ".png", ".jpg" });

			OpenFilePickerOptionsStartInWellKnownDirectory options = new()
			{
				Multiple = false,
				StartIn = WellKnownDirectory.Downloads,
				Types = new[] { acceptType }
			};

			var service = new FileSystemAccessService(JS);
			FileSystemFileHandle[] fileHandles = await service.ShowOpenFilePickerAsync(options);

			fileHandle = fileHandles.Single();
		}
		catch (JSException ex)
		{
			Console.WriteLine(ex);
		}
		finally
		{
			if (fileHandle is not null)
			{
				File file = await fileHandle.GetFileAsync();
				byte[] fileBytes = await file.ArrayBufferAsync();

				BlobInProcess blob = await BlobInProcess.CreateAsync(JS,
					blobParts: new BlobPart[] { fileBytes },
					options: new() { Type = "image/*" }
				);

				string blobURL = await URL.CreateObjectURLAsync(blob);

				await workspace.StartTransformationAsync(blobURL);
				await workspace.RegisterUndoableActionAsync("Import image");
			}
		}
	}

	private async Task SetToolTypeAsync(ToolType toolType)
	{
		if (workspace is null)
			return;

		await workspace.SetActiveToolAsync(toolType);
	}

	protected override void OnInitialized()
	{
		CreateNewWorkspace();
		base.OnInitialized();
	}

	private void CreateNewWorkspace()
	{
		currentDocument = new Document { IsTransparentBackground = cachedIsTransparentBackground };

		var backgroundLayer = new LayerInfo("Background", new Size(cachedWidth, cachedHeight));
		currentDocument.LayerInfos.Add(backgroundLayer);

		isNewMode = false;

		StateHasChanged();
	}

	private async Task ResizeDocumentAsync()
	{
		if (workspace is null)
			return;

		var newSize = new Size(cachedWidth, cachedHeight);
		await workspace.PerformResizeCanvasAsync(newSize, CanvasAnchor.Stretched);

		isResizeDocumentMode = false;
	}

	private async Task ResizeCanvasAsync()
	{
		if (workspace is null)
			return;

		var newSize = new Size(cachedWidth, cachedHeight);
		await workspace.PerformResizeCanvasAsync(newSize, cachedCanvasPlacement);

		isResizeCanvasMode = false;
	}

	private void CloseAllMenus()
	{
		isFileMenuVisible =
		isEditMenuVisible =
		isSelectMenuVisible =
		isDocumentMenuVisible =
		isLayerMenuVisible =
		isLayerItemContextMenuVisible = false;
	}

	private async Task ToggleMenuAsync(MenuType type)
	{
		bool cachedFileMenuState = isFileMenuVisible,
			 cachedEditMenuState = isEditMenuVisible,
			 cachedSelectMenuState = isSelectMenuVisible,
			 cachedDocumentMenuState = isDocumentMenuVisible,
			 cachedLayerMenuState = isLayerMenuVisible;

		await Task.Delay(1);

		isFileMenuVisible = type == MenuType.File && !cachedFileMenuState;
		isEditMenuVisible = type == MenuType.Edit && !cachedEditMenuState;
		isSelectMenuVisible = type == MenuType.Select && !cachedSelectMenuState;
		isDocumentMenuVisible = type == MenuType.Document && !cachedDocumentMenuState;
		isLayerMenuVisible = type == MenuType.Layer && !cachedLayerMenuState;
	}

	private async Task OnKeyDownAsync(KeyboardEventArgs e)
	{
		if (e.CtrlKey)
		{
			isMultiselectMode = true;

			if (e.Key != "Control")
				await HandleKeyboardShortcutsAsync(e);
		}

		if (e.ShiftKey)
			isBatchSelectMode = true;

		if (e.Key == "Escape")
			await ExecuteCommandAsync(EditorCommand.ClearSelection);
	}

	private void OnKeyUp(KeyboardEventArgs e)
	{
		if (e.Key == "Control")
			isMultiselectMode = false;

		if (e.Key == "Shift")
			isBatchSelectMode = false;
	}

	private async Task HandleKeyboardShortcutsAsync(KeyboardEventArgs e)
	{
		EditorCommand command = e.Key switch
		{
			"s" => EditorCommand.SaveAs,

			"z" => EditorCommand.Undo,
			"y" => EditorCommand.Redo,

			"x" => EditorCommand.Cut,
			"c" => EditorCommand.Copy,
			"v" => EditorCommand.Paste,

			"a" => EditorCommand.SelectAll,

			_ => EditorCommand.None
		};

		await ExecuteCommandAsync(command);
	}

	private async Task ExecuteCommandAsync(EditorCommand command)
	{
		CloseAllMenus();

		switch (command)
		{
			case EditorCommand.New: isNewMode = true; break;
			case EditorCommand.Open: await OpenDocumentAsync(); break;
		}

		if (workspace is not null)
		{
			switch (command)
			{
				case EditorCommand.Undo: await workspace.UndoAsync(); break;
				case EditorCommand.Redo: await workspace.RedoAsync(); break;
				default:
					await workspace.ApplyTransformationAsync();

					switch (command)
					{
						case EditorCommand.SaveAs: await workspace.SaveAsync(_tempFileName); break;
						case EditorCommand.ExportAs: await workspace.ExportAsync(_tempFileName, _tempFileFormat); break;
						case EditorCommand.Import: await ImportImageAsync(); break;

						case EditorCommand.Cut: await workspace.PerformCutAsync(); break;
						case EditorCommand.Copy: await workspace.CopyAsync(); break;
						case EditorCommand.Paste: await workspace.PerformPasteAsync(); break;

						case EditorCommand.SelectAll: await workspace.PerformSelectAllAsync(); break;
						case EditorCommand.ClearSelection: await workspace.PerformClearSelectionAsync(); break;

						case EditorCommand.ResizeDocument:
							cachedWidth = workspace.CanvasSize.Width;
							cachedHeight = workspace.CanvasSize.Height;
							isResizeDocumentMode = true;
							break;

						case EditorCommand.ResizeCanvas:
							cachedWidth = workspace.CanvasSize.Width;
							cachedHeight = workspace.CanvasSize.Height;
							cachedCanvasPlacement = CanvasAnchor.TopLeft;
							isResizeCanvasMode = true;
							break;

						case EditorCommand.ToggleTransparency: workspace.Document.IsTransparentBackground = !workspace.Document.IsTransparentBackground; break;

						case EditorCommand.NewLayer:
							var newLayer = new LayerInfo($"Layer {workspace.LayerInfos.Count + 1}", workspace.CanvasSize);
							await workspace.InsertLayersAsync(0, newLayer);

							await workspace.RegisterUndoableActionAsync("Create Layer");
							break;

						case EditorCommand.NewFillLayer:
							var newFillLayer = new LayerInfo($"Layer {workspace.LayerInfos.Count + 1} (Fill)", workspace.CanvasSize);
							await workspace.InsertLayersAsync(0, newFillLayer);
							await workspace.FillLayerAsync(newFillLayer.LayerId, workspace.PrimaryColor);

							await workspace.RegisterUndoableActionAsync("Create Fill Layer");
							break;

						case EditorCommand.RemoveLayer: await workspace.PerformRemoveSelectedLayersAsync(); break;
						case EditorCommand.RenameLayer: await workspace.PerformRenameLayerAsync(workspace.ActiveLayerInfos.First().LayerId, cachedLayerName!); break;
						case EditorCommand.MoveLayerUp: await workspace.PerformMoveSelectedLayerUpAsync(); break;
						case EditorCommand.MoveLayerDown: await workspace.PerformMoveSelectedLayerDownAsync(); break;
						case EditorCommand.MergeSelectedLayers:
							await workspace.PerformMergeLayersAsync(workspace.ActiveLayerInfos.Select(x => x.LayerId).ToArray());
							break;
					}

					break;
			}
		}
	}

	private bool isMultiselectMode;

	private FluentTreeItem? lastTreeItem;
	private FluentTreeItem? treeItemBeforeThat;

	private FluentTreeItem? selectedTreeItem;

	private async Task SelectedLayerChanged(FluentTreeItem value)
	{
		if (workspace is null)
			return;

		await workspace.ApplyTransformationAsync();

		if (value == selectedTreeItem && value != lastTreeItem)
		{
			lastTreeItem = value;
			return;
		}

		lastTreeItem = null;

		selectedTreeItem = value;

		object? layerIdValue = selectedTreeItem?.AdditionalAttributes?.GetValueOrDefault("layerId");

		if (layerIdValue is null)
			return;

		var layerId = (Guid)layerIdValue;

		if (isMultiselectMode)
		{
			var newSelectedLayerIdList = new List<Guid>();
			newSelectedLayerIdList.AddRange(workspace.ActiveLayers.Select(x => x.LayerId));

			if (newSelectedLayerIdList.Contains(layerId))
				newSelectedLayerIdList.Remove(layerId);
			else
				newSelectedLayerIdList.Add(layerId);

			await workspace.PerformSetActiveLayersAsync(newSelectedLayerIdList.ToArray());
		}
		else if (isBatchSelectMode)
		{
			var newSelectedLayerIdList = new List<Guid>();

			object? lastLayerIdValue = treeItemBeforeThat?.AdditionalAttributes?.GetValueOrDefault("LayerId");

			if (lastLayerIdValue is null)
			{
				await workspace.PerformSetActiveLayersAsync(layerId);
				return;
			}

			var lastLayerId = (Guid)lastLayerIdValue;
			int lastLayerIndex = workspace.RenderedLayers.FindIndex(layer => layer.LayerId == lastLayerId);

			if (lastLayerIndex == -1)
			{
				await workspace.PerformSetActiveLayersAsync(layerId);
				return;
			}

			int currentLayerIndex = workspace.RenderedLayers.FindIndex(layer => layer.LayerId == layerId);

			int start = Math.Min(currentLayerIndex, lastLayerIndex);
			int end = Math.Max(currentLayerIndex, lastLayerIndex) + 1;

			newSelectedLayerIdList.AddRange(workspace.RenderedLayers.ToArray()[start..end].Select(layer => layer.LayerId));

			await workspace.PerformSetActiveLayersAsync(newSelectedLayerIdList.ToArray());
		}
		else
			await workspace.PerformSetActiveLayersAsync(layerId);

		treeItemBeforeThat = selectedTreeItem;
	}

	private async Task SelectedActionChanged(FluentTreeItem item)
	{
		object? actionTypeObject = item.AdditionalAttributes?.GetValueOrDefault("actionType");
		object? stepCountObject = item.AdditionalAttributes?.GetValueOrDefault("stepCount");

		if (workspace is not null
		&& Enum.TryParse(actionTypeObject?.ToString(), true, out UndoRedoAction action)
		&& int.TryParse(stepCountObject?.ToString(), out int stepCount))
		{
			if (action == UndoRedoAction.Undo)
				await workspace.UndoAsync(stepCount);
			else
				await workspace.RedoAsync(stepCount);
		}
	}
}
