using System;
using CommandSystem;

namespace VPNGuard;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class Cage : ICommand
{
    public string Command => "vpncheck";

    public string[] Aliases { get; } = [];

    public string Description => "Ki/be kapcsolja a VPNGuard-ot!";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (EventHandler.IsCheck)
        {
            EventHandler.IsCheck = false;
            response = "VPNGuard ki kapcsolva!";
            return true;
        }

        EventHandler.IsCheck = true;
        response = "VPNGuard be kapcsolva!";
        return true;
    }
}