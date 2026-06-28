using System;

namespace Jellyfin.Plugin.HomeScreenSections.Client;

public sealed class SectionsException : Exception
{
    public SectionsException()
    {
    }
    public SectionsException(string message)
        : base(message)
    {
    }
    public SectionsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}