using Kartly.Application.Auth;
using Kartly.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kartly.Api.Controllers;

/// <summary>Site-wide settings: publicly readable by the storefront, editable by an admin.</summary>
[ApiController]
[Route("api/settings")]
[Authorize(Roles = Roles.Admin)]
[Produces("application/json")]
public sealed class SettingsController(ISettingsService settings) : ControllerBase
{
    /// <summary>
    /// Returns the current site settings. Anonymous — the storefront renders the site name,
    /// contact details and currency before (or without) signing in.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SiteSettingsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SiteSettingsResponse>> Get(CancellationToken ct)
        => Ok(await settings.GetAsync(ct));

    /// <summary>Replaces the site settings. Admin only.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(SiteSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SiteSettingsResponse>> Update(
        UpdateSiteSettingsRequest request, CancellationToken ct)
        => Ok(await settings.UpdateAsync(request, ct));
}
