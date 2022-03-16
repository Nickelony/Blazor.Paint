namespace Blazor.Canvas;

internal static class IdUtils
{
	public static string NewId(string prefix)
		=> prefix.TrimEnd('_') + '_' + Guid.NewGuid().ToString().Replace('-', '_');
}
