using Blazor.Canvas;
using Blazor.Canvas.Contexts;

namespace System.Drawing;

public static class Context2DDrawingExtensions
{
	public static async Task ClearRectAsync(this Context2D context, Rectangle rectangle)
		=> await context.ClearRectAsync(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

	public static async Task FillRectAsync(this Context2D context, Rectangle rectangle)
		=> await context.FillRectAsync(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

	public static async ValueTask<ImageData> GetImageDataAsync(this Context2D context, Rectangle rectangle)
		=> await context.GetImageDataAsync(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

	public static async Task LineToAsync(this Context2D context, Point point)
		=> await context.LineToAsync(point.X, point.Y);

	public static async Task MoveToAsync(this Context2D context, Point point)
		=> await context.MoveToAsync(point.X, point.Y);

	public static async Task PutImageDataAsync(this Context2D context, ImageData imageData, Point point)
		=> await context.PutImageDataAsync(imageData, point.X, point.Y);

	public static async Task RectAsync(this Context2D context, Rectangle rectangle)
		=> await context.RectAsync(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
}
