namespace CloudConnectorWindowsGui;

internal sealed record SelfUpdateStatus(
    string CurrentVersion,
    string LatestVersion,
    bool IsUpdateAvailable,
    GitHubReleaseAsset? Asset);
