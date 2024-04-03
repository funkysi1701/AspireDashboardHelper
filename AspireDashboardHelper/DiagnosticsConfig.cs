namespace AspireDashboardHelper;

public static class DiagnosticsConfig
{
    public static readonly string? ServiceName;

    private static string? _versionInfo;

    public static string GetVersion() => _versionInfo ??= GetFileVersion();

    public static string SetServiceName(string serviceName)
    {
        return serviceName;
    }

    private static string GetFileVersion()
    {
        return string.Empty;
    }

    public static readonly string? SaasTenant = "";

    public static string? GetSaasTenant() => SaasTenant;

    public static string GetWebSiteInstanceId()
    {
        string instanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? Guid.NewGuid().ToString();

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            instanceId = "local";
        }

        return instanceId;
    }
}
