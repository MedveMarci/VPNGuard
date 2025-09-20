using System.ComponentModel;

namespace VPNGuard;

public class Config
{
    [Description("Put your IPHub ApiKey here to use the IPHub service. You can get your API key from https://iphub.info/")]
    public string ApiKey { get; set; } = "";
    public string KickReason { get; set; } =
        "Kicked for using a VPN or Proxy!\nIf you think this is a mistake, please contact a server staff!";
    public string Webhook { get; set; } = "";
}