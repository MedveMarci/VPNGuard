using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Console;
using VPNGuard.ApiFeatures;
using VPNGuard.VpnApi;

namespace VPNGuard;

public static class EventHandler
{
    public static readonly List<string> CheckedPlayers = [];
    public static readonly List<string> BannedIps = [];

    public static void OnJoined(PlayerJoinedEventArgs ev)
    {
        try
        {
            if (ev.Player.IsNorthwoodStaff) return;
            if (BannedIps.Contains(ev.Player.IpAddress))
            {
                ev.Player.Kick(VpnGuard.Singleton.Config.KickReason);
                return;
            }

            if (CheckedPlayers.Contains(ev.Player.IpAddress)) return;
            VpnChecker.Check(ev.Player.IpAddress, ev.Player);
        }
        catch (Exception e)
        {
            Logger.Error("Error in OnVerified: " + e);
        }
    }

    public static void OnWaitingForPlayers()
    {
        try
        {
            ApiManager.CheckForUpdates();
        }
        catch (Exception e)
        {
            Logger.Error("Error in OnWaitingForPlayers: " + e);
        }
    }
}