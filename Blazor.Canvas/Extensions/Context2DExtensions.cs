using Blazor.Canvas.Contexts;

namespace Blazor.Canvas.Extensions;

public static class Context2DExtensions
{
	/// <summary>
	/// Clears the entire canvas.
	/// </summary>
	public static async Task ClearAllAsync(this Context2D context)
		=> await context.ClearRectAsync(0, 0, context.Canvas.Width, context.Canvas.Height);

	/// <summary>
	/// Fills the entire canvas.
	/// </summary>
	public static async Task FillAllAsync(this Context2D context)
		=> await context.FillRectAsync(0, 0, context.Canvas.Width, context.Canvas.Height);
}
