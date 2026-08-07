<h1>VPNGuard</h1>

[![Version](https://img.shields.io/github/v/release/MedveMarci/VPNGuard?&label=Version&color=d500ff)](https://github.com/MedveMarci/VPNGuard/releases/latest) [![LabAPI Version](https://img.shields.io/badge/LabAPI_Version-1.1.7-51f4ff )](https://github.com/northwood-studios/LabAPI/releases/tag/1.1.7) [![SCP:SL Version](https://img.shields.io/badge/SCP:SL_Version-14.2.7-blue?&color=e5b200)](https://store.steampowered.com/app/700330/SCP_Secret_Laboratory/) [![Total Downloads](https://img.shields.io/github/downloads/MedveMarci/VPNGuard/total.svg?label=Total%20Downloads&color=&color=ffbf00)]()<br>

A SCP: Secret Laboratory LabApi plugin that prevent players to use VPN or Proxy.

# Features

- Blocks players using a VPN or Proxy from playing on the server.
- Detection powered by proxycheck.io — accurate VPN/Proxy/Tor detection over HTTPS.
- Saves the checked and banned IPs so the plugin does not waste your daily API quota.
- If a player is blocked, it sends a detailed message to Discord (webhook).

# Configuration

- `ProxyCheckApiKey` — optional free key for proxycheck.io (leave empty for the keyless tier of 100/day, or get a
  free key for 1000/day).
- `BlockHostingProviders` — also kick datacenter/hosting IPs (catches more VPNs, but may cause false positives).
- `AllowedIsps` — ISP/provider name substrings that are never kicked. Pre-filled with cloud gaming services (GeForce
  NOW, Xbox Cloud Gaming, Shadow, Boosteroid) so legitimate cloud players are not blocked.
- `AllowedAsns` — ASN numbers that are never kicked (e.g. `AS20347`).
- `Webhook` — optional Discord webhook URL for kick notifications.

> **Cloud gaming (GeForce NOW etc.):** these connect from datacenter IPs. With the default config they pass through, and
> the `AllowedIsps` allowlist guarantees they are never kicked — even if you enable `BlockHostingProviders`.

# For Support

<a href='https://discord.gg/KmpA8cfaSA'><img src='https://www.allkpop.com/upload/2021/01/content/262046/1611711962-discord-button.png' height="100"></a>

# Credits

* Plugin made by MedveMarci

