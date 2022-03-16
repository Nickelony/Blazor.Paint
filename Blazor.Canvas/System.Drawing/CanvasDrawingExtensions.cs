using Blazor.Canvas;

namespace System.Drawing;

public static class CanvasDrawingExtensions
{
	public static Size GetSize(this BlazorCanvas canvas)
		=> new((int)canvas.Width, (int)canvas.Height);
}
