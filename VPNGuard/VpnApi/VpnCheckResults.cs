namespace VPNGuard.VpnApi;

public sealed class VpnCheckResult
{
    public bool Success { get; set; }
    public bool IsVpn { get; set; }
    public string Provider { get; set; }
    public string Ip { get; set; }
    public string Hostname { get; set; }
    public string CountryName { get; set; }
    public string CountryCode { get; set; }
    public string Isp { get; set; }
    public string Asn { get; set; }
    public string Operator { get; set; }
    public string DetectionReason { get; set; }
    public string Error { get; set; }

    public static VpnCheckResult Failed(string provider, string error)
    {
        return new VpnCheckResult { Success = false, Provider = provider, Error = error };
    }
}