namespace Blazor.Paint.Core.Records;

public sealed record WorkspaceAction(
	string Label,
	WorkspaceSnapshot ResultingWorkspaceSnapshot
);
