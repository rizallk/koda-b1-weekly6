
using Microsoft.AspNetCore.Mvc;
using Weekly6.DTOs;
using Weekly6.Models;
using Weekly6.Services;

namespace Weekly6.Controllers;

[ApiController]
[Route("api/payment-methods")]
public sealed class PaymentMethodController(IPaymentMethodService service) : ControllerBase
{
  private readonly IPaymentMethodService _service = service;

  [HttpGet]
  [ProducesResponseType(typeof(IReadOnlyList<PaymentMethodResponse>), StatusCodes.Status200OK)]
  public async Task<ActionResult<IReadOnlyList<PaymentMethod>>> GetAll([FromQuery] PaymentMethodFilterRequest filter, CancellationToken cancellationToken)
  {
    var result = await _service.GetAllAsync(filter, cancellationToken);

    return Ok(result);
  }
}