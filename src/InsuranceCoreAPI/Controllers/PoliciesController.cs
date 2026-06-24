using InsuranceCoreAPI.DTOs.Policies;
using InsuranceCoreAPI.Services.Interfaces;
using InsuranceCoreAPI.Utils.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceCoreAPI.Controllers;

[ApiController]
[Route("policies")]
[Produces("application/json")]
public sealed class PoliciesController(IPolicyService service) : ControllerBase
{
    /// <summary>Creates a new policy in Draft status.</summary>
    /// <response code="201">Policy created in Draft status.</response>
    /// <response code="400">Validation error (e.g. EndDate ≤ StartDate, negative Premium).</response>
    /// <response code="404">Referenced customer not found.</response>
    [HttpPost]
    [ProducesResponseType(typeof(PolicyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreatePolicyRequest request)
    {
        var policy = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = policy.Id }, PolicyHelper.ToResponse(policy));
    }

    /// <summary>
    /// Activates a Draft policy.
    /// Returns 409 if the policy is not Draft, or if it overlaps with another Active policy
    /// for the same customer and product type.
    /// </summary>
    /// <response code="200">Policy activated successfully.</response>
    /// <response code="404">Policy not found.</response>
    /// <response code="409">Policy is not in Draft status, or activation would create an overlap.</response>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(PolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Activate(Guid id)
    {
        var policy = await service.ActivateAsync(id);
        return Ok(PolicyHelper.ToResponse(policy));
    }

    /// <summary>Gets a policy by ID.</summary>
    /// <response code="200">Policy found.</response>
    /// <response code="404">Policy not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var policy = await service.GetByIdAsync(id);
        return Ok(PolicyHelper.ToResponse(policy));
    }
}
