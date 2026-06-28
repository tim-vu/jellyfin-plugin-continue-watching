namespace Jellyfin.Plugin.HomeScreenSections.Client;

public interface ISectionsClient
{
    bool IsAvailable { get; }

    bool TryRegisterSection<TResultsProvider>(SectionDefinition section)
        where TResultsProvider : class, ISectionResultsProvider;
}