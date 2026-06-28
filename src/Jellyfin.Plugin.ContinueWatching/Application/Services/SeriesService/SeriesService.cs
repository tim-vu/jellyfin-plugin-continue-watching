using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.ContinueWatching.Application.Services.SeriesService;

public sealed class SeriesService(ILibraryManager libraryManager) : ISeriesService
{
    public Task<Guid?> GetNextEpisodeId(User user, Guid seriesId, Guid episodeId)
    {
        var series = libraryManager.GetItemById<Series>(seriesId);

        if (series is null)
        {
            return Task.FromResult<Guid?>(null);
        }

        var options = new DtoOptions(false);

        var episode = series.GetEpisodes(user, options, false)
            .OfType<Episode>()
            .Where(e => !e.IsMissingEpisode)
            .SkipWhile(e => e.Id != episodeId)
            .Skip(1)
            .FirstOrDefault();

        return Task.FromResult(episode?.Id);
    }
}