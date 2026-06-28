using Jellyfin.Plugin.ContinueWatching.Application.Repositories;
using Jellyfin.Plugin.ContinueWatching.Application.Services;
using Jellyfin.Plugin.ContinueWatching.Application.Services.CursorService;
using Jellyfin.Plugin.ContinueWatching.Application.Services.Sections;
using Jellyfin.Plugin.ContinueWatching.Application.Services.SeriesService;
using Jellyfin.Plugin.ContinueWatching.Controllers;
using Jellyfin.Plugin.ContinueWatching.Infrastructure.Repositories;
using Jellyfin.Plugin.HomeScreenSections.Client;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ContinueWatching;

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    private const string JellyfinItemsControllerFullName = "Jellyfin.Api.Controllers.ItemsController";

    public void RegisterServices(IServiceCollection services, IServerApplicationHost applicationHost)
    {
        services.Configure<MvcOptions>(
            static options =>
            {
                options.Conventions.Add(new RemoveActionConvention(
                    JellyfinItemsControllerFullName,
                    "GetResumeItems"));
                options.Conventions.Add(new RemoveActionConvention(
                    JellyfinItemsControllerFullName,
                    "GetResumeItemsLegacy"));
            });
        services.AddSingleton<CursorStore>();
        services.AddScoped<ISeriesCursorRepository, SeriesCursorRepository>();
        services.AddScoped<IMovieCursorRepository, MovieCursorRepository>();
        services.AddScoped<ICursorRepository, CursorRepository>();
        services.AddHostedService(
            static serviceProvider => serviceProvider.GetRequiredService<CursorStore>());
        services.AddScoped<ICursorService, CursorService>();
        services.AddScoped<ICursorHandler, SeriesCursorPlaybackHandler>();
        services.AddScoped<ICursorHandler, MovieCursorPlaybackHandler>();
        services.AddScoped<ISeriesService, SeriesService>();
        services.AddSingleton<ISectionsClient, SectionsClient>();
        services.AddTransient<ContinueWatchingSection>();
        services.AddHostedService<EventListener>();
    }
}