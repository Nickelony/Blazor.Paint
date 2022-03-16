using Blazor.Canvas;

namespace System.Drawing;

public static class ImageDataDrawingExtensions
{
	public static Size GetSize(this ImageData imageData)
		=> new(imageData.Width, imageData.Height);

	public static Color GetPixel(this ImageData imageData, int x, int y)
		=> GetPixel(imageData, new Point(x, y));

	public static Color GetPixel(this ImageData imageData, Point point)
	{
		int pixelIndex = ((point.Y * imageData.Width) + point.X) * 4;
		return GetPixel(imageData, pixelIndex);
	}

	public static Color GetPixel(this ImageData imageData, int pixelIndex)
	{
		return Color.FromArgb(
			imageData.Data[pixelIndex + 3],
			imageData.Data[pixelIndex],
			imageData.Data[pixelIndex + 1],
			imageData.Data[pixelIndex + 2]);
	}

	public static void SetPixel(this ImageData imageData, int x, int y, Color color)
		=> SetPixel(imageData, new Point(x, y), color);

	public static void SetPixel(this ImageData imageData, Point point, Color color)
	{
		int pixelIndex = ((point.Y * imageData.Width) + point.X) * 4;
		SetPixel(imageData, pixelIndex, color);
	}

	public static void SetPixel(this ImageData imageData, int pixelIndex, Color color)
	{
		imageData.Data[pixelIndex] = color.R;
		imageData.Data[pixelIndex + 1] = color.G;
		imageData.Data[pixelIndex + 2] = color.B;
		imageData.Data[pixelIndex + 3] = color.A;
	}
}
