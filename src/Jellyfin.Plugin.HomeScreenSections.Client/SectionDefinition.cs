using System;

namespace Jellyfin.Plugin.HomeScreenSections.Client;

public sealed record SectionDefinition
{
    public required Guid Id { get; init; }

    public required string DisplayText { get; init; }

    public int Limit { get; init; } = 1;

    public string? Route { get; init; }

    public string? AdditionalData { get; init; }
}