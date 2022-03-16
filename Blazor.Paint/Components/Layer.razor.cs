using Blazor.Canvas;
using Blazor.Canvas.Contexts;
using Blazor.Canvas.Extensions;
using Blazor.Paint.Core;
using Blazor.Paint.Core.Enums;
using Microsoft.AspNetCore.Components;
using System.Drawing;

namespace Blazor.Paint.Components;

public partial class Layer : ComponentBase
{
	#region Properties / Fields

	[EditorRequired, Parameter] public LayerInfo LayerInfo { private get; set; } = null!;
	[EditorRequired, Parameter] public int ZIndex { get; set; }

	public Guid LayerId => LayerInfo.LayerId;
	public Size CanvasSize => LayerInfo.CanvasSize;
	public bool IsVisible => LayerInfo.IsVisible;
	public int Opacity => LayerInfo.Opacity;

	public BlazorCanvas? DrawingCanvas { get; private set; }

	public string? CachedSelectionDataUrl { get; set; }

	public bool IsWaitingForFirstRender { get; private set; } = true;

	public bool IsValidForEditing
		=> DrawingCanvas is not null && IsVisible;

	/// <summary>
	/// A canvas which renders the current action (such as moving a selected area), without applying it onto the actual layer canvas right away.
	/// </summary>
	private BlazorCanvas? actionPreviewCanvas;

	private ImageProcessor? imageProcessor;

	private Lazy<Task<Context2D>> drawingCanvasContextTask;
	private Lazy<Task<Context2D>> actionPreviewContextTask;

	public double brushOpacity = 1.0;

	#endregion Properties / Fields

	public Layer()
	{
		drawingCanvasContextTask = new(() => DrawingCanvas!.GetContext2DAsync().AsTask());
		actionPreviewContextTask = new(() => actionPreviewCanvas!.GetContext2DAsync().AsTask());
	}

	#region Override methods

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await ApplyImageData();
			IsWaitingForFirstRender = false;
		}

		await base.OnAfterRenderAsync(firstRender);
	}

	#endregion Override methods

	#region Selection

	public async Task CacheCanvasSelection(Rectangle selection)
	{
		Context2D drawingCanvasContext = await drawingCanvasContextTask.Value;
		ImageData imageData = await drawingCanvasContext.GetImageDataAsync(selection);

		CachedSelectionDataUrl = await imageProcessor!.ImageDataToUrlAsync(imageData);
	}

	public async Task RestoreTransformationData(Rectangle selection, string dataUrl)
	{
		CachedSelectionDataUrl = dataUrl;
		await PreviewTransformSelection(selection.Location, selection.Size);
	}

	public async Task PreviewTransformSelection(Point newLocation, Size newSize)
	{
		Context2D actionPreviewContext = await actionPreviewContextTask.Value;
		await actionPreviewContext.ClearAllAsync();

		string variableName = await imageProcessor.UrlToImgElementAsync(CachedSelectionDataUrl);
		await actionPreviewContext.DrawImageAsync(variableName, newLocation.X, newLocation.Y, newSize.Width, newSize.Height);
	}

	public async Task PreviewTransformSelection(string variableName, Point newLocation, Size newSize)
	{
		Context2D actionPreviewContext = await actionPreviewContextTask.Value;
		await actionPreviewContext.ClearAllAsync();

		await actionPreviewContext.DrawImageAsync(variableName, newLocation.X, newLocation.Y, newSize.Width, newSize.Height);
	}

	public async Task ApplyTransformation(Point finalLocation, Size finalSize)
	{
		Context2D drawingCanvasContext = await drawingCanvasContextTask.Value;

		string variableName = await imageProcessor.UrlToImgElementAsync(CachedSelectionDataUrl);
		await drawingCanvasContext.DrawImageAsync(variableName, finalLocation.X, finalLocation.Y, finalSize.Width, finalSize.Height);

		CachedSelectionDataUrl = null;

		await ClearActionPreviewCanvas();
	}

	#endregion Selection

	#region Drawing shapes

	public async Task DrawLinePreviewAsync(Point start, Point end)
	{
		Context2D actionPreviewContext = await actionPreviewContextTask.Value;
		await actionPreviewContext.ClearAllAsync();

		await actionPreviewContext.BeginPathAsync();
		await actionPreviewContext.MoveToAsync(start.X, start.Y);
		await actionPreviewContext.LineToAsync(end.X, end.Y);
		await actionPreviewContext.StrokeAsync();
	}

	public async Task DrawRectanglePreviewAsync(Point location, Size size, ShapeDrawingMode drawingMode)
	{
		Context2D actionPreviewContext = await actionPreviewContextTask.Value;
		await actionPreviewContext.ClearAllAsync();

		await actionPreviewContext.BeginPathAsync();
		await actionPreviewContext.RectAsync(location.X, location.Y, size.Width, size.Height);

		if (drawingMode is ShapeDrawingMode.Fill or ShapeDrawingMode.Both)
			await actionPreviewContext.FillAsync();

		if (drawingMode is ShapeDrawingMode.Stroke or ShapeDrawingMode.Both)
			await actionPreviewContext.StrokeAsync();
	}

	public async Task DrawEllipsePreviewAsync(Point location, Size size, ShapeDrawingMode drawingMode)
	{
		Context2D actionPreviewContext = await actionPreviewContextTask.Value;
		await actionPreviewContext.ClearAllAsync();

		await actionPreviewContext.BeginPathAsync();

		int radiusX = size.Width / 2;
		int radiusY = size.Height / 2;

		int x = location.X + radiusX;
		int y = location.Y + radiusY;

		await actionPreviewContext.EllipseAsync(x, y, radiusX, radiusY, 0, 0, 2 * Math.PI);

		if (drawingMode is ShapeDrawingMode.Fill or ShapeDrawingMode.Both)
			await actionPreviewContext.FillAsync();

		if (drawingMode is ShapeDrawingMode.Stroke or ShapeDrawingMode.Both)
			await actionPreviewContext.StrokeAsync();
	}

	public async Task ApplyShapeAsync()
	{
		string dataUrl = await actionPreviewCanvas!.ToDataURLAsync();

		Context2D drawingCanvasContext = await drawingCanvasContextTask.Value;

		await drawingCanvasContext.SaveAsync();

		await drawingCanvasContext.GlobalAlphaAsync(brushOpacity);
		string variableName = await imageProcessor.UrlToImgElementAsync(dataUrl);
		await drawingCanvasContext.DrawImageAsync(variableName, 0, 0);

		await drawingCanvasContext.RestoreAsync();

		await ClearActionPreviewCanvas();
	}

	#endregion Drawing shapes

	#region Actions

	public async Task FillCanvasAsync(string color)
	{
		Context2D drawingCanvasContext = await drawingCanvasContextTask.Value;
		await drawingCanvasContext.FillStyleAsync(color);
		await drawingCanvasContext.FillRectAsync(0, 0, CanvasSize.Width, CanvasSize.Height);
	}

	public async Task InitializeStrokeActionAsync(int strokeWidth, string color, int opacityPercent, LineCap cap)
	{
		brushOpacity = opacityPercent / 100.0;
		StateHasChanged();

		Context2D actionPreviewContext = await actionPreviewContextTask.Value;

		await actionPreviewContext.LineWidthAsync(strokeWidth);
		await actionPreviewContext.LineJoinAsync(LineJoin.Round);
		await actionPreviewContext.LineCapAsync(cap);
		await actionPreviewContext.StrokeStyleAsync(color);
	}

	public async Task InitializeShapeAsync(int strokeWidth, string strokeColor, string fillColor, int opacityPercent)
	{
		brushOpacity = opacityPercent / 100.0;
		StateHasChanged();

		Context2D actionPreviewContext = await actionPreviewContextTask.Value;

		await actionPreviewContext.LineWidthAsync(strokeWidth);
		await actionPreviewContext.StrokeStyleAsync(strokeColor);
		await actionPreviewContext.FillStyleAsync(fillColor);
	}

	public async Task DoBrushStrokeStepAsync(Point stepStart, Point stepEnd)
	{
		Context2D actionPreviewContext = await actionPreviewContextTask.Value;

		actionPreviewContext.BeginPath();

		actionPreviewContext.MoveTo(stepStart.X, stepStart.Y);
		actionPreviewContext.LineTo(stepEnd.X, stepEnd.Y);

		actionPreviewContext.Stroke();
	}

	public async Task ApplyBrushStrokeAsync()
	{
		string dataUrl = await actionPreviewCanvas!.ToDataURLAsync();

		Context2D drawingCanvasContext = await drawingCanvasContextTask.Value;

		await drawingCanvasContext.SaveAsync();

		await drawingCanvasContext.GlobalAlphaAsync(brushOpacity);
		string variableName = await imageProcessor!.UrlToImgElementAsync(dataUrl);
		await drawingCanvasContext.DrawImageAsync(variableName, 0, 0);

		await drawingCanvasContext.RestoreAsync();

		await ClearActionPreviewCanvas();
	}

	public async Task DoEraserStepAsync(Point stepStart, Point stepEnd, int strokeWidth)
	{
		Context2D drawingCanvasContext = await drawingCanvasContextTask.Value;

		await drawingCanvasContext.SaveAsync();

		await drawingCanvasContext.BeginPathAsync();
		await drawingCanvasContext.MoveToAsync(stepStart.X, stepStart.Y);

		await drawingCanvasContext.GlobalCompositeOperationAsync(CompositeOperation.DestinationOut);
		await drawingCanvasContext.LineWidthAsync(strokeWidth);
		await drawingCanvasContext.LineJoinAsync(LineJoin.Round);
		await drawingCanvasContext.LineCapAsync(LineCap.Round);
		await drawingCanvasContext.StrokeStyleAsync($"rgba(255, 255, 255, 1)");

		await drawingCanvasContext.LineToAsync(stepEnd.X, stepEnd.Y);
		await drawingCanvasContext.StrokeAsync();

		await drawingCanvasContext.RestoreAsync();
	}

	public async Task DoPaintBucketAsync(Point startingPoint, string color)
	{
		if (startingPoint.X < 0 || startingPoint.Y < 0 || startingPoint.X >= CanvasSize.Width || startingPoint.Y >= CanvasSize.Height)
			return;

		Context2D drawingCanvasContext = await drawingCanvasContextTask.Value;
		ImageData imageData = await drawingCanvasContext.GetImageDataAsync(0, 0, CanvasSize.Width, CanvasSize.Height);

		int pixelPosition = ((startingPoint.Y * CanvasSize.Width) + startingPoint.X) * 4;

		Color startColor = imageData.GetPixel(pixelPosition);
		Color targetColor = ColorTranslator.FromHtml(color);

		if (startColor == targetColor)
			return;

		bool reachLeft,
			 reachRight;

		int drawingBoundLeft = 0,
			drawingBoundTop = 0,
			drawingBoundRight = CanvasSize.Width - 1,
			drawingBoundBottom = CanvasSize.Height - 1;

		var pixelStack = new Stack<Point>(new[] { new Point(startingPoint.X, startingPoint.Y) });

		while (pixelStack.Count > 0)
		{
			Point newPosition = pixelStack.Pop();

			int x = newPosition.X,
				y = newPosition.Y;

			pixelPosition = ((y * CanvasSize.Width) + x) * 4;

			// Go up as long as the color matches and are inside the canvas
			while (y >= drawingBoundTop && imageData.GetPixel(pixelPosition) == startColor)
			{
				pixelPosition -= CanvasSize.Width * 4;
				y--;
			}

			pixelPosition += CanvasSize.Width * 4;
			y++;

			reachLeft = false;
			reachRight = false;

			// Go down as long as the color matches and in inside the canvas
			while (y <= drawingBoundBottom && imageData.GetPixel(pixelPosition) == startColor)
			{
				y++;

				imageData.Data[pixelPosition] = targetColor.R;
				imageData.Data[pixelPosition + 1] = targetColor.G;
				imageData.Data[pixelPosition + 2] = targetColor.B;
				imageData.Data[pixelPosition + 3] = targetColor.A;

				if (x > drawingBoundLeft)
				{
					if (imageData.GetPixel(pixelPosition - 4) == startColor)
					{
						if (!reachLeft)
						{
							// Add pixel to stack
							pixelStack.Push(new Point(x - 1, y));
							reachLeft = true;
						}
					}
					else if (reachLeft)
					{
						reachLeft = false;
					}
				}

				if (x < drawingBoundRight)
				{
					if (imageData.GetPixel(pixelPosition + 4) == startColor)
					{
						if (!reachRight)
						{
							// Add pixel to stack
							pixelStack.Push(new Point(x + 1, y));
							reachRight = true;
						}
					}
					else if (reachRight)
					{
						reachRight = false;
					}
				}

				pixelPosition += CanvasSize.Width * 4;
			}
		}

		await drawingCanvasContext.PutImageDataAsync(imageData, 0, 0);
	}

	#endregion Actions

	#region Other methods

	public async ValueTask<Context2D> GetDrawingCanvasContext2D()
		=> await drawingCanvasContextTask.Value;

	private async Task ApplyImageData()
	{
		int x = LayerInfo.ImageSnapshot.CanvasPlacement.X;
		int y = LayerInfo.ImageSnapshot.CanvasPlacement.Y;
		int width = LayerInfo.ImageSnapshot.Size.Width;
		int height = LayerInfo.ImageSnapshot.Size.Height;

		Context2D drawingCanvasContext = await drawingCanvasContextTask.Value;

		string variableName = await imageProcessor!.UrlToImgElementAsync(LayerInfo.ImageSnapshot.Url);
		await drawingCanvasContext.DrawImageAsync(variableName, x, y, width, height);
	}

	private async Task ClearActionPreviewCanvas()
	{
		Context2D actionPreviewContext = await actionPreviewContextTask.Value;
		await actionPreviewContext.ClearAllAsync();

		brushOpacity = 1.0;
	}

	#endregion Other methods
}
