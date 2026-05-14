using System.Xml;
using Microsoft.Extensions.Configuration;

namespace HermesAgent.Cli.Configuration;

/// <summary>
/// Reads a .NET app.config / web.config XML file and exposes its values
/// to <see cref="IConfiguration"/>.
/// </summary>
public sealed class AppConfigConfigurationSource : IConfigurationSource
{
    private readonly string _path;
    private readonly bool _optional;
    private readonly bool _reloadOnChange;

    public AppConfigConfigurationSource(string path, bool optional = true, bool reloadOnChange = false)
    {
        _path = path;
        _optional = optional;
        _reloadOnChange = reloadOnChange;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new AppConfigConfigurationProvider(_path, _optional, _reloadOnChange);
}

/// <summary>
/// Configuration provider that parses app.config XML into the flat key:value
/// dictionary that <see cref="IConfiguration"/> expects.
/// </summary>
public sealed class AppConfigConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly string _path;
    private readonly bool _optional;
    private readonly bool _reloadOnChange;
    private FileSystemWatcher? _watcher;

    public AppConfigConfigurationProvider(string path, bool optional, bool reloadOnChange)
    {
        _path = path;
        _optional = optional;
        _reloadOnChange = reloadOnChange;
    }

    public override void Load()
    {
        if (!File.Exists(_path))
        {
            if (!_optional)
                throw new FileNotFoundException($"app.config not found at '{_path}'.");

            Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            SetupWatcher();
            return;
        }

        try
        {
            Data = ParseAppConfig(_path);
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException($"app.config at '{_path}' contains invalid XML: {ex.Message}", ex);
        }

        SetupWatcher();
    }

    // ─── Parsing ────────────────────────────────────────────────────────────

    internal static Dictionary<string, string?> ParseAppConfig(string path)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var doc = new XmlDocument();
        doc.Load(path);

        var root = doc.DocumentElement;
        if (root is null) return data;

        var appSettings = root.SelectSingleNode("appSettings");
        if (appSettings is not null)
            ParseAppSettings(appSettings, data);

        var hermesSettings = root.SelectSingleNode("hermesSettings");
        if (hermesSettings is not null)
            ParseHermesSettings(hermesSettings, data);

        var connStrings = root.SelectSingleNode("connectionStrings");
        if (connStrings is not null)
            ParseConnectionStrings(connStrings, data);

        return data;
    }

    private static void ParseAppSettings(XmlNode node, Dictionary<string, string?> data)
    {
        foreach (XmlNode child in node.ChildNodes)
        {
            if (child is not XmlElement el) continue;
            if (!el.Name.Equals("add", StringComparison.OrdinalIgnoreCase)) continue;

            var key = el.GetAttribute("key");
            var value = el.GetAttribute("value");
            if (string.IsNullOrWhiteSpace(key)) continue;

            var normalizedKey = NormalizeKey(key);
            data[normalizedKey] = value;
        }
    }

    private static void ParseHermesSettings(XmlNode node, Dictionary<string, string?> data)
    {
        foreach (XmlNode child in node.ChildNodes)
        {
            if (child is not XmlElement el) continue;

            var section = ToPascalCase(el.Name);
            var prefix = $"Hermes:{section}";

            foreach (XmlAttribute attr in el.Attributes)
            {
                var propertyKey = $"{prefix}:{ToPascalCase(attr.Name)}";
                data[propertyKey] = attr.Value;
            }

            foreach (XmlNode grandchild in el.ChildNodes)
            {
                if (grandchild is not XmlElement gcEl) continue;
                var subSection = ToPascalCase(gcEl.Name);
                foreach (XmlAttribute attr in gcEl.Attributes)
                    data[$"{prefix}:{subSection}:{ToPascalCase(attr.Name)}"] = attr.Value;
            }
        }
    }

    private static void ParseConnectionStrings(XmlNode node, Dictionary<string, string?> data)
    {
        foreach (XmlNode child in node.ChildNodes)
        {
            if (child is not XmlElement el) continue;
            if (!el.Name.Equals("add", StringComparison.OrdinalIgnoreCase)) continue;

            var name = el.GetAttribute("name");
            var connStr = el.GetAttribute("connectionString");
            if (!string.IsNullOrWhiteSpace(name))
                data[$"Hermes:ConnectionStrings:{name}"] = connStr;
        }
    }

    // ─── Key normalization ───────────────────────────────────────────────────

    public static string NormalizeKey(string key)
    {
        if (key.Contains(':') && !key.Contains('.') && !key.Contains("__"))
        {
            var segments = key.Split(':');
            return string.Join(':', segments.Select(ToPascalCase));
        }

        if (key.Contains("__"))
        {
            var segments = key.Split("__", StringSplitOptions.RemoveEmptyEntries);
            return string.Join(':', segments.Select(ToPascalCase));
        }

        if (key.Contains('.'))
        {
            var segments = key.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(':', segments.Select(s => ToPascalCase(s.Replace("_", ""))));
        }

        return $"Hermes:{ToPascalCase(key)}";
    }

    private static string ToPascalCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        if (char.IsUpper(s[0]) && !s.Contains('_') && !s.Contains('-'))
            return s;

        if (s.Contains('_') || s.Contains('-'))
        {
            var parts = s.Split(['_', '-'], StringSplitOptions.RemoveEmptyEntries);
            return string.Concat(parts.Select(p =>
                p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p[1..]));
        }

        return char.ToUpperInvariant(s[0]) + s[1..];
    }

    // ─── File watching ───────────────────────────────────────────────────────

    private void SetupWatcher()
    {
        if (!_reloadOnChange) return;

        var dir = Path.GetDirectoryName(Path.GetFullPath(_path));
        var file = Path.GetFileName(_path);
        if (dir is null || file is null) return;

        _watcher = new FileSystemWatcher(dir, file)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Renamed += OnFileChanged;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        Thread.Sleep(200);
        Load();
        OnReload();
    }

    public void Dispose() => _watcher?.Dispose();
}

public static class AppConfigConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddAppConfig(
        this IConfigurationBuilder builder,
        string? path = null,
        bool optional = true,
        bool reloadOnChange = false)
    {
        var resolvedPath = path ?? (File.Exists("app.config") ? "app.config" : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".hermes", "app.config"));
        return builder.Add(new AppConfigConfigurationSource(resolvedPath, optional, reloadOnChange));
    }
}
