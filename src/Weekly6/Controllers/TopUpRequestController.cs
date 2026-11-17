
using Microsoft.AspNetCore.Mvc;
using Weekly6.DTOs;
using Weekly6.Models;
using Weekly6.Services;

namespace Weekly6.Controllers;

[ApiController]
[Route("api/topup-requests")]
public sealed class TopUpRequestController(ITopUpRequestService service) : ControllerBase
{
  private readonly ITopUpRequestService _service = service;

  [HttpGet]
  [ProducesResponseType(typeof(IReadOnlyList<TopUpResponse>), StatusCodes.Status200OK)]
  public async Task<ActionResult<IReadOnlyList<TopUpRequest>>> GetAll(CancellationToken cancellationToken)
  {
    var result = await _service.GetAllAsync(cancellationToken);

    return Ok(result);
  }
}