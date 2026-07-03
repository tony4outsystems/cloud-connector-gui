namespace CloudConnectorWindowsGui.App;

public sealed record BinaryVersionStatus(string? CurrentVersion, string LatestVersion, bool IsLatest);
