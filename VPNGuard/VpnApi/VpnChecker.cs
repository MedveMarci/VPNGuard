using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LabApi.Features.Wrappers;
using VPNGuard.ApiFeatures;

namespace VPNGuard.VpnApi;

public static class VpnChecker
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly ProxyCheckProvider Provider = new(Client);

    public static async void Check(string ipAddress, Player player)
    {
        try
        {
            VpnCheckResult result;
            try
            {
                result = await Provider.CheckAsync(ipAddress);
            }
            catch (Exception ex)
            {
                LogManager.Error($"An exception occurred whilst checking {ProxyCheckProvider.Name}. Exception: {ex}.");
                return;
            }

            if (!result.Success)
            {
                LogManager.Error($"VPN check via {result.Provider} failed for {ipAddress}: {result.Error}");
                return;
            }

            if (result.IsVpn && IsAllowlisted(result, out var allowMatch))
            {
                LogManager.Debug(
                    $"{ipAddress} ({player.Nickname}) was flagged by {result.Provider} but matches the allowlist ({allowMatch}); allowing.");
                MarkChecked(player);
                return;
            }

            if (result.IsVpn)
            {
                LogManager.Debug(
                    $"{ipAddress} ({player.Nickname}) flagged as VPN/proxy by {result.Provider} ({result.DetectionReason}). Kicking...");
                await KickAndReport(player, result);
            }
            else
            {
                LogManager.Debug($"{ipAddress} ({player.Nickname}) is not a detectable VPN ({result.Provider}).");
                MarkChecked(player);
            }
        }
        catch (Exception ex)
        {
            LogManager.Error($"An exception occurred in the VPN check pipeline. Exception: {ex}.");
        }
    }

    private static async Task KickAndReport(Player player, VpnCheckResult result)
    {
        EventHandler.BannedIps.Add(player.IpAddress);
        player.Kick(VpnGuard.Singleton.Config.KickReason);

        try
        {
            AppendUnique(VpnGuard.Singleton.BannedIpsFilePath, player.IpAddress);
        }
        catch (Exception e)
        {
            LogManager.Error($"Writing banned file: {e}");
        }

        try
        {
            await SendWebhookAsync(player, result);
        }
        catch (Exception e)
        {
            LogManager.Error($"Sending webhook: {e}");
        }
    }

    private static bool IsAllowlisted(VpnCheckResult result, out string match)
    {
        match = null;
        var config = VpnGuard.Singleton.Config;

        if (config.AllowedIsps != null)
            foreach (var entry in config.AllowedIsps)
            {
                if (string.IsNullOrWhiteSpace(entry)) continue;
                if (Contains(result.Isp, entry) || Contains(result.Hostname, entry))
                {
                    match = $"ISP~'{entry}'";
                    return true;
                }
            }

        if (config.AllowedAsns != null && !string.IsNullOrEmpty(result.Asn))
        {
            var resultAsn = AsnDigits(result.Asn);
            if (!string.IsNullOrEmpty(resultAsn))
                foreach (var entry in config.AllowedAsns)
                {
                    if (string.IsNullOrWhiteSpace(entry)) continue;
                    if (AsnDigits(entry) == resultAsn)
                    {
                        match = $"ASN {entry}";
                        return true;
                    }
                }
        }

        return false;
    }

    private static bool Contains(string haystack, string needle)
    {
        return !string.IsNullOrEmpty(haystack) && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string AsnDigits(string asn)
    {
        var token = asn.Trim().Split(' ')[0];
        return new string([.. token.Where(char.IsDigit)]);
    }

    private static void MarkChecked(Player player)
    {
        EventHandler.CheckedPlayers.Add(player.IpAddress);
        try
        {
            AppendUnique(VpnGuard.Singleton.CheckedIpsFilePath, player.IpAddress);
        }
        catch (Exception e)
        {
            LogManager.Error($"Writing checked file: {e}");
        }
    }

    private static void AppendUnique(string filePath, string ip)
    {
        var lines = File.ReadAllLines(filePath).ToHashSet();
        if (lines.Add(ip))
            File.AppendAllText(filePath, ip + Environment.NewLine);
    }

    private static string StripRichTags(string text)
    {
        return string.IsNullOrEmpty(text) ? text : Regex.Replace(text, "<[^>]*>", "");
    }

    private static async Task SendWebhookAsync(Player player, VpnCheckResult result)
    {
        var webhook = VpnGuard.Singleton.Config.Webhook;
        if (string.IsNullOrWhiteSpace(webhook))
            return;

        var country = string.IsNullOrEmpty(result.CountryName)
            ? "Unknown"
            : $"{result.CountryName} ({result.CountryCode})";

        var value =
            $"\n**`🔢`Player:** {player.Nickname} ({player.UserId})" +
            $"\n`🔗` **IP:** {result.Ip}{(string.IsNullOrEmpty(result.Hostname) ? "" : $" ({result.Hostname})")}" +
            $"\n`🗺️` **Country:** {country}" +
            $"\n**ISP:** {result.Isp}" +
            $"\n**ASN:** {result.Asn}" +
            (string.IsNullOrEmpty(result.Operator) ? "" : $"\n**VPN/Operator:** {result.Operator}") +
            $"\n**Detected by:** {result.Provider} — {result.DetectionReason}";

        var webhookData = new
        {
            username = "VPNGuard",
            embeds = new[]
            {
                new
                {
                    title = "🖧 VPN Detected",
                    fields = new[]
                    {
                        new
                        {
                            name = $"{player.Nickname} has been kicked for using a VPN!",
                            value
                        }
                    },
                    color = 16711680,
                    footer = new { text = StripRichTags(Server.ServerListName) }
                }
            }
        };

        using var content = new StringContent(JsonSerializer.Serialize(webhookData), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync(webhook, content);
        if (!response.IsSuccessStatusCode)
        {
            var responseMessage = await response.Content.ReadAsStringAsync();
            LogManager.Error(
                $"[{(int)response.StatusCode} - {response.StatusCode}] A non-successful status code was returned by Discord when trying to post to webhook regarding {player.UserId}'s ({player.IpAddress}) kick. Response Message: {responseMessage}.");
        }
    }
}