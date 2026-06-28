using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ContinueWatching.Infrastructure.Repositories;

public sealed class CursorStore : IHostedService, IDisposable
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(30);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly Dictionary<CursorKey, CursorDto> _cursors = [];
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private readonly ILogger<CursorStore> _logger;
    private readonly string _filePath;
    private CancellationTokenSource? _stoppingTokenSource;
    private Task? _flushTask;
    private int _dirty;

    public CursorStore(IApplicationPaths applicationPaths, ILogger<CursorStore> logger)
    {
        ArgumentNullException.ThrowIfNull(applicationPaths);
        _logger = logger;

        var directoryPath = Path.Combine(applicationPaths.PluginConfigurationsPath, "Jellyfin.Plugin.ContinueWatching");

        Directory.CreateDirectory(directoryPath);

        _filePath = Path.Combine(directoryPath, "cursors.json");
    }

    static CursorStore()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public T Read<T>(Func<IReadOnlyDictionary<CursorKey, CursorDto>, T> reader)
    {
        _lock.EnterReadLock();
        try
        {
            return reader.Invoke(_cursors);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Upsert(CursorKey key, CursorDto cursor)
    {
        _lock.EnterWriteLock();
        try
        {
            _cursors[key] = cursor;
            Interlocked.Exchange(ref _dirty, 1);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Delete(CursorKey key)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_cursors.Remove(key))
            {
                Interlocked.Exchange(ref _dirty, 1);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _flushTask = FlushPeriodically(_stoppingTokenSource.Token);
        return Load();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_stoppingTokenSource is not null)
        {
            await _stoppingTokenSource.CancelAsync().ConfigureAwait(false);
        }

        if (_flushTask is not null)
        {
            try
            {
                await _flushTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected during host shutdown.
            }
        }

        await Flush(cancellationToken, force: true).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _stoppingTokenSource?.Dispose();
        _lock.Dispose();
        _flushLock.Dispose();
    }

    private async Task Load()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        try
        {
            await using FileStream stream = File.OpenRead(_filePath);
            List<CursorDto>? cursors = await JsonSerializer.DeserializeAsync<List<CursorDto>>(stream, JsonOptions);

            if (cursors is null)
            {
                return;
            }

            foreach (CursorDto cursor in cursors)
            {
                _cursors[CursorKey.From(cursor)] = cursor;
            }
        }
        catch (IOException exception)
        {
            _logger.LogError(exception, "Failed to load cursor cache from {FilePath}", _filePath);
        }
        catch (JsonException exception)
        {
            _logger.LogError(exception, "Cursor cache at {FilePath} is not valid JSON", _filePath);
        }
    }

    private async Task FlushPeriodically(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(FlushInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    await Flush(cancellationToken, force: false).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    _logger.LogError(
                        exception,
                        "Failed to flush cursor cache to {FilePath}; the next interval will retry",
                        _filePath);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task Flush(CancellationToken cancellationToken, bool force)
    {
        if (!force && Interlocked.CompareExchange(ref _dirty, 0, 0) == 0)
        {
            return;
        }

        await _flushLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            IReadOnlyList<CursorDto> cursorsToWrite;
            _lock.EnterReadLock();
            try
            {
                if (!force && Interlocked.Exchange(ref _dirty, 0) == 0)
                {
                    return;
                }

                if (force)
                {
                    Interlocked.Exchange(ref _dirty, 0);
                }

                cursorsToWrite = [.. _cursors.Values];
            }
            finally
            {
                _lock.ExitReadLock();
            }

            string temporaryFilePath = _filePath + ".tmp";

            await using (FileStream stream = new(
                temporaryFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    cursorsToWrite,
                    JsonOptions,
                    cancellationToken).ConfigureAwait(false);

                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryFilePath, _filePath, overwrite: true);
        }
        catch
        {
            Interlocked.Exchange(ref _dirty, 1);
            throw;
        }
        finally
        {
            _flushLock.Release();
        }
    }

    public readonly record struct CursorKey(Guid UserId, Guid ItemId)
    {
        public static CursorKey From(CursorDto cursor) => new(cursor.UserId, cursor.ItemId);
    }
}

public enum CursorType
{
    Series,
    Movie
}

public sealed record CursorDto(
    CursorType Type,
    Guid UserId,
    Guid ItemId,
    Guid? EpisodeId,
    long PositionTicks,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc
);