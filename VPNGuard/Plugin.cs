using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using Version = System.Version;

namespace VPNGuard;

public class Plugin : Plugin<Config>
{
    public static Plugin Instance;
    public string BannedIpsFilePath;
    public string CheckedIpsFilePath;
    public override string Name => "VPNGuard";
    public override string Author => "MedveMarci";
    public override string Description => "vpn_guard";
    public override Version Version { get; } = new(1, 0, 0);
    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);
    private static bool PreRelease => false;

    public override void Enable()
    {
        Instance = this;
        var path = Path.Combine(PathManager.Configs.FullName, "VPNGuard");
        var bannedIpsName = Path.Combine(path, "BannedIps.txt");
        var checkedIpsName = Path.Combine(path, "CheckedIps.txt");

        if (!Directory.Exists(path))
        {
            Logger.Warn("VPNGuard directory does not exist. Creating...");
            Directory.CreateDirectory(path);
        }

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        if (!File.Exists(bannedIpsName))
            File.Create(bannedIpsName).Close();

        if (!File.Exists(checkedIpsName))
            File.Create(checkedIpsName).Close();

        BannedIpsFilePath = bannedIpsName;
        CheckedIpsFilePath = checkedIpsName;
        EventHandler.CheckedPlayers.Clear();
        EventHandler.BannedIps.Clear();
        EventHandler.CheckedPlayers.AddRange(File.ReadAllLines(CheckedIpsFilePath)
            .Where(line => !string.IsNullOrWhiteSpace(line)));
        EventHandler.BannedIps.AddRange(File.ReadAllLines(BannedIpsFilePath)
            .Where(line => !string.IsNullOrWhiteSpace(line)));
        PlayerEvents.Joined += EventHandler.OnJoined;
        ServerEvents.WaitingForPlayers += EventHandler.OnWaitingForPlayers;
    }

    public override void Disable()
    {
        try
        {
            var bannedIpsRead = File.ReadAllLines(Instance.BannedIpsFilePath).ToHashSet();
            var checkedIpsRead = File.ReadAllLines(Instance.CheckedIpsFilePath).ToHashSet();
            foreach (var checkedPlayer in EventHandler.CheckedPlayers.Where(checkedPlayer =>
                         !checkedIpsRead.Contains(checkedPlayer))) checkedIpsRead.Add(checkedPlayer);
            foreach (var bannedIp in EventHandler.BannedIps.Where(bannedIp => !bannedIpsRead.Contains(bannedIp)))
                bannedIpsRead.Add(bannedIp);
            File.WriteAllLines(Instance.BannedIpsFilePath, bannedIpsRead);
            File.WriteAllLines(Instance.CheckedIpsFilePath, checkedIpsRead);
        }
        catch (Exception e)
        {
            LogManager.Error($"Writing checked/banned file: {e}");
        }

        Instance = null;
        PlayerEvents.Joined -= EventHandler.OnJoined;
        ServerEvents.WaitingForPlayers -= EventHandler.OnWaitingForPlayers;
    }

    internal static async Task CheckForUpdatesAsync(Version currentVersion)
    {
        try
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"{Instance.Name}/{currentVersion}");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            var repo = $"MedveMarci/{Instance.Name}";
            var latestStableJson = await client.GetStringAsync($"https://api.github.com/repos/{repo}/releases/latest")
                .ConfigureAwait(false);
            var allReleasesJson = await client
                .GetStringAsync($"https://api.github.com/repos/{repo}/releases?per_page=20").ConfigureAwait(false);

            using var latestStableDoc = JsonDocument.Parse(latestStableJson);
            using var allReleasesDoc = JsonDocument.Parse(allReleasesJson);

            var latestStableRoot = latestStableDoc.RootElement;
            string stableTag = null;
            if (latestStableRoot.TryGetProperty("tag_name", out var tagProp))
                stableTag = tagProp.GetString();
            var stableVer = ParseVersion(stableTag);

            JsonElement? latestPre = null;
            Version preVer = null;
            string preTag = null;

            if (allReleasesDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                DateTime? bestPublishedAt = null;
                foreach (var rel in allReleasesDoc.RootElement.EnumerateArray()
                             .Where(rel => rel.ValueKind == JsonValueKind.Object))
                {
                    var draft = rel.TryGetProperty("draft", out var draftProp) &&
                                draftProp.ValueKind == JsonValueKind.True;
                    if (draft) continue;

                    var prerelease = rel.TryGetProperty("prerelease", out var preProp) &&
                                     preProp.ValueKind == JsonValueKind.True;
                    if (!prerelease) continue;

                    DateTime? publishedAt = null;
                    if (rel.TryGetProperty("published_at", out var pubProp))
                    {
                        var s = pubProp.GetString();
                        if (!string.IsNullOrWhiteSpace(s) && DateTime.TryParse(s, out var dt))
                            publishedAt = dt;
                    }

                    if (latestPre != null && (!publishedAt.HasValue ||
                                              (bestPublishedAt.HasValue && publishedAt.Value <= bestPublishedAt.Value)))
                        continue;
                    latestPre = rel;
                    bestPublishedAt = publishedAt;
                }
            }

            if (latestPre.HasValue)
            {
                if (latestPre.Value.TryGetProperty("tag_name", out var preTagProp))
                    preTag = preTagProp.GetString();
                preVer = ParseVersion(preTag);
            }

            var outdatedStable = stableVer != null && stableVer > currentVersion;
            var prereleaseNewer = preVer != null && preVer > currentVersion && !outdatedStable;

            if (outdatedStable)
                LogManager.Info(
                    $"A new {Instance.Name} version is available: {stableTag} (current {currentVersion}). Download: https://github.com/MedveMarci/{Instance.Name}/releases/latest",
                    ConsoleColor.DarkRed);
            else if (prereleaseNewer)
                LogManager.Info(
                    $"A newer pre-release is available: {preTag} (current {currentVersion}). Download: https://github.com/MedveMarci/{Instance.Name}/releases/tag/{preTag}",
                    ConsoleColor.DarkYellow);
            else
                LogManager.Info(
                    $"Thanks for using {Instance.Name} v{currentVersion}. To get support and latest news, join to my Discord Server: https://discord.gg/KmpA8cfaSA",
                    ConsoleColor.Blue);
            if (PreRelease)
                LogManager.Info(
                    "This is a pre-release version. There might be bugs, if you find one, please report it on GitHub or Discord.",
                    ConsoleColor.DarkYellow);
        }
        catch (Exception e)
        {
            LogManager.Error($"Version check failed.\n{e}");
        }
    }

    private static Version ParseVersion(string tag)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tag)) return null;
            var t = tag.Trim();
            if (t.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                t = t.Substring(1);

            var cut = t.IndexOfAny(new[] { '-', '+' });
            if (cut >= 0)
                t = t.Substring(0, cut);

            return Version.TryParse(t, out var v) ? v : null;
        }
        catch
        {
            return null;
        }
    }
}