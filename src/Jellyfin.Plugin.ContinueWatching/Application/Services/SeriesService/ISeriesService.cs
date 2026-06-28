using System;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Plugin.ContinueWatching.Application.Services.SeriesService;

public interface ISeriesService
{
    Task<Guid?> GetNextEpisodeId(User user, Guid seriesId, Guid episodeId);
}