using System.ComponentModel;
using Exiled.API.Interfaces;

namespace VPNGuard;

public class Config : IConfig
{
    public bool IsEnabled { get; set; } = true;
    public bool Debug { get; set; } = false;
    [Description("Put your IPHub ApiKey here to use the IPHub service.\n" +
                 "You can get your API key from https://iphub.info/")]
    public string ApiKey { get; set; } = "";
    public string KickReason { get; set; } =
        "Kicked for using a VPN or Proxy!\nIf you think this is a mistake, please contact a server staff!";
    public string Webhook { get; set; } = "";
}