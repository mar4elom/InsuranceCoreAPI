using InsuranceCoreAPI.DTOs.Claims;
using InsuranceCoreAPI.Services.Interfaces;
using InsuranceCoreAPI.Utils.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceCoreAPI.Controllers;

[ApiController]
[Route("claims")]
[Produces("application/json")]
public sealed class ClaimsController(IClaimService service) : ControllerBase
{
    /// <summary>Creates a new claim in New status.</summary>
    /// <response code="201">Claim created in New status.</response>
    /// <response code="400">Validation error (e.g. negative AmountRequested).</response>
    /// <response code="404">Referenced policy not found.</response>
    /// <response code="409">Policy is not Active, or IncidentDate is outside the policy period.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ClaimResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateClaimRequest request)
    {
        var claim = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = claim.Id }, ClaimHelper.ToResponse(claim));
    }

    /// <summary>
    /// Approves or rejects a claim.
    /// Rejection requires a non-empty DecisionReason.
    /// </summary>
    /// <response code="200">Decision recorded.</response>
    /// <response code="400">Reject decision is missing DecisionReason.</response>
    /// <response code="404">Claim not found.</response>
    /// <response code="409">Claim is not in New status.</response>
    [HttpPost("{id:guid}/decide")]
    [ProducesResponseType(typeof(ClaimResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Decide(Guid id, [FromBody] DecideClaimRequest request)
    {
        var claim = await service.DecideAsync(id, request);
        return Ok(ClaimHelper.ToResponse(claim));
    }

    /// <summary>Gets a claim by ID.</summary>
    /// <response code="200">Claim found.</response>
    /// <response code="404">Claim not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClaimResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var claim = await service.GetByIdAsync(id);
        return Ok(ClaimHelper.ToResponse(claim));
    }
}
