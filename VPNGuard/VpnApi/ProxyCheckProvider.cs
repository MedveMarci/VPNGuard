using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using VPNGuard.ApiFeatures;

namespace VPNGuard.VpnApi;

public sealed class ProxyCheckProvider(HttpClient client)
{
    public static string Name => "proxycheck.io";

    public async Task<VpnCheckResult> CheckAsync(string ipAddress)
    {
        var config = VpnGuard.Singleton.Config;
        var key = config.ProxyCheckApiKey;

        var url = $"https://proxycheck.io/v3/{Uri.EscapeDataString(ipAddress)}";
        if (!string.IsNullOrWhiteSpace(key))
            url += $"?key={Uri.EscapeDataString(key)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        LogManager.Debug($"proxycheck.io response: {body}");

        if (!response.IsSuccessStatusCode)
            return VpnCheckResult.Failed(Name, $"HTTP {(int)response.StatusCode} {response.StatusCode}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var status = GetString(root, "status");
        if (status is not ("ok" or "warning"))
            return VpnCheckResult.Failed(Name, $"status={status} ({GetString(root, "message") ?? "no message"})");

        if (!root.TryGetProperty(ipAddress, out var ipData) || ipData.ValueKind != JsonValueKind.Object)
            return VpnCheckResult.Failed(Name, "IP object missing from response.");

        if (!ipData.TryGetProperty("detections", out var detections) || detections.ValueKind != JsonValueKind.Object)
            return VpnCheckResult.Failed(Name, "Detections object missing from response.");

        return Parse(ipAddress, ipData, detections, config);
    }

    private static VpnCheckResult Parse(string ipAddress, JsonElement ipData, JsonElement detections, Config config)
    {
        var flags = new (string Name, bool Value)[]
        {
            ("Proxy", GetBool(detections, "proxy")),
            ("VPN", GetBool(detections, "vpn")),
            ("Tor", GetBool(detections, "tor")),
            ("Compromised", GetBool(detections, "compromised")),
            ("Scraper", GetBool(detections, "scraper"))
        };
        var hosting = GetBool(detections, "hosting");
        var risk = GetInt(detections, "risk");

        var isVpn = false;
        var reasons = new List<string>();
        foreach (var (name, value) in flags)
            if (value)
            {
                isVpn = true;
                reasons.Add(name);
            }

        if (hosting)
        {
            reasons.Add("Hosting");
            if (config.BlockHostingProviders) isVpn = true;
        }

        var hasNetwork = ipData.TryGetProperty("network", out var network) && network.ValueKind == JsonValueKind.Object;
        var hasLocation = ipData.TryGetProperty("location", out var location) &&
                          location.ValueKind == JsonValueKind.Object;

        return new VpnCheckResult
        {
            Success = true,
            IsVpn = isVpn,
            Provider = Name,
            Ip = ipAddress,
            Hostname = hasNetwork ? GetString(network, "hostname") ?? GetString(network, "provider") : null,
            CountryName = hasLocation ? GetString(location, "country_name") : null,
            CountryCode = hasLocation ? GetString(location, "country_code") : null,
            Isp = hasNetwork ? GetString(network, "provider") ?? GetString(network, "organisation") : null,
            Asn = hasNetwork ? GetString(network, "asn") : null,
            Operator = GetOperatorName(ipData),
            DetectionReason = BuildReason(reasons, risk, isVpn)
        };
    }

    private static string GetOperatorName(JsonElement ipData)
    {
        return ipData.TryGetProperty("operator", out var op) && op.ValueKind == JsonValueKind.Object
            ? GetString(op, "name")
            : null;
    }

    private static string BuildReason(List<string> reasons, int risk, bool isVpn)
    {
        var joined = reasons.Count > 0 ? string.Join(", ", reasons) : isVpn ? "Proxy/VPN" : "Clean";
        return risk >= 0 ? $"{joined} (risk {risk})" : joined;
    }

    private static string GetString(JsonElement obj, string name)
    {
        return obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    }

    private static bool GetBool(JsonElement obj, string name)
    {
        return obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;
    }

    private static int GetInt(JsonElement obj, string name)
    {
        return obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)
            ? i
            : -1;
    }
}