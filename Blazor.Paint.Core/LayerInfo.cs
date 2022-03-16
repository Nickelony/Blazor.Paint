using Blazor.Paint.Core.Enums;
using System.Drawing;

namespace Blazor.Paint.Core;

public sealed class LayerInfo
{
	#region Properties

	/// <summary>
	/// Primary identifying key of the layer.
	/// <para>This key <u>never changes</u> for the layer.</para>
	/// </summary>
	public Guid LayerId { get; init; }

	/// <summary>
	/// Displayed name of the layer.
	/// </summary>
	public string Name { get; set; } = "Layer";

	/// <summary>
	/// Dimensions of the layer's canvas.
	/// </summary>
	public Size CanvasSize { get; set; }

	/// <summary>
	/// Storage variable for re-applying dry pixels after some breaking change, such as <b>canvas resize</b>, caused the canvas to clear itself.
	/// </summary>
	public ImageSnapshot ImageSnapshot { get; set; } = ImageSnapshot.Empty;

	/// <summary>
	/// Main blazor component state identifier.
	/// <para>This key <u>always changes</u> when the layers are regenerated.</para>
	/// <para>Layer regeneration is needed when creating a new layer or resizing the canvas.</para>
	/// <para>Changing this key makes the blazor UI framework re-run the layer rendering loop.</para>
	/// </summary>
	public readonly Guid ComponentKey = Guid.NewGuid();

	public event EventHandler? VisibilityChanged;

	public event EventHandler? OpacityChanged;

	private bool _isVisible;

	/// <summary>
	/// Determines whether the layer is visible (through CSS property) and can have actions performed on it.
	/// </summary>
	public bool IsVisible
	{
		get => _isVisible;
		set
		{
			_isVisible = value;
			VisibilityChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private int _opacity;

	/// <summary>
	/// Determines the opacity of the layer in % between 0 and 100.
	/// </summary>
	public int Opacity
	{
		get => _opacity;
		set
		{
			_opacity = value;
			OpacityChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	#endregion Properties

	#region Construction

	public LayerInfo()
	{ }

	public LayerInfo(string name, Size canvasSize, ImageSnapshot? imageData = null)
	{
		LayerId = Guid.NewGuid();
		Name = name;
		CanvasSize = canvasSize;
		IsVisible = true;
		Opacity = 100;

		ImageSnapshot = imageData ?? ImageSnapshot.Empty;
	}

	private LayerInfo(Guid layerId, string name, Size canvasSize, bool isVisible, int opacity, ImageSnapshot? imageData)
	{
		LayerId = layerId;
		Name = name;
		CanvasSize = canvasSize;
		IsVisible = isVisible;
		Opacity = opacity;

		ImageSnapshot = imageData ?? ImageSnapshot.Empty;
	}

	#endregion Construction

	#region Public methods

	public void ResizeCanvas(Size newSize, CanvasAnchor canvasAnchor)
	{
		CanvasSize = newSize;

		if (ImageSnapshot is not null)
		{
			int x = canvasAnchor switch
			{
				CanvasAnchor.TopCenter or
				CanvasAnchor.MiddleCenter or
				CanvasAnchor.BottomCenter => (CanvasSize.Width / 2) - (ImageSnapshot.Size.Width / 2),

				CanvasAnchor.TopRight or
				CanvasAnchor.MiddleRight or
				CanvasAnchor.BottomRight => CanvasSize.Width - ImageSnapshot.Size.Width,

				_ => 0
			};

			int y = canvasAnchor switch
			{
				CanvasAnchor.MiddleLeft or
				CanvasAnchor.MiddleCenter or
				CanvasAnchor.MiddleRight => (CanvasSize.Height / 2) - (ImageSnapshot.Size.Height / 2),

				CanvasAnchor.BottomLeft or
				CanvasAnchor.BottomCenter or
				CanvasAnchor.BottomRight => CanvasSize.Height - ImageSnapshot.Size.Height,

				_ => 0
			};

			if (canvasAnchor == CanvasAnchor.Stretched)
				ImageSnapshot.Size = CanvasSize;

			ImageSnapshot.CanvasPlacement = new Point(x, y);
		}
	}

	public void UpdateImageData(string dataUrl)
		=> ImageSnapshot = new ImageSnapshot(dataUrl, CanvasSize, new Point(0, 0));

	/// <summary>
	/// Creates a copy of the <c>LayerInfo</c> object with a new <c>ComponentKey</c>.
	/// </summary>
	public LayerInfo Clone()
		=> new(LayerId, Name, CanvasSize, IsVisible, Opacity, ImageSnapshot.Clone());

	#endregion Public methods
}
