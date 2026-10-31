using System.Reflection;

namespace Platform.Api;

/// <summary>
/// The release version that GET /healthz reports. The Dockerfile sets APP_VERSION (and the assembly version) from the
/// build's VERSION, and the release script waits until /healthz reports the version it built.
/// </summary>
public static class AppVersion
{
    public const string Setting = "APP_VERSION";

    public static string Get(IConfiguration config) =>
        config[Setting] is { Length: > 0 } fromBuild ? fromBuild : FromAssembly;

    // "1.2.3+<commit>" in local builds: the part before "+" is the version.
    internal static readonly string FromAssembly =
        typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            .Split('+')[0] is { Length: > 0 } version ? version : "0.0.0";
}
