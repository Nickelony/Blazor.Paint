using Blazor.Paint.Core.Enums;
using System.Drawing;

namespace Blazor.Paint.Core.Records;

public sealed record WorkspaceSnapshot
(
	ToolType UsedToolType,
	IEnumerable<LayerInfo> LayerInfos,
	IEnumerable<Guid> SelectedLayerIds,
	Rectangle Selection,
	Dictionary<Guid, string?> TransformedDataUrls
);
