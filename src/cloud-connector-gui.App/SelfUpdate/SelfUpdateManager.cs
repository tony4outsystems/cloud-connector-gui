using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace CloudConnectorGui.App;

public sealed class SelfUpdateManager
{
    private const string RepositoryOwner = "tony4outsystems";
    private const string RepositoryName = "cloud-connector-gui";

    private readonly GitHubReleaseClient releaseClient;

    public SelfUpdateManager()
        : this(new GitHubReleaseClient(RepositoryOwner, RepositoryName, "No stable cloud-connector-gui release was found."))
    {
    }

    public SelfUpdateManager(GitHubReleaseClient releaseClient)
    {
        this.releaseClient = releaseClient;
    }

    public string CurrentVersion => GetCurrentVersion();

    public async Task<SelfUpdateStatus> GetUpdateStatusAsync(CancellationToken cancellationToken = default)
    {
        var release = await releaseClient.GetLatestReleaseAsync(cancellationToken).ConfigureAwait(false);
        var isUpdateAvailable = GitHubReleaseClient.IsVersionNewer(release.TagName, CurrentVersion);
        var asset = isUpdateAvailable ? SelectReleaseAsset(release) : null;
        return new SelfUpdateStatus(CurrentVersion, release.TagName, isUpdateAvailable, asset);
    }

    public async Task ApplyUpdateAndRestartAsync(SelfUpdateStatus status, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!status.IsUpdateAvailable || status.Asset is null)
        {
            return;
        }

        progress?.Report($"Downloading cloud-connector-gui {status.LatestVersion}...");
        var archivePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-{status.Asset.Name}");
        var stagingDirectory = Path.Combine(Path.GetTempPath(), $"cloud-connector-gui-{Guid.NewGuid():N}");

        await releaseClient.DownloadFileAsync(status.Asset.BrowserDownloadUrl, archivePath, cancellationToken).ConfigureAwait(false);
        await VerifyDigestAsync(status.Asset, archivePath, cancellationToken).ConfigureAwait(false);

        progress?.Report("Preparing GUI update...");
        Directory.CreateDirectory(stagingDirectory);
        ExtractArchive(archivePath, stagingDirectory);

        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var executableName = isWindows ? "cloud-connector-gui.exe" : "cloud-connector-gui";
        var executablePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, executableName);
        var installDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        progress?.Report("Restarting to finish GUI update...");
        if (isWindows)
        {
            var scriptPath = Path.Combine(Path.GetTempPath(), $"cloud-connector-gui-update-{Guid.NewGuid():N}.ps1");
            File.WriteAllText(scriptPath, CreateWindowsUpdateScript(Environment.ProcessId, stagingDirectory, installDirectory, executablePath, archivePath));

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        else
        {
            var scriptPath = Path.Combine(Path.GetTempPath(), $"cloud-connector-gui-update-{Guid.NewGuid():N}.sh");
            File.WriteAllText(scriptPath, CreateUnixUpdateScript(Environment.ProcessId, stagingDirectory, installDirectory, executablePath, archivePath));
            File.SetUnixFileMode(scriptPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

            Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/sh",
                Arguments = $"\"{scriptPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }

        Environment.Exit(0);
    }

    private static GitHubReleaseAsset SelectReleaseAsset(GitHubRelease release)
    {
        var rid = GetRuntimeIdentifier();
        var assetName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $"cloud-connector-gui-{rid}.zip"
            : $"cloud-connector-gui-{rid}.tar.gz";

        return release.Assets.FirstOrDefault(asset => string.Equals(asset.Name, assetName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Release {release.TagName} does not include {assetName}.");
    }

    private static string GetRuntimeIdentifier()
    {
        var architecture = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return $"win-{architecture}";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return $"osx-{architecture}";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return $"linux-{architecture}";
        }

        throw new PlatformNotSupportedException($"Unsupported platform: {RuntimeInformation.OSDescription}.");
    }

    private static void ExtractArchive(string archivePath, string destinationDirectory)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ZipFile.ExtractToDirectory(archivePath, destinationDirectory, overwriteFiles: true);
            return;
        }

        using var file = File.OpenRead(archivePath);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        TarFile.ExtractToDirectory(gzip, destinationDirectory, overwriteFiles: true);
    }

    private static async Task VerifyDigestAsync(GitHubReleaseAsset asset, string archivePath, CancellationToken cancellationToken)
    {
        if (asset.Digest is null || !asset.Digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await using var stream = File.OpenRead(archivePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        var actual = Convert.ToHexString(hash).ToLowerInvariant();
        var expected = asset.Digest["sha256:".Length..].ToLowerInvariant();
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Downloaded GUI update failed SHA-256 verification.");
        }
    }

    private static string GetCurrentVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0";
        var metadataIndex = version.IndexOf('+', StringComparison.Ordinal);
        return metadataIndex >= 0 ? version[..metadataIndex] : version;
    }

    private static string CreateWindowsUpdateScript(int processId, string sourceDirectory, string installDirectory, string executablePath, string archivePath)
    {
        return $$"""
$ErrorActionPreference = 'Stop'
$processId = {{processId}}
$sourceDirectory = '{{EscapePowerShellString(sourceDirectory)}}'
$installDirectory = '{{EscapePowerShellString(installDirectory)}}'
$executablePath = '{{EscapePowerShellString(executablePath)}}'
$archivePath = '{{EscapePowerShellString(archivePath)}}'

Wait-Process -Id $processId -ErrorAction SilentlyContinue
Copy-Item -Path (Join-Path $sourceDirectory '*') -Destination $installDirectory -Recurse -Force
Start-Process -FilePath $executablePath
Remove-Item -Path $archivePath -Force -ErrorAction SilentlyContinue
Remove-Item -Path $sourceDirectory -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path $MyInvocation.MyCommand.Path -Force -ErrorAction SilentlyContinue
""";
    }

    private static string CreateUnixUpdateScript(int processId, string sourceDirectory, string installDirectory, string executablePath, string archivePath)
    {
        return $$"""
#!/bin/sh
set -e
process_id={{processId}}
source_directory='{{EscapeShellString(sourceDirectory)}}'
install_directory='{{EscapeShellString(installDirectory)}}'
executable_path='{{EscapeShellString(executablePath)}}'
archive_path='{{EscapeShellString(archivePath)}}'

while kill -0 "$process_id" 2>/dev/null; do
    sleep 0.2
done

cp -Rf "$source_directory"/. "$install_directory"/
chmod +x "$executable_path"
nohup "$executable_path" >/dev/null 2>&1 &
rm -f "$archive_path"
rm -rf "$source_directory"
rm -f "$0"
""";
    }

    private static string EscapePowerShellString(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }

    private static string EscapeShellString(string value)
    {
        return value.Replace("'", "'\\''", StringComparison.Ordinal);
    }
}
