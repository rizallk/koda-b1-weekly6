using Microsoft.AspNetCore.Mvc;
using Weekly6.DTOs;
using Weekly6.Middlewares;
using Weekly6.Services;

namespace Weekly6.Controllers;

[ApiController]
[Route("api/transfers")]

public sealed class TransferController(ITransferService service) : Controller
{
  private readonly ITransferService _service = service;

  [HttpPost]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
  public async Task<ActionResult> Transfer(
      [FromBody] TransferRequest request, CancellationToken cancellationToken)
  {
    try
    {
      var correlationId = HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString();

      var updated = await _service.TransferAsync(request, correlationId, cancellationToken);

      if (updated is null)
      {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Wallet not found",
            detail: $"Sender wallet ({request.SenderWalletId}) or Receiver wallet ({request.ReceiverWalletId}) was not found.");
      }

      return Ok(updated);
    }
    catch (ArgumentException e)
    {
      return Problem(
          statusCode: StatusCodes.Status400BadRequest,
          title: "Invalid transfer data",
          detail: e.Message);
    }
    catch (InvalidOperationException e)
    {
      return Problem(
          statusCode: StatusCodes.Status400BadRequest,
          title: "Transfer failed",
          detail: e.Message);
    }
  }
}