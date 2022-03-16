using System.Drawing;
using System.Xml.Serialization;

namespace Blazor.Paint.Core;

[XmlRoot]
public sealed class Document
{
	public readonly Guid Id = Guid.NewGuid();

	[XmlArray]
	public List<LayerInfo> LayerInfos { get; set; } = new();

	[XmlAttribute]
	public bool IsTransparentBackground { get; set; }

	[XmlIgnore]
	public Size CanvasSize => LayerInfos.FirstOrDefault()?.CanvasSize ?? new(0, 0);

	[XmlIgnore]
	public string FileName { get; set; } = "Untitled.blp";
}
