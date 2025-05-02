using System;
using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using VPNGuard.VpnApi;

namespace VPNGuard;

public class EventHandler
{
    public static List<string> CheckedPlayers = [];
    public static List<string> BannedIps = [];
    public static bool IsCheck = true;

    public static void OnVerified(VerifiedEventArgs ev)
    {
        if (!IsCheck) return;
        try
        {
            if (ev.Player.UserId.Contains("@northwood")) return;
            if (BannedIps.Contains(ev.Player.IPAddress))
            {
                ev.Player.Kick(Plugin.PluginInstance.Config.KickReason);
                return;
            }

            if (CheckedPlayers.Contains(ev.Player.IPAddress)) return;
            IpHub.CheckIpHub(ev.Player.IPAddress, ev.Player);
        }
        catch (Exception e)
        {
            Log.Error("Error in OnPreAuthenticating: " + e);
        }
    }
}