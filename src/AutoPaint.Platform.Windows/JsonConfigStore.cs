using System.Text.Json;
using System.Text.Json.Serialization;
using AutoPaint.Core.Config;

namespace AutoPaint.Platform.Windows;

/// <summary>
/// <see cref="IConfigStore"/> em JSON: %AppData%\AutoPaintHeartopia\config.json,
/// escrita atômica (.tmp + File.Replace) e save com debounce de 500 ms.
/// </summary>
public sealed class JsonConfigStore : IConfigStore, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _dir;
    private readonly string _path;
    private readonly object _gate = new();
    private readonly Timer _debounce;
    private bool _dirty;

    public AppConfig Current { get; private set; } = new();

    public JsonConfigStore(string? baseDir = null)
    {
        _dir = baseDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AutoPaintHeartopia");
        _path = Path.Combine(_dir, "config.json");
        _debounce = new Timer(_ => Flush(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public AppConfig Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var loaded = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(_path), JsonOptions);
                Current = loaded is null ? new AppConfig() : Migrate(loaded);
            }
            else
            {
                Current = new AppConfig();
            }
        }
        catch (Exception)
        {
            TryBackupCorrupt();
            Current = new AppConfig();
        }

        return Current;
    }

    public void Apply(AppConfig config)
    {
        lock (_gate)
        {
            Current = config;
            _dirty = true;
        }
    }

    public void Save()
    {
        lock (_gate) _dirty = true;
        _debounce.Change(500, Timeout.Infinite);
    }

    public void Flush()
    {
        lock (_gate)
        {
            if (!_dirty && File.Exists(_path)) return;
            Directory.CreateDirectory(_dir);

            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(Current, JsonOptions));

            if (File.Exists(_path))
                File.Replace(tmp, _path, null);
            else
                File.Move(tmp, _path);

            _dirty = false;
        }
    }

    public void Reset()
    {
        lock (_gate)
        {
            if (File.Exists(_path)) File.Delete(_path);
            Current = new AppConfig();
            _dirty = false;
        }
    }

    private static AppConfig Migrate(AppConfig cfg)
    {
        // v0 -> v1: nada ainda. Pontos de migração futuros ficam aqui.
        cfg.SchemaVersion = AppConfig.CurrentSchemaVersion;
        return cfg;
    }

    private void TryBackupCorrupt()
    {
        try
        {
            if (File.Exists(_path))
                File.Move(_path, _path + $".corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}", overwrite: true);
        }
        catch { /* melhor esforço */ }
    }

    public void Dispose() => _debounce.Dispose();
}
