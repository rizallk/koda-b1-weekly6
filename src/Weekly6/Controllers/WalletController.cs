using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Weekly6.DTOs;
using Weekly6.Middlewares;
using Weekly6.Services;

namespace Weekly6.Controllers;

[ApiController]
[Route("api/wallets")]

public sealed class WalletController(IWalletService service) : ControllerBase
{
  private readonly IWalletService _service = service;

  [HttpGet("{userId}")]
  [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
  public async Task<ActionResult<UserResponse>> GetByUserId(
      [FromRoute] int userId, CancellationToken cancellationToken)
  {
    var wallet = await _service.GetByUserIdAsync(userId, cancellationToken);

    return wallet is null ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Wallet not found") : Ok(wallet);
  }

  // Top Up Request
  [HttpPost("{userId}/topup")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
  public async Task<ActionResult<WalletResponse>> RequestTopUp(
      [FromRoute] long userId, [FromBody] TopUpRequestDto request, CancellationToken cancellationToken)
  {
    try
    {
      var correlationId = HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString();

      var result = await _service.RequestTopUpAsync(userId, request, correlationId, cancellationToken);

      return result is null ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Wallet not found", detail: $"No wallet was found for user id {userId}.") : Ok(result);
    }
    catch (ArgumentException e)
    {
      return Problem(
          statusCode: StatusCodes.Status400BadRequest,
          title: "Invalid wallet data!",
          detail: e.Message);
    }
  }

  // Top Up Confirm
  [HttpPost("topup/{topUpRequestId}/confirm")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
  public async Task<IActionResult> ConfirmTopUp(
    long topUpRequestId,
    [FromQuery] long actorUserId,
    CancellationToken cancellationToken)
  {
    var correlationId = HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString();

    try
    {
      var updatedWallet = await _service.ConfirmTopUpAsync(
        topUpRequestId,
        actorUserId,
        correlationId,
        cancellationToken);

      if (updatedWallet is null)
        return NotFound(new { message = $"Permintaan top up dengan ID {topUpRequestId} tidak ditemukan." });

      return Ok(updatedWallet);
    }
    catch (InvalidOperationException ex)
    {
      return BadRequest(new { message = ex.Message });
    }
    catch (KeyNotFoundException ex)
    {
      return NotFound(new { message = ex.Message });
    }
    catch (DbUpdateConcurrencyException)
    {
      return Conflict(new { message = "Terjadi konflik pembaruan saldo secara bersamaan. Silakan muat ulang data dan coba lagi." });
    }
  }
}