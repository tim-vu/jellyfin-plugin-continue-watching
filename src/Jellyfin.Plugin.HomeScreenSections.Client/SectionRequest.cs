using System;

namespace Jellyfin.Plugin.HomeScreenSections.Client;

public sealed class SectionRequest
{
    public Guid UserId { get; set; }

    public string? AdditionalData { get; set; }
}