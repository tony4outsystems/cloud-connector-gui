namespace CloudConnectorWindowsGui;

internal sealed record BinaryVersionStatus(string? CurrentVersion, string LatestVersion, bool IsLatest);
