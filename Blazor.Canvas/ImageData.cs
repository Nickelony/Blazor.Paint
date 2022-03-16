namespace Blazor.Canvas;

/// <summary>
/// Represents the underlying pixel data of an area of a <c>&lt;canvas&gt;</c> element.
/// </summary>
public class ImageData
{
	public ImageData()
	{
		Data = Array.Empty<byte>();
		ColorSpace = "srgb";
	}

	public ImageData(int width, int height) : this()
	{
		Width = width;
		Height = height;
	}

	public ImageData(int width, int height, ImageDataSettings settings) : this(width, height)
		=> ColorSpace = settings.ColorSpace;

	public ImageData(byte[] data, int width) : this()
	{
		Data = data;
		Width = width;
		Height = data.Length / width;
	}

	public ImageData(byte[] data, int width, int height) : this(width, height)
		=> Data = data;

	public ImageData(byte[] data, int width, int height, ImageDataSettings settings) : this(data, width, height)
		=> ColorSpace = settings.ColorSpace;

	/// <summary>
	/// A byte array containing the pixel data in the RGBA order.
	/// <para>See <see href="https://developer.mozilla.org/en-US/docs/Web/API/ImageData/data">developer.mozilla.org</see> for more information.</para>
	/// </summary>
	public byte[] Data { get; set; }

	/// <summary>
	/// The actual width, in pixels, of the <see cref="ImageData" />.
	/// <para>See <see href="https://developer.mozilla.org/en-US/docs/Web/API/ImageData/width">developer.mozilla.org</see> for more information.</para>
	/// </summary>
	public int Width { get; set; }

	/// <summary>
	/// The actual height, in pixels, of the <see cref="ImageData" />.
	/// <para>See <see href="https://developer.mozilla.org/en-US/docs/Web/API/ImageData/height">developer.mozilla.org</see> for more information.</para>
	/// </summary>
	public int Height { get; set; }

	/// <summary>
	/// A string indicating the color space of the image data.
	/// <para>See <see href="https://developer.mozilla.org/en-US/docs/Web/API/ImageData/colorSpace">developer.mozilla.org</see> for more information.</para>
	/// </summary>
	public string ColorSpace { get; set; }

	public static ImageData Empty = new();
}
