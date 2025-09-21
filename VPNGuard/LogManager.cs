using System;
using LabApi.Features.Console;

namespace VPNGuard;

internal abstract class LogManager
{
    private static bool DebugEnabled => Plugin.Instance?.Config?.Debug == true;

    public static void Debug(string message)
    {
        if (!DebugEnabled)
            return;

        Logger.Raw($"[DEBUG] [{Plugin.Instance?.Name}] {message}", ConsoleColor.Green);
    }

    public static void Info(string message, ConsoleColor color = ConsoleColor.Cyan)
    {
        Logger.Raw($"[INFO] [{Plugin.Instance?.Name}] {message}", color);
    }

    public static void Warn(string message)
    {
        Logger.Warn(message);
    }

    public static void Error(string message)
    {
        Logger.Raw(
            $"[ERROR] [{Plugin.Instance?.Name}] Details:\nVersion: {Plugin.Instance?.Version}\n{message}",
            ConsoleColor.Red);
    }
}