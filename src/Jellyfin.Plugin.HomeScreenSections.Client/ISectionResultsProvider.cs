using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Plugin.HomeScreenSections.Client;

public interface ISectionResultsProvider
{
    QueryResult<BaseItemDto> GetResults(SectionRequest request);
}