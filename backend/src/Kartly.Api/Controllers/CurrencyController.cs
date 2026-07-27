using Kartly.Application.Currency;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kartly.Api.Controllers;

/// <summary>
/// Exchange rates for converting stored base-currency amounts into the site's display currency.
/// Read-only and anonymous — the storefront needs rates before (or without) signing in.
/// </summary>
[ApiController]
[Route("api/currency")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class CurrencyController(ICurrencyRateService rates) : ControllerBase
{
    /// <summary>
    /// Current rates. Always 200: when the provider is unreachable this serves last-known-good or
    /// built-in rates with source="fallback", because a rate outage must not blank out prices.
    /// </summary>
    [HttpGet("rates")]
    [ProducesResponseType(typeof(ExchangeRatesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExchangeRatesResponse>> GetRates(CancellationToken ct)
        => Ok(await rates.GetRatesAsync(ct));
}
