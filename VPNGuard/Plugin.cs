using System;
using System.IO;
using System.Linq;
using Exiled.API.Features;
using Player = Exiled.Events.Handlers.Player;

namespace VPNGuard;

public class Plugin : Plugin<Config>
{
    public static Plugin PluginInstance;
    public string BannedIpsFilePath;
    public string CheckedIpsFilePath;
    public override string Name => "VPNGuard";
    public override string Author => "MedveMarci";
    public override string Prefix => "vpn_guard";
    public override Version Version { get; } = new(1, 0, 0);

    public override void OnEnabled()
    {
        PluginInstance = this;
        var path = Path.Combine(Paths.Configs, "VPNGuard");
        var bannedIpsName = Path.Combine(path, "BannedIps.txt");
        var checkedIpsName = Path.Combine(path, "CheckedIps.txt");

        if (!Directory.Exists(path))
        {
            Log.Warn("VPNGuard directory does not exist. Creating...");
            Directory.CreateDirectory(path);
        }

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        if (!File.Exists(bannedIpsName))
            File.Create(bannedIpsName).Close();

        if (!File.Exists(checkedIpsName))
            File.Create(checkedIpsName).Close();

        BannedIpsFilePath = bannedIpsName;
        CheckedIpsFilePath = checkedIpsName;
        EventHandler.CheckedPlayers.Clear();
        EventHandler.BannedIps.Clear();
        EventHandler.CheckedPlayers.AddRange(File.ReadAllLines(CheckedIpsFilePath)
            .Where(line => !string.IsNullOrWhiteSpace(line)));
        EventHandler.BannedIps.AddRange(File.ReadAllLines(BannedIpsFilePath) 
            .Where(line => !string.IsNullOrWhiteSpace(line)));
        Player.Verified += EventHandler.OnVerified;
        base.OnEnabled();
    }

    public override void OnDisabled()
    {
        try
        {
            var bannedIpsRead = File.ReadAllLines(PluginInstance.BannedIpsFilePath).ToHashSet();
            var checkedIpsRead = File.ReadAllLines(PluginInstance.CheckedIpsFilePath).ToHashSet();
            foreach (var checkedPlayer in EventHandler.CheckedPlayers.Where(checkedPlayer =>
                         !checkedIpsRead.Contains(checkedPlayer))) checkedIpsRead.Add(checkedPlayer);
            foreach (var bannedIp in EventHandler.BannedIps.Where(bannedIp => !bannedIpsRead.Contains(bannedIp)))
                bannedIpsRead.Add(bannedIp);
            File.WriteAllLines(PluginInstance.BannedIpsFilePath, bannedIpsRead);
            File.WriteAllLines(PluginInstance.CheckedIpsFilePath, checkedIpsRead);
        }
        catch (Exception e)
        {
            Log.Error($"Round End:\n{e}");
        }

        PluginInstance = null;
        Player.Verified -= EventHandler.OnVerified;
        base.OnDisabled();
    }
}