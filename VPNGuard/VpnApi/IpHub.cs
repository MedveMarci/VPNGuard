using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Exiled.API.Features;
using Utf8Json;

namespace VPNGuard.VpnApi;

public class IpHub
{
    private static readonly HttpClient Client = new();

    public static async void CheckIpHub(string ipAddress, Player player)
    {
        try
        {
            if (!Client.DefaultRequestHeaders.Contains("x-key"))
                Client.DefaultRequestHeaders.Add("x-key", Plugin.PluginInstance.Config.ApiKey);
            var webRequest = await Client.GetAsync($"https://v2.api.iphub.info/ip/{ipAddress}");
            if (!webRequest.IsSuccessStatusCode)
            {
                var errorResponse = await webRequest.Content.ReadAsStringAsync();
                Log.Error(webRequest.StatusCode == (HttpStatusCode)429
                    ? "VPN check could not complete. You have reached your API key's limit."
                    : $"VPN API connection error: {webRequest.StatusCode} - {errorResponse}");
                return;
            }

            var apiResponse = await webRequest.Content.ReadAsStringAsync();
            var ipHubApiResponse = JsonSerializer.Deserialize<IpHubApiResponse>(apiResponse);
            Log.Debug($"IPHub API response: {apiResponse}");
            if (ipHubApiResponse == null)
            {
                Log.Error("IPHub API response was null.");
                return;
            }

            if (ipHubApiResponse.isp.ToLower().Contains("vpn") || ipHubApiResponse.isp.ToLower().Contains("proton") || ipHubApiResponse.hostname.ToLower().Contains("vpn") || ipHubApiResponse.hostname.ToLower().Contains("proton"))
            {
                Log.Debug($"{ipAddress} ({player.Nickname}) is a detectable VPN. Kicking...");
                EventHandler.BannedIps.Add(player.IPAddress);
                player.Kick(Plugin.PluginInstance.Config.KickReason);
                try
                {
                    var bannedIpsRead = File.ReadAllLines(Plugin.PluginInstance.BannedIpsFilePath).ToHashSet();

                    if (!bannedIpsRead.Contains(player.IPAddress))
                    {
                        File.AppendAllText(Plugin.PluginInstance.BannedIpsFilePath,
                            player.IPAddress + Environment.NewLine);
                        bannedIpsRead.Add(player.IPAddress);
                    }

                    File.WriteAllLines(Plugin.PluginInstance.BannedIpsFilePath, bannedIpsRead);
                    var webhookData = new
                    {
                        username = "VPNGuard",
                        embeds = new[]
                        {
                            new
                            {
                                title = "\ud83d\udda7 VPN Detected",
                                fields = new[]
                                {
                                    new
                                    {
                                        name = $"{player.Nickname} has been kicked for using a VPN!",
                                        value =
                                            $"\n**`\ud83d\udd22`Player:** {player.Nickname} ({player.UserId})\n`\ud83d\udd17` **IP:** {ipHubApiResponse.ip} ({ipHubApiResponse.hostname})\n`\ud83d\uddfa\ufe0f` **Country:** {ipHubApiResponse.countryName} ({ipHubApiResponse.countryCode})\n**ISP:** {ipHubApiResponse.isp}\n**ASN:** {ipHubApiResponse.asn}"
                                    }
                                },
                                color = 16711680
                            }
                        }
                    };
                    StringContent webhookStringContent =
                        new(Encoding.UTF8.GetString(JsonSerializer.Serialize(webhookData)), Encoding.UTF8,
                            "application/json");
                    var responseMessage =
                        await Client.PostAsync(Plugin.PluginInstance.Config.Webhook, webhookStringContent);
                    var responseMessageString = await responseMessage.Content.ReadAsStringAsync();

                    if (!responseMessage.IsSuccessStatusCode)
                        Log.Error(
                            $"[{(int)responseMessage.StatusCode} - {responseMessage.StatusCode}] A non-successful status code was returned by Discord when trying to post to webhook regarding {player.UserId}'s ({player.IPAddress}) kick. Response Message: {responseMessageString}.");
                }
                catch (Exception e)
                {
                    Log.Error($"Writing checked file: {e}");
                }

                return;
            }

            switch (ipHubApiResponse.block)
            {
                case 1:
                {
                    Log.Debug($"{ipAddress} ({player.Nickname}) is a detectable VPN. Kicking...");
                    EventHandler.BannedIps.Add(player.IPAddress);
                    player.Kick(Plugin.PluginInstance.Config.KickReason);
                    try
                    {
                        var bannedIpsRead = File.ReadAllLines(Plugin.PluginInstance.BannedIpsFilePath).ToHashSet();

                        if (!bannedIpsRead.Contains(player.IPAddress))
                        {
                            File.AppendAllText(Plugin.PluginInstance.BannedIpsFilePath,
                                player.IPAddress + Environment.NewLine);
                            bannedIpsRead.Add(player.IPAddress);
                        }

                        File.WriteAllLines(Plugin.PluginInstance.BannedIpsFilePath, bannedIpsRead);
                        var webhookData = new
                        {
                            username = "VPNGuard",
                            embeds = new[]
                            {
                                new
                                {
                                    title = "\ud83d\udda7 VPN Detected",
                                    fields = new[]
                                    {
                                        new
                                        {
                                            name = $"{player.Nickname} has been kicked for using a VPN.",
                                            value =
                                                $"\n**`\ud83d\udd22`Player:** {player.Nickname} ({player.UserId})\n`\ud83d\udd17` **IP:** {ipHubApiResponse.ip} ({ipHubApiResponse.hostname})\n`\ud83d\uddfa\ufe0f` **Country:** {ipHubApiResponse.countryName} ({ipHubApiResponse.countryCode})\n**ISP:** {ipHubApiResponse.isp}\n**ASN:** {ipHubApiResponse.asn}"
                                        }
                                    },
                                    color = 16711680
                                }
                            }
                        };
                        StringContent webhookStringContent =
                            new(Encoding.UTF8.GetString(JsonSerializer.Serialize(webhookData)), Encoding.UTF8,
                                "application/json");
                        var responseMessage =
                            await Client.PostAsync(Plugin.PluginInstance.Config.Webhook, webhookStringContent);
                        var responseMessageString = await responseMessage.Content.ReadAsStringAsync();

                        if (!responseMessage.IsSuccessStatusCode)
                            Log.Error(
                                $"[{(int)responseMessage.StatusCode} - {responseMessage.StatusCode}] A non-successful status code was returned by Discord when trying to post to webhook regarding {player.UserId}'s ({player.IPAddress}) kick. Response Message: {responseMessageString}.");
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Writing checked file: {e}");
                    }

                    return;
                }
                case 0:
                case 2:
                {
                    Log.Debug($"{ipAddress} ({player.Nickname}) is not a detectable VPN.");
                    EventHandler.CheckedPlayers.Add(player.IPAddress);
                    try
                    {
                        var checkedIpsRead = File.ReadAllLines(Plugin.PluginInstance.CheckedIpsFilePath).ToHashSet();

                        if (!checkedIpsRead.Contains(player.IPAddress))
                        {
                            File.AppendAllText(Plugin.PluginInstance.CheckedIpsFilePath,
                                player.IPAddress + Environment.NewLine);
                            checkedIpsRead.Add(player.IPAddress);
                        }

                        File.WriteAllLines(Plugin.PluginInstance.CheckedIpsFilePath, checkedIpsRead);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Writing checked file: {e}");
                    }

                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"An exception occurred whilst checking IPHub. Exception: {ex}.");
        }
    }

    public class IpHubApiResponse 
    {
        public string ip { get; set; }
        public string countryCode { get; set; }
        public string countryName { get; set; }
        public int asn { get; set; }
        public string isp { get; set; }
        public int block { get; set; }
        public string hostname { get; set; }
        
        public IpHubApiResponse() { }
    }
}