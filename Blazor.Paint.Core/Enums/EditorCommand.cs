namespace Blazor.Paint.Core.Enums;

public enum EditorCommand
{
	None,

	// File:

	New,
	Open,
	SaveAs,
	ExportAs,
	Import,

	// Edit:

	Undo,
	Redo,
	Cut,
	Copy,
	Paste,

	// Select:

	SelectAll,
	ClearSelection,

	// Document:

	ResizeDocument,
	ResizeCanvas,
	ToggleTransparency,

	// Layer:

	NewLayer,
	NewFillLayer,

	// Other:

	RemoveLayer,
	RenameLayer,
	MoveLayerUp,
	MoveLayerDown,
	MergeSelectedLayers
}
