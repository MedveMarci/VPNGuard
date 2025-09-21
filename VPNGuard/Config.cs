using System.ComponentModel;

namespace VPNGuard;

public class Config
{
    [Description("Enable debug messages in the console.")]
    public bool Debug { get; set; } = false;

    [Description(
        "Put your IPHub ApiKey here to use the IPHub service. You can get your API key from https://iphub.info/")]
    public string ApiKey { get; set; } = "";

    [Description("The reason players will see when they are kicked by the plugin.")]
    public string KickReason { get; set; } =
        "Kicked for using a VPN or Proxy!\nIf you think this is a mistake, please contact a server staff!";

    [Description("Optional: Put your Discord webhook URL here to get notified when a player is kicked.")]
    public string Webhook { get; set; } = "";
}