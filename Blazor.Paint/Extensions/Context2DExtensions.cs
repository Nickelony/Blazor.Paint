using Blazor.Canvas.Contexts;
using Blazor.Paint.Core.Enums;
using System.Drawing;

namespace Blazor.Paint.Extensions;

public static class Context2DExtensions
{
	public static async Task DrawStrokePreviewCursorAsync(this Context2D context, Point pointerLocation, int strokeWidth)
	{
		int radius = strokeWidth / 2;

		if (radius == 0)
			radius = 1;

		await context.BeginPathAsync();
		await context.ArcAsync(pointerLocation.X, pointerLocation.Y, radius, 0, 2 * Math.PI, false);
		await context.LineWidthAsync(1);
		await context.StrokeStyleAsync("black");
		await context.StrokeAsync();

		await context.BeginPathAsync();
		await context.ArcAsync(pointerLocation.X, pointerLocation.Y, radius + 1, 0, 2 * Math.PI, false);
		await context.LineWidthAsync(1);
		await context.StrokeStyleAsync("white");
		await context.StrokeAsync();
	}

	public static async Task DrawSelectionRectangleAsync(this Context2D context, Rectangle selection)
	{
		await context.LineWidthAsync(1);
		await context.SetLineDashAsync(3.0, 3.0);

		// Black dashes
		await context.StrokeStyleAsync("black");
		await context.LineDashOffsetAsync(0.0);

		await context.BeginPathAsync();
		await context.RectAsync(selection);
		await context.StrokeAsync();

		// White dashes
		await context.StrokeStyleAsync("white");
		await context.LineDashOffsetAsync(3.0);

		await context.BeginPathAsync();
		await context.RectAsync(selection);
		await context.StrokeAsync();

		await context.SetLineDashAsync(0.0);
	}

	public static async Task DrawTransformationAnchorsAsync(this Context2D context, Rectangle selection)
	{
		selection.Inflate(Constants.TRANSFORMATION_RECT_OFFSET, Constants.TRANSFORMATION_RECT_OFFSET);

		// Focus rectangle
		await context.StrokeStyleAsync("blue");

		await context.BeginPathAsync();
		await context.RectAsync(selection);
		await context.StrokeAsync();

		// TopLeft
		await context.FillStyleAsync("white");

		await context.BeginPathAsync();
		await context.RectAsync(Utils.GetTransformationAnchorRectangle(TransformationAnchor.TopLeft, selection));
		await context.FillAsync();
		await context.StrokeAsync();

		// Top
		await context.BeginPathAsync();
		await context.RectAsync(Utils.GetTransformationAnchorRectangle(TransformationAnchor.Top, selection));
		await context.FillAsync();
		await context.StrokeAsync();

		// TopRight
		await context.BeginPathAsync();
		await context.RectAsync(Utils.GetTransformationAnchorRectangle(TransformationAnchor.TopRight, selection));
		await context.FillAsync();
		await context.StrokeAsync();

		// Left
		await context.BeginPathAsync();
		await context.RectAsync(Utils.GetTransformationAnchorRectangle(TransformationAnchor.Left, selection));
		await context.FillAsync();
		await context.StrokeAsync();

		// Right
		await context.BeginPathAsync();
		await context.RectAsync(Utils.GetTransformationAnchorRectangle(TransformationAnchor.Right, selection));
		await context.FillAsync();
		await context.StrokeAsync();

		// BottomLeft
		await context.BeginPathAsync();
		await context.RectAsync(Utils.GetTransformationAnchorRectangle(TransformationAnchor.BottomLeft, selection));
		await context.FillAsync();
		await context.StrokeAsync();

		// Bottom
		await context.BeginPathAsync();
		await context.RectAsync(Utils.GetTransformationAnchorRectangle(TransformationAnchor.Bottom, selection));
		await context.FillAsync();
		await context.StrokeAsync();

		// BottomRight
		await context.BeginPathAsync();
		await context.RectAsync(Utils.GetTransformationAnchorRectangle(TransformationAnchor.BottomRight, selection));
		await context.FillAsync();
		await context.StrokeAsync();
	}
}
