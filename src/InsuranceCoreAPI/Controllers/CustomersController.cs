using InsuranceCoreAPI.DTOs.Customers;
using InsuranceCoreAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceCoreAPI.Controllers;

[ApiController]
[Route("customers")]
[Produces("application/json")]
public sealed class CustomersController(ICustomerService service) : ControllerBase
{
    /// <summary>Creates a new customer.</summary>
    /// <response code="201">Customer created successfully.</response>
    /// <response code="400">Validation error (e.g. empty FullName).</response>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        var customer = await service.CreateAsync(request);

        var response = new CustomerResponse(customer.Id, customer.FullName);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, response);
    }

    /// <summary>Gets a customer by ID.</summary>
    /// <response code="200">Customer found.</response>
    /// <response code="404">Customer not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var customer = await service.GetByIdAsync(id);
        return Ok(new CustomerResponse(customer.Id, customer.FullName));
    }
}
