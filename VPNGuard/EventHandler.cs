using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Console;
using VPNGuard.VpnApi;

namespace VPNGuard;

public class EventHandler
{
    public static List<string> CheckedPlayers = [];
    public static List<string> BannedIps = [];

    public static void OnJoined(PlayerJoinedEventArgs ev)
    {
        try
        {
            if (ev.Player.IsNorthwoodStaff) return;
            if (BannedIps.Contains(ev.Player.IpAddress))
            {
                ev.Player.Kick(Plugin.PluginInstance.Config.KickReason);
                return;
            }

            if (CheckedPlayers.Contains(ev.Player.IpAddress)) return;
            IpHub.CheckIpHub(ev.Player.IpAddress, ev.Player);
        }
        catch (Exception e)
        {
            Logger.Error("Error in OnVerified: " + e);
        }
    }
}