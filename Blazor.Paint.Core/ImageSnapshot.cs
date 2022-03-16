using System.Drawing;

namespace Blazor.Paint.Core;

public sealed class ImageSnapshot
{
	public string Url { get; set; } = string.Empty;
	public Size Size { get; set; }
	public Point CanvasPlacement { get; set; }

	public ImageSnapshot()
	{ }

	public ImageSnapshot(string url, Size size, Point canvasPlacement)
	{
		Url = url;
		Size = size;
		CanvasPlacement = canvasPlacement;
	}

	public ImageSnapshot Clone()
		=> new(Url, Size, CanvasPlacement);

	public static readonly ImageSnapshot Empty = new(string.Empty, new Size(1, 1), new Point(0, 0));
}
