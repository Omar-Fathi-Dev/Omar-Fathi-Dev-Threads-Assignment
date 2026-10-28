using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using OrderProcessing.Shared.Exceptions;

namespace OrderProcessing.Shared.Storage;

public class JsonFileStore
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> FileLocks = new();

    private static readonly JsonSerializerOptions Options = new ()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private async Task<List<T>> ReadCoreAsync<T>(
        string path,
        CancellationToken ct)
    {
        if (!File.Exists(path))
            return new List<T>();

        var json =await File.ReadAllTextAsync(path, ct);
        try
        {
            List<T> items = JsonSerializer.Deserialize<List<T>>(json , Options ) ?? new List<T>();
            return items;
        }
        catch (JsonException e)
        {
            throw new DataFileCorruptedException(Path.GetFileName(path), e);
        }

    }
    public async Task<List<T>> ReadAsync<T>(string path, CancellationToken ct)
    {
        SemaphoreSlim fileLock = GetFileLock(path);
        await fileLock.WaitAsync(ct);
        try
        {
            List<T> items = await ReadCoreAsync<T>(path, ct);
            return items;
        }
        finally
        {
            fileLock.Release();
        }
    }

    private async Task WriteCoreAsync<T>(string path, List<T> items, CancellationToken ct)
    {
        string json = JsonSerializer.Serialize(items, Options);
        string temporaryFileName = "temporary_" + Guid.NewGuid().ToString() + ".json";
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporaryPath = Path.Combine(Path.GetDirectoryName(path)!,temporaryFileName);
        
        try
        {
            await File.WriteAllTextAsync(temporaryPath, json, ct);
            ct.ThrowIfCancellationRequested();
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
    
    private SemaphoreSlim GetFileLock(string path)
        => FileLocks.GetOrAdd(path,key => new SemaphoreSlim(1, 1));

    public async Task WriteAsync<T>(string path, List<T> items, CancellationToken ct)
    {
        SemaphoreSlim fileLock = GetFileLock(path);
        await fileLock.WaitAsync(ct);
        try
        {
            await WriteCoreAsync<T>(path, items, ct);
        }
        finally
        {
            fileLock.Release();
        }
    }

    public async Task UpdateAsync<T>(string path, Func<List<T>, List<T>> update, CancellationToken ct)
    {
       SemaphoreSlim fileLock = GetFileLock(path);
       await fileLock.WaitAsync(ct);
       try
       {
            var items  = await ReadCoreAsync<T>(path, ct);
            var updatedItems = update(items);
            ct.ThrowIfCancellationRequested();
            await WriteCoreAsync<T>(path, updatedItems, ct);
       }
       finally
       {
           fileLock.Release();
       }
    }
    
}