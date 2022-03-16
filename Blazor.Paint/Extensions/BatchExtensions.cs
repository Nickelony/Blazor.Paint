using Blazor.Canvas;
using Blazor.Paint.Components;
using System.Drawing;

namespace Blazor.Paint.Extensions;

public static class BatchExtensions
{
	public static async Task Batch_PreviewTransformSelectionAsync(this IEnumerable<Layer> layers, Point newLocation, Size newSize)
	{
		foreach (Layer layer in layers.Where(layer => layer.IsValidForEditing))
			await layer.PreviewTransformSelection(newLocation, newSize);
	}

	public static async Task Batch_ApplyTransformationAsync(this IEnumerable<Layer> layers, Point finalLocation, Size finalSize)
	{
		foreach (Layer layer in layers.Where(layer => layer.IsValidForEditing))
			await layer.ApplyTransformation(finalLocation, finalSize);
	}

	public static async Task Batch_InitializeStrokeActionAsync(this IEnumerable<Layer> layers, int strokeWidth, string color, int opacityPercent, LineCap cap)
	{
		foreach (Layer layer in layers.Where(layer => layer.IsValidForEditing))
			await layer.InitializeStrokeActionAsync(strokeWidth, color, opacityPercent, cap);
	}

	public static async Task Batch_InitializeShapeAsync(this IEnumerable<Layer> layers,
		int strokeWidth, string strokeColor, string fillColor, int opacityPercent)
	{
		foreach (Layer layer in layers.Where(layer => layer.IsValidForEditing))
			await layer.InitializeShapeAsync(strokeWidth, strokeColor, fillColor, opacityPercent);
	}

	public static async Task Batch_DoBrushStrokeStepAsync(this IEnumerable<Layer> layers, Point stepStart, Point stepEnd)
	{
		foreach (Layer layer in layers.Where(layer => layer.IsValidForEditing))
			await layer.DoBrushStrokeStepAsync(stepStart, stepEnd);
	}

	public static async Task Batch_ApplyBrushStrokeAsync(this IEnumerable<Layer> layers)
	{
		foreach (Layer layer in layers.Where(layer => layer.IsValidForEditing))
			await layer.ApplyBrushStrokeAsync();
	}

	public static async Task Batch_DoEraserStepAsync(this IEnumerable<Layer> layers,
		Point stepStart, Point stepEnd, int strokeWidth)
	{
		foreach (Layer layer in layers.Where(layer => layer.IsValidForEditing))
			await layer.DoEraserStepAsync(stepStart, stepEnd, strokeWidth);
	}

	public static async Task Batch_DoPaintBucketAsync(this IEnumerable<Layer> layers, Point startingPoint, string color)
	{
		foreach (Layer layer in layers.Where(layer => layer.IsValidForEditing))
			await layer.DoPaintBucketAsync(startingPoint, color);
	}

	public static async Task Batch_DrawLinePreviewAsync(this IEnumerable<Layer> layers, Point start, Point end)
	{
		foreach (Layer layer in layers.Where(layer => layer.IsValidForEditing))
			await layer.DrawLinePreviewAsync(start, end);
	}

	public static async Task Batch_ApplyShapeAsync(this IEnumerable<Layer> layers)
	{
		foreach (Layer layer in layers.Where(layer => layer.IsValidForEditing))
			await layer.ApplyShapeAsync();
	}
}
