using System;
using System.IO;
using System.Reflection;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.ContinueWatching;

public class Plugin : IPlugin
{
    public Guid Id { get; } = Guid.Parse("7b3f1b70-4d0e-4d9d-a92d-5e2f632b91c1");

    public string Name => "Continue Watching";

    public string Description => "Continue Watching updates Jellyfin's default resume list to work like you expect it to.";

    public Version Version { get; } = typeof(Plugin).Assembly.GetName().Version!;

    public string AssemblyFilePath { get; } = typeof(Plugin).Assembly.Location;

    public bool CanUninstall => true;

    public string DataFolderPath { get; } = string.Empty;

    public PluginInfo GetPluginInfo() => new(Name, Version, Description, Id, CanUninstall);

    public void OnUninstalling() { }
}