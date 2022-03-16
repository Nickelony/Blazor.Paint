using Microsoft.JSInterop;
using System.Text.RegularExpressions;

namespace Blazor.Canvas.Contexts;

/// <summary>
/// Provides the 2D rendering context for the drawing surface of a <c>&lt;canvas&gt;</c> element.<br />
/// It is used for drawing shapes, text, images, and other objects.
/// </summary>
public class Context2D
{
	public BlazorCanvas Canvas { get; }

	protected string Id { get; }

	private IJSInProcessRuntime JS { get; }

	public Context2D(IJSRuntime js, string id, BlazorCanvas ownerCanvas)
	{
		Id = id;
		JS = (IJSInProcessRuntime)js;
		Canvas = ownerCanvas;
	}

	/* Properties */

	public async ValueTask<string> FillStyleAsync() => await JS.InvokeAsync<string>("getFillStyle", Id);
	public async Task FillStyleAsync(string style) => await JS.InvokeVoidAsync("setFillStyle", Id, style);

	public async ValueTask<double> GlobalAlphaAsync() => await JS.InvokeAsync<double>("getGlobalAlpha", Id);
	public async Task GlobalAlphaAsync(double alpha) => await JS.InvokeVoidAsync("setGlobalAlpha", Id, alpha);

	public async ValueTask<CompositeOperation> GlobalCompositeOperationAsync()
	{
		string jsString = await JS.InvokeAsync<string>("getGlobalCompositeOperation", Id);
		return Enum.Parse<CompositeOperation>(jsString.Replace("-", ""), true);
	}

	public async Task GlobalCompositeOperationAsync(CompositeOperation operation)
	{
		var regex = new Regex(@"
			(?<=[A-Z])(?=[A-Z][a-z]) |
			(?<=[^A-Z])(?=[A-Z]) |
			(?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

		string jsString = regex.Replace(Enum.GetName(operation)!, "-").ToLower();
		await JS.InvokeVoidAsync("setGlobalCompositeOperation", Id, jsString);
	}

	public async ValueTask<LineCap> LineCapAsync() => Enum.Parse<LineCap>(await JS.InvokeAsync<string>("getLineCap", Id), true);
	public async Task LineCapAsync(LineCap cap) => await JS.InvokeVoidAsync("setLineCap", Id, Enum.GetName(cap)!.ToLower());

	public async ValueTask<double> LineDashOffsetAsync() => await JS.InvokeAsync<double>("getLineDashOffset", Id);
	public async Task LineDashOffsetAsync(double offset) => await JS.InvokeVoidAsync("setLineDashOffset", Id, offset);

	public async ValueTask<LineJoin> LineJoinAsync() => Enum.Parse<LineJoin>(await JS.InvokeAsync<string>("getLineJoin", Id), true);
	public async Task LineJoinAsync(LineJoin join) => await JS.InvokeVoidAsync("setLineJoin", Id, Enum.GetName(join)!.ToLower());

	public async ValueTask<double> LineWidthAsync() => await JS.InvokeAsync<double>("getLineWidth", Id);
	public async Task LineWidthAsync(double width) => await JS.InvokeVoidAsync("setLineWidth", Id, width);

	public async ValueTask<string> StrokeStyleAsync() => await JS.InvokeAsync<string>("getStrokeStyle", Id);
	public async Task StrokeStyleAsync(string style) => await JS.InvokeVoidAsync("setStrokeStyle", Id, style);

	/* Methods */

	public void BeginPath()
		=> JS.InvokeVoid("doBeginPath", Id);

	public void LineTo(double x, double y)
		=> JS.InvokeVoid("doLineTo", Id, x, y);

	public void MoveTo(double x, double y)
		=> JS.InvokeVoid("doMoveTo", Id, x, y);

	public void Stroke()
		=> JS.InvokeVoid("doStroke", Id);

	/* Async methods */

	public async Task ArcAsync(double x, double y, double radius, double startAngle, double endAngle, bool counterclockwise = false)
		=> await JS.InvokeVoidAsync("doArc", Id, x, y, radius, startAngle, endAngle, counterclockwise);

	public async Task BeginPathAsync()
		=> await JS.InvokeVoidAsync("doBeginPath", Id);

	public async Task ClearRectAsync(double x, double y, double width, double height)
		=> await JS.InvokeVoidAsync("doClearRect", Id, x, y, width, height);

	public async Task ClosePathAsync()
		=> await JS.InvokeVoidAsync("doClosePath", Id);

	public async Task DrawImageAsync(string jsVariableName, double x, double y)
		=> await JS.InvokeVoidAsync("doDrawImage", Id, jsVariableName, x, y);

	public async Task DrawImageAsync(string jsVariableName, double x, double y, double width, double height)
		=> await JS.InvokeVoidAsync("doDrawImage2", Id, jsVariableName, x, y, width, height);

	public async Task EllipseAsync(double x, double y, double radiusX, double radiusY, double rotation, double startAngle, double endAngle, bool counterclockwise = false)
		=> await JS.InvokeVoidAsync("doEllipse", Id, x, y, radiusX, radiusY, rotation, startAngle, endAngle, counterclockwise);

	public async Task FillAsync()
		=> await JS.InvokeVoidAsync("doFill", Id);

	public async Task FillRectAsync(double x, double y, double width, double height)
		=> await JS.InvokeVoidAsync("doFillRect", Id, x, y, width, height);

	public async ValueTask<ImageData> GetImageDataAsync(double x, double y, double width, double height)
		=> await JS.InvokeAsync<ImageData>("getImageData", Id, x, y, width, height);

	public async ValueTask<double[]> GetLineDashAsync()
		=> await JS.InvokeAsync<double[]>("getLineDash", Id);

	public async Task LineToAsync(double x, double y)
		=> await JS.InvokeVoidAsync("doLineTo", Id, x, y);

	public async Task MoveToAsync(double x, double y)
		=> await JS.InvokeVoidAsync("doMoveTo", Id, x, y);

	public async Task PutImageDataAsync(ImageData data, double x, double y)
		=> await JS.InvokeVoidAsync("putImageData", Id, data.Data, data.Width, data.Height, data.ColorSpace, x, y);

	public async Task RectAsync(double x, double y, double width, double height)
		=> await JS.InvokeVoidAsync("doRect", Id, x, y, width, height);

	public async Task RestoreAsync()
		=> await JS.InvokeVoidAsync("doRestore", Id);

	public async Task RotateAsync(double angle)
		=> await JS.InvokeVoidAsync("doRotate", Id, angle);

	public async Task SaveAsync()
		=> await JS.InvokeVoidAsync("doSave", Id);

	public async Task SetLineDashAsync(params double[] segments)
		=> await JS.InvokeVoidAsync("setLineDash", Id, segments);

	public async Task StrokeAsync()
		=> await JS.InvokeVoidAsync("doStroke", Id);

	public async Task TranslateAsync(double x, double y)
		=> await JS.InvokeVoidAsync("doTranslate", Id, x, y);
}
