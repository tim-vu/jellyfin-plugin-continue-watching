using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.ContinueWatching.Application.Services.Sections;

public static class DtoExtensions
{
    public const string ClientClaimType = "Jellyfin-Client";
    internal static DtoOptions AddClientFields(
        this DtoOptions dtoOptions, string? client)
    {
        if (string.IsNullOrEmpty(client))
        {
            return dtoOptions;
        }

        if (!dtoOptions.ContainsField(ItemFields.RecursiveItemCount))
        {
            if (client.Contains("kodi", StringComparison.OrdinalIgnoreCase) ||
                client.Contains("wmc", StringComparison.OrdinalIgnoreCase) ||
                client.Contains("media center", StringComparison.OrdinalIgnoreCase) ||
                client.Contains("classic", StringComparison.OrdinalIgnoreCase))
            {
                dtoOptions.Fields = [.. dtoOptions.Fields, ItemFields.RecursiveItemCount];
            }
        }

        if (!dtoOptions.ContainsField(ItemFields.ChildCount))
        {
            if (client.Contains("kodi", StringComparison.OrdinalIgnoreCase) ||
                client.Contains("wmc", StringComparison.OrdinalIgnoreCase) ||
                client.Contains("media center", StringComparison.OrdinalIgnoreCase) ||
                client.Contains("classic", StringComparison.OrdinalIgnoreCase) ||
                client.Contains("roku", StringComparison.OrdinalIgnoreCase) ||
                client.Contains("samsung", StringComparison.OrdinalIgnoreCase) ||
                client.Contains("androidtv", StringComparison.OrdinalIgnoreCase))
            {
                dtoOptions.Fields = [.. dtoOptions.Fields, ItemFields.ChildCount];
            }
        }

        return dtoOptions;
    }
    internal static DtoOptions AddAdditionalDtoOptions(
        this DtoOptions dtoOptions,
        bool? enableImages,
        bool? enableUserData,
        int? imageTypeLimit,
        IReadOnlyList<ImageType> enableImageTypes)
    {
        dtoOptions.EnableImages = enableImages ?? true;

        if (imageTypeLimit.HasValue)
        {
            dtoOptions.ImageTypeLimit = imageTypeLimit.Value;
        }

        if (enableUserData.HasValue)
        {
            dtoOptions.EnableUserData = enableUserData.Value;
        }

        if (enableImageTypes.Count != 0)
        {
            dtoOptions.ImageTypes = enableImageTypes;
        }

        return dtoOptions;
    }
}