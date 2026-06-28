using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
using Jellyfin.Plugin.ContinueWatching.Application.Repositories;
using Jellyfin.Plugin.ContinueWatching.Domain;
using Jellyfin.Plugin.HomeScreenSections.Client;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Plugin.ContinueWatching.Application.Services.Sections;

public sealed class ContinueWatchingSection(
    ICursorRepository cursorRepository,
    IUserManager userManager,
    ILibraryManager libraryManager,
    IDtoService dtoService,
    ISessionManager sessionManager) : ISectionResultsProvider
{

    public QueryResult<BaseItemDto> GetResults(SectionRequest request)
    {
        var result = GetItemsAsync(
            request.UserId,
            startIndex: null,
            limit: 12,
            searchTerm: null,
            parentId: null,
            fields: [ItemFields.PrimaryImageAspectRatio],
            mediaTypes: [MediaType.Video],
            enableUserData: true,
            imageTypeLimit: 1,
            enableImageTypes: [ImageType.Primary, ImageType.Backdrop, ImageType.Thumb],
            excludeItemTypes: [],
            includeItemTypes: [],
            enableTotalRecordCount: false);

        result.Wait();
        return result.Result;
    }

    public async Task<QueryResult<BaseItemDto>> GetItemsAsync(
        Guid userId,
        int? startIndex,
        int? limit,
        string? searchTerm,
        Guid? parentId,
        ItemFields[] fields,
        MediaType[] mediaTypes,
        bool? enableUserData,
        int? imageTypeLimit,
        ImageType[] enableImageTypes,
        BaseItemKind[] excludeItemTypes,
        BaseItemKind[] includeItemTypes,
        bool enableTotalRecordCount = true,
        bool? enableImages = true,
        bool excludeActiveSessions = false,
        string? client = null)
    {
        var user = userManager.GetUserById(userId);
        if (user is null)
        {
            return new QueryResult<BaseItemDto>([]);
        }

        var cursors = await cursorRepository.GetByUserId(userId);
        if (cursors.Count == 0)
        {
            return new QueryResult<BaseItemDto>(startIndex, 0, []);
        }

        var parentIdGuid = parentId ?? Guid.Empty;
        var dtoOptions = new DtoOptions { Fields = fields }
            .AddClientFields(client)
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

        var ancestorIds = Array.Empty<Guid>();

        var excludeFolderIds = user.GetPreferenceValues<Guid>(PreferenceKind.LatestItemExcludes);
        if (parentIdGuid.IsEmpty() && excludeFolderIds.Length > 0)
        {
            ancestorIds = [.. libraryManager.GetUserRootFolder().GetChildren(user, true)
                .Where(i => i is Folder && !excludeFolderIds.Contains(i.Id))
                .Select(i => i.Id)];
        }

        var excludeItemIds = Array.Empty<Guid>();
        if (excludeActiveSessions)
        {
            excludeItemIds = [.. sessionManager.Sessions
                .Where(s => s.UserId.Equals(userId) && s.NowPlayingItem is not null)
                .Select(s => s.NowPlayingItem.Id)];
        }

        QueryResult<BaseItem> itemsResult = libraryManager.GetItemsResult(
            new InternalItemsQuery(user)
            {
                StartIndex = startIndex,
                Limit = limit,
                ParentId = Guid.Empty,
                Recursive = true,
                DtoOptions = dtoOptions,
                MediaTypes = mediaTypes ?? Enum.GetValues<MediaType>(),
                IsVirtualItem = false,
                CollapseBoxSetItems = false,
                EnableTotalRecordCount = enableTotalRecordCount,
                AncestorIds = ancestorIds,
                IncludeItemTypes = includeItemTypes ?? [],
                ExcludeItemTypes = excludeItemTypes ?? [],
                ExcludeItemIds = excludeItemIds,
                SearchTerm = searchTerm,
                ItemIds = [.. cursors.Select(GetItemId)]
            });

        var cursorByItemId = cursors.ToDictionary(GetItemId);

        var results = dtoService.GetBaseItemDtos(itemsResult.Items, dtoOptions, user)
            .Select(i => (Item: i, Cursor: cursorByItemId.GetValueOrDefault(i.Id)))
            .Where(p => p.Cursor is not null)
            .Cast<(BaseItemDto Item, Cursor Cursor)>()
            .OrderByDescending(CursorUpdatedAt)
            .Select(UpdateUserData)
            .Select(p => p.Item)
            .ToList();

        (BaseItemDto Item, Cursor Cursor) UpdateUserData((BaseItemDto Item, Cursor Cursor) arg)
        {
            if (enableUserData is false or null)
            {
                return arg;
            }

            arg.Item.UserData ??= new UserItemDataDto
            {
                Key = arg.Item.Id.ToString()
            };
            arg.Item.UserData.PlaybackPositionTicks = arg.Cursor.PositionTicks;
            if (arg.Item.RunTimeTicks is > 0)
            {
                arg.Item.UserData.PlayedPercentage = (double)arg.Cursor.PositionTicks / arg.Item.RunTimeTicks * 100;
            }

            return arg;
        }

        static DateTimeOffset CursorUpdatedAt((BaseItemDto Item, Cursor Cursor) arg)
        {
            return arg.Cursor.UpdatedAt;
        }

        return new QueryResult<BaseItemDto>(
            startIndex,
            itemsResult.TotalRecordCount,
            results);
    }

    private static Guid GetItemId(Cursor cursor)
    {
        return cursor switch
        {
            SeriesCursor sc => sc.EpisodeId,
            MovieCursor mc => mc.ItemId,
            _ => throw new NotImplementedException()
        };
    }
}