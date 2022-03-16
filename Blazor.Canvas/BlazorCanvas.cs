using Blazor.Canvas.Contexts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Blazor.Canvas;

public class BlazorCanvas : ComponentBase
{
	[Inject] protected IJSRuntime JS { get; set; } = null!;

	[Parameter(CaptureUnmatchedValues = true)]
	public Dictionary<string, object>? AdditionalAttributes { get; set; }

	public double Width
	{
		get
		{
			if (AdditionalAttributes is not null
			&& AdditionalAttributes.TryGetValue("width", out object? widthObject)
			&& double.TryParse(widthObject.ToString(), out double width))
				return width;

			return 0;
		}
	}

	public double Height
	{
		get
		{
			if (AdditionalAttributes is not null
			&& AdditionalAttributes.TryGetValue("height", out object? heightObject)
			&& double.TryParse(heightObject.ToString(), out double height))
				return height;

			return 0;
		}
	}

	private readonly string id;

	public BlazorCanvas()
		=> id = IdUtils.NewId("canvas");

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenElement(0, "canvas");
		builder.AddAttribute(1, "id", id);
		builder.AddMultipleAttributes(2, AdditionalAttributes);
		builder.CloseElement();
	}

	public async ValueTask<Context2D> GetContext2DAsync()
	{
		string contextId = IdUtils.NewId("ctx");
		await JS.InvokeVoidAsync("initContext2D", id, contextId);

		return new Context2D(JS, contextId, this);
	}

	public async ValueTask<string> ToDataURLAsync()
		=> await JS.InvokeAsync<string>("toDataURL", id);

	public async ValueTask<string> ToDataURLAsync(string format)
		=> await JS.InvokeAsync<string>("toDataURL2", id, format);
}
