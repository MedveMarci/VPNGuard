using System.Collections.Generic;
using System.ComponentModel;

namespace VPNGuard;

public class Config
{
    [Description("Enable debug messages in the console.")]
    public bool Debug { get; set; } = false;

    [Description(
        "Optional API key for proxycheck.io. Leave empty for the free keyless tier " +
        "(100/day), or get a free key at https://proxycheck.io/ for 1000/day.")]
    public string ProxyCheckApiKey { get; set; } = "";

    [Description(
        "Catches more VPN endpoints but can produce false positives on some mobile/business connections. " +
        "Cloud gaming (GeForce NOW, Xbox Cloud, etc.) runs from datacenters, so keep the allowlist below in sync when enabling this.")]
    public bool BlockHostingProviders { get; set; } = false;

    [Description(
        "Allowlist of ISP/provider name substrings (case-insensitive) that are NEVER kicked, even if flagged. " +
        "Matched against the ISP, provider and hostname. Use this for legitimate cloud gaming services.")]
    public List<string> AllowedIsps { get; set; } =
    [
        "nvidia",
        "geforce now",
        "microsoft",
        "shadow",
        "blade group",
        "boosteroid"
    ];

    [Description(
        "Allowlist of ASN numbers that are NEVER kicked, even if flagged (e.g. \"AS20347\" or \"20347\"). " +
        "Add the ASNs of cloud gaming providers you want to allow.")]
    public List<string> AllowedAsns { get; set; } = [];

    [Description("The reason players will see when they are kicked by the plugin.")]
    public string KickReason { get; set; } =
        "Kicked for using a VPN or Proxy!\nIf you think this is a mistake, please contact a server staff!";

    [Description("Optional: Put your Discord webhook URL here to get notified when a player is kicked.")]
    public string Webhook { get; set; } = "";
}