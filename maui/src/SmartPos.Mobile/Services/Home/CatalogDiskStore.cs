using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SmartPos.Mobile.Services.Home;

/// <summary>
/// JSON file persistence for browse catalog cache under AppData/catalog-cache.
/// </summary>
public sealed class CatalogDiskStore
{
    public static readonly TimeSpan DiskTtl = TimeSpan.FromHours(24);

    private static readonly Regex InvalidFileChars = new(
        $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]",
        RegexOptions.Compiled);

    private readonly string _root;
    private readonly object _gate = new();

    public CatalogDiskStore()
    {
        _root = Path.Combine(FileSystem.AppDataDirectory, "catalog-cache");
        try
        {
            Directory.CreateDirectory(_root);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CatalogDiskStore] CreateDirectory failed: {ex.Message}");
        }
    }

    public bool TryRead<T>(string fileName, out T? value, out DateTimeOffset expiresAt)
    {
        value = default;
        expiresAt = default;
        try
        {
            var path = GetPath(fileName);
            if (!File.Exists(path))
            {
                return false;
            }

            var json = File.ReadAllText(path);
            var envelope = JsonSerializer.Deserialize<DiskCacheEnvelope<T>>(json, JsonDefaults.Options);
            if (envelope == null || envelope.Value is null)
            {
                return false;
            }

            if (DateTimeOffset.UtcNow >= envelope.ExpiresAt)
            {
                TryDeleteFile(path);
                return false;
            }

            value = envelope.Value;
            expiresAt = envelope.ExpiresAt;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CatalogDiskStore] TryRead({fileName}) failed: {ex.Message}");
            return false;
        }
    }

    public void WriteAsync<T>(string fileName, T value, TimeSpan ttl)
    {
        _ = Task.Run(() =>
        {
            try
            {
                Directory.CreateDirectory(_root);
                var envelope = new DiskCacheEnvelope<T>
                {
                    ExpiresAt = DateTimeOffset.UtcNow.Add(ttl),
                    Value = value
                };
                var json = JsonSerializer.Serialize(envelope, JsonDefaults.Options);
                var path = GetPath(fileName);
                var temp = path + ".tmp";
                lock (_gate)
                {
                    File.WriteAllText(temp, json);
                    File.Copy(temp, path, overwrite: true);
                    TryDeleteFile(temp);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CatalogDiskStore] WriteAsync({fileName}) failed: {ex.Message}");
            }
        });
    }

    public void Delete(string fileName)
    {
        try
        {
            TryDeleteFile(GetPath(fileName));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CatalogDiskStore] Delete({fileName}) failed: {ex.Message}");
        }
    }

    public void DeleteByPrefix(string prefix)
    {
        try
        {
            if (!Directory.Exists(_root))
            {
                return;
            }

            var safePrefix = SanitizeFileName(prefix);
            foreach (var path in Directory.EnumerateFiles(_root))
            {
                var name = Path.GetFileName(path);
                if (name.StartsWith(safePrefix, StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    TryDeleteFile(path);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CatalogDiskStore] DeleteByPrefix({prefix}) failed: {ex.Message}");
        }
    }

    public void DeleteAll()
    {
        try
        {
            if (!Directory.Exists(_root))
            {
                return;
            }

            foreach (var path in Directory.EnumerateFiles(_root))
            {
                TryDeleteFile(path);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CatalogDiskStore] DeleteAll failed: {ex.Message}");
        }
    }

    public static string SanitizeFileName(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "empty";
        }

        var sanitized = InvalidFileChars.Replace(key, "_");
        sanitized = sanitized.Replace('|', '_').Replace(' ', '_');
        return sanitized.Length > 120 ? sanitized[..120] : sanitized;
    }

    private string GetPath(string fileName) =>
        Path.Combine(_root, SanitizeFileName(fileName));

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // ignored
        }
    }

    private sealed class DiskCacheEnvelope<T>
    {
        public DateTimeOffset ExpiresAt { get; set; }
        public T? Value { get; set; }
    }
}
