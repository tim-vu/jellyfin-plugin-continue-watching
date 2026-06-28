using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.ContinueWatching.Application.Services.Sections;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.ContinueWatching.Controllers;

[ApiController]
[Authorize]
[Route("")]
public sealed class ContinueWatchingController(ContinueWatchingSection continueWatchingSection, IUserManager userManager) : ControllerBase
{
    private const string UserIdClaimType = "Jellyfin-UserId";
    private const string AdministratorRole = "Administrator";


    [HttpGet("UserItems/Resume")]
    [ProducesResponseType(StatusCodes.Status200OK)]

    public async Task<ActionResult<QueryResult<BaseItemDto>>> GetResumeItemsAsync(
        [FromQuery] Guid? userId,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? parentId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] MediaType[] mediaTypes,
        [FromQuery] bool? enableUserData,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ImageType[] enableImageTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] BaseItemKind[] excludeItemTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] BaseItemKind[] includeItemTypes,
        [FromQuery] bool enableTotalRecordCount = true,
        [FromQuery] bool? enableImages = true,
        [FromQuery] bool excludeActiveSessions = false)
    {
        var requestUserId = GetUserId(userId);
        var user = userManager.GetUserById(requestUserId);

        if (user is null)
        {
            return NotFound();
        }

        return await continueWatchingSection.GetItemsAsync(
            requestUserId,
            startIndex,
            limit,
            searchTerm,
            parentId,
            fields,
            mediaTypes,
            enableUserData,
            imageTypeLimit,
            enableImageTypes,
            excludeItemTypes,
            includeItemTypes,
            enableTotalRecordCount,
            enableImages,
            excludeActiveSessions);
    }

    [HttpGet("Users/{userId}/Items/Resume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<QueryResult<BaseItemDto>>> GetResumeItemsLegacy(
        [FromRoute, Required] Guid userId,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? parentId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] MediaType[] mediaTypes,
        [FromQuery] bool? enableUserData,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ImageType[] enableImageTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] BaseItemKind[] excludeItemTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] BaseItemKind[] includeItemTypes,
        [FromQuery] bool enableTotalRecordCount = true,
        [FromQuery] bool? enableImages = true,
        [FromQuery] bool excludeActiveSessions = false)
        => GetResumeItemsAsync(
                userId,
                startIndex,
                limit,
                searchTerm,
                parentId,
                fields,
                mediaTypes,
                enableUserData,
                imageTypeLimit,
                enableImageTypes,
                excludeItemTypes,
                includeItemTypes,
                enableTotalRecordCount,
                enableImages,
                excludeActiveSessions);
    private Guid GetUserId(Guid? userId)
    {
        string? value = User.Claims
            .FirstOrDefault(claim => claim.Type.Equals(UserIdClaimType, StringComparison.OrdinalIgnoreCase))
            ?.Value;

        if (!Guid.TryParse(value, out Guid authenticatedUserId))
        {
            authenticatedUserId = default;
        }

        if (userId is null)
        {
            return authenticatedUserId;
        }

        var isAdministrator = User.IsInRole(AdministratorRole);
        return !userId.Equals(authenticatedUserId) && !isAdministrator ? throw new SecurityException("Forbidden") : userId.Value;
    }
}