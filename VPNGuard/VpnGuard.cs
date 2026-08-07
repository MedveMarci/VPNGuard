using System;
using System.IO;
using System.Linq;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using VPNGuard.ApiFeatures;
using Version = System.Version;

namespace VPNGuard;

public class VpnGuard : Plugin<Config>
{
    public static VpnGuard Singleton;
    public string BannedIpsFilePath;
    public string CheckedIpsFilePath;
    public override string Name => "VPNGuard";
    public override string Author => "MedveMarci";
    public override string Description => "vpn_guard";
    public override Version Version { get; } = new(1, 1, 0);
    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);
    public override bool IsTransparent => true;


    public override void Enable()
    {
        Singleton = this;
        var path = Path.Combine(PathManager.Configs.FullName, "VPNGuard");
        var bannedIpsName = Path.Combine(path, "BannedIps.txt");
        var checkedIpsName = Path.Combine(path, "CheckedIps.txt");

        if (!Directory.Exists(path))
        {
            Logger.Warn("VPNGuard directory does not exist. Creating...");
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
        PlayerEvents.Joined += EventHandler.OnJoined;
        ServerEvents.WaitingForPlayers += EventHandler.OnWaitingForPlayers;
    }

    public override void Disable()
    {
        try
        {
            var bannedIpsRead = File.ReadAllLines(Singleton.BannedIpsFilePath).ToHashSet();
            var checkedIpsRead = File.ReadAllLines(Singleton.CheckedIpsFilePath).ToHashSet();
            foreach (var checkedPlayer in EventHandler.CheckedPlayers.Where(checkedPlayer =>
                         !checkedIpsRead.Contains(checkedPlayer))) checkedIpsRead.Add(checkedPlayer);
            foreach (var bannedIp in EventHandler.BannedIps.Where(bannedIp => !bannedIpsRead.Contains(bannedIp)))
                bannedIpsRead.Add(bannedIp);
            File.WriteAllLines(Singleton.BannedIpsFilePath, bannedIpsRead);
            File.WriteAllLines(Singleton.CheckedIpsFilePath, checkedIpsRead);
        }
        catch (Exception e)
        {
            LogManager.Error($"Writing checked/banned file: {e}");
        }

        Singleton = null;
        PlayerEvents.Joined -= EventHandler.OnJoined;
        ServerEvents.WaitingForPlayers -= EventHandler.OnWaitingForPlayers;
    }
}