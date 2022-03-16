using Microsoft.AspNetCore.Components;

namespace Blazor.Paint.Components;

public partial class ModalDialog : ComponentBase
{
	[Parameter] public string Title { get; set; } = string.Empty;
	[Parameter] public bool ShowCloseButton { get; set; } = true;
	[Parameter] public RenderFragment? MainContent { get; set; }
	[Parameter] public string? ErrorMessage { get; set; }
	[Parameter] public RenderFragment? ButtonsFragment { get; set; }
	[Parameter] public EventCallback OnClose { get; set; }

	protected virtual async Task Close()
		=> await OnClose.InvokeAsync();
}
