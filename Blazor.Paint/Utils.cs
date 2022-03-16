using Blazor.Paint.Core.Enums;
using System.Drawing;

namespace Blazor.Paint;

public static class Utils
{
	public static string GetActionLabelForTool(ToolType tool) => tool switch
	{
		ToolType.Transform => "Transform Selection",
		ToolType.BoxSelection => "Box Selection",
		ToolType.Brush => "Brush Stroke",
		ToolType.Eraser => "Eraser",
		ToolType.Line => "Draw Line",
		ToolType.Rectangle => "Draw Rectangle",
		ToolType.Ellipse => "Draw Ellipse",
		_ => "Unknown Action"
	};

	public static Rectangle GetTransformationAnchorRectangle(TransformationAnchor anchor, Rectangle selection)
	{
		if (selection.Size.IsEmpty)
			return Rectangle.Empty;

		int x = anchor switch
		{
			TransformationAnchor.TopLeft => selection.X - Constants.TRANSFORMATION_RECT_WIDTH - Constants.TRANSFORMATION_RECT_OFFSET,
			TransformationAnchor.Top => selection.X + (selection.Width / 2) - (Constants.TRANSFORMATION_RECT_WIDTH / 2),
			TransformationAnchor.TopRight => selection.X + selection.Width + Constants.TRANSFORMATION_RECT_OFFSET,

			TransformationAnchor.Left => selection.X - Constants.TRANSFORMATION_RECT_WIDTH - Constants.TRANSFORMATION_RECT_OFFSET,
			TransformationAnchor.Right => selection.X + selection.Width + Constants.TRANSFORMATION_RECT_OFFSET,

			TransformationAnchor.BottomLeft => selection.X - Constants.TRANSFORMATION_RECT_WIDTH - Constants.TRANSFORMATION_RECT_OFFSET,
			TransformationAnchor.Bottom => selection.X + (selection.Width / 2) - (Constants.TRANSFORMATION_RECT_WIDTH / 2),
			TransformationAnchor.BottomRight => selection.X + selection.Width + Constants.TRANSFORMATION_RECT_OFFSET,

			_ => selection.X
		};

		int y = anchor switch
		{
			TransformationAnchor.TopLeft => selection.Y - Constants.TRANSFORMATION_RECT_HEIGHT - Constants.TRANSFORMATION_RECT_OFFSET,
			TransformationAnchor.Top => selection.Y - Constants.TRANSFORMATION_RECT_HEIGHT - Constants.TRANSFORMATION_RECT_OFFSET,
			TransformationAnchor.TopRight => selection.Y - Constants.TRANSFORMATION_RECT_HEIGHT - Constants.TRANSFORMATION_RECT_OFFSET,

			TransformationAnchor.Left => selection.Y + (selection.Height / 2) - (Constants.TRANSFORMATION_RECT_HEIGHT / 2),
			TransformationAnchor.Right => selection.Y + (selection.Height / 2) - (Constants.TRANSFORMATION_RECT_HEIGHT / 2),

			TransformationAnchor.BottomLeft => selection.Y + selection.Height + Constants.TRANSFORMATION_RECT_OFFSET,
			TransformationAnchor.Bottom => selection.Y + selection.Height + Constants.TRANSFORMATION_RECT_OFFSET,
			TransformationAnchor.BottomRight => selection.Y + selection.Height + Constants.TRANSFORMATION_RECT_OFFSET,

			_ => selection.Y
		};

		return new Rectangle(x, y, Constants.TRANSFORMATION_RECT_WIDTH, Constants.TRANSFORMATION_RECT_HEIGHT);
	}

	public static TransformationAnchor GetTransformationAnchorFromPoint(Rectangle selection, Point point)
	{
		TransformationAnchor result = TransformationAnchor.None;

		Rectangle topLeft = GetTransformationAnchorRectangle(TransformationAnchor.TopLeft, selection);
		Rectangle top = GetTransformationAnchorRectangle(TransformationAnchor.Top, selection);
		Rectangle topRight = GetTransformationAnchorRectangle(TransformationAnchor.TopRight, selection);

		Rectangle left = GetTransformationAnchorRectangle(TransformationAnchor.Left, selection);
		Rectangle right = GetTransformationAnchorRectangle(TransformationAnchor.Right, selection);

		Rectangle bottomLeft = GetTransformationAnchorRectangle(TransformationAnchor.BottomLeft, selection);
		Rectangle bottom = GetTransformationAnchorRectangle(TransformationAnchor.Bottom, selection);
		Rectangle bottomRight = GetTransformationAnchorRectangle(TransformationAnchor.BottomRight, selection);

		result = !topLeft.IsEmpty && topLeft.Contains(point) ? TransformationAnchor.TopLeft : result;
		result = !top.IsEmpty && top.Contains(point) ? TransformationAnchor.Top : result;
		result = !topRight.IsEmpty && topRight.Contains(point) ? TransformationAnchor.TopRight : result;

		result = !left.IsEmpty && left.Contains(point) ? TransformationAnchor.Left : result;
		result = !right.IsEmpty && right.Contains(point) ? TransformationAnchor.Right : result;

		result = !bottomLeft.IsEmpty && bottomLeft.Contains(point) ? TransformationAnchor.BottomLeft : result;
		result = !bottom.IsEmpty && bottom.Contains(point) ? TransformationAnchor.Bottom : result;
		result = !bottomRight.IsEmpty && bottomRight.Contains(point) ? TransformationAnchor.BottomRight : result;

		return result;
	}

	public static string GetTransformationCursorStyle(TransformationAnchor anchor)
	{
		string result = "auto";

		result = anchor is TransformationAnchor.Top or TransformationAnchor.Bottom ? "ns-resize" : result;
		result = anchor is TransformationAnchor.Left or TransformationAnchor.Right ? "ew-resize" : result;
		result = anchor is TransformationAnchor.TopLeft or TransformationAnchor.BottomRight ? "nwse-resize" : result;
		result = anchor is TransformationAnchor.BottomLeft or TransformationAnchor.TopRight ? "nesw-resize" : result;

		return result;
	}
}
