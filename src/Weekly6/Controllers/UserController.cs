using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Weekly6.DTOs;
using Weekly6.Models;
using Weekly6.Services;

namespace Weekly6.Controllers;

[ApiController]
[Route("api/users")]

public sealed class UserController(IUserService service) : ControllerBase
{
  private readonly IUserService _service = service;

  [HttpGet]
  [ProducesResponseType(
      typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
  public async Task<ActionResult<IReadOnlyList<User>>> GetAll(CancellationToken cancellationToken)
  {
    var users = await _service.GetAllAsync(cancellationToken);
    return Ok(users);
  }

  [HttpGet("{id}/transactions")]
  [ProducesResponseType(
      typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
  public async Task<ActionResult<PagedResponse<TransactionResponse>>> GetUserTransactionById([FromRoute] long id, [FromQuery] TransactionFilterRequest filter, CancellationToken cancellationToken)
  {
    var userTransactions = await _service.GetUserTransactionsAsync(id, filter, cancellationToken);
    return Ok(userTransactions);
  }

  [HttpGet("{id}/mutations")]
  [ProducesResponseType(
      typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
  public async Task<ActionResult<PagedResponse<WalletMutationResponse>>> GetUserMutationById([FromRoute] long id, [FromQuery] WalletMutationFilterRequest filter, CancellationToken cancellationToken)
  {
    var userTransactions = await _service.GetUserMutationsAsync(id, filter, cancellationToken);
    return Ok(userTransactions);
  }

  [HttpGet("{id}")]
  [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
  public async Task<ActionResult<UserResponse>> GetById(
      [FromRoute] int id, CancellationToken cancellationToken)
  {
    var user = await _service.GetByIdAsync(id, cancellationToken);

    return user is null ? Problem(statusCode: StatusCodes.Status404NotFound, title: "User not found") : Ok(user);
  }

  [HttpPost]
  [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
  public async Task<ActionResult<UserResponse>> Create([FromBody] UserRequest request, CancellationToken cancellationToken)
  {
    try
    {
      var created = await _service.CreateAsync(request, cancellationToken);

      if (created is null)
      {
        return Problem(statusCode: StatusCodes.Status409Conflict, title: "User already exist");
      }

      return CreatedAtAction(
          nameof(GetById),
          new { id = created.Id }, created);
    }
    catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
    {
      return Problem(
          statusCode: StatusCodes.Status409Conflict,
          title: "User already exists",
          detail: "User tersebut sudah terdaftar");
    }
    catch (ArgumentException e)
    {
      return Problem(
          statusCode: StatusCodes.Status400BadRequest,
          title: "Invalid user data!",
          detail: e.Message);
    }
  }

  [HttpPut("{id}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
  public async Task<ActionResult<UserResponse>> Update(
      [FromRoute] int id, [FromBody] UserUpdateRequest request, CancellationToken cancellationToken)
  {
    try
    {
      var updated = await _service.UpdateAsync(id, request, cancellationToken);

      return updated is null ? Problem(statusCode: StatusCodes.Status404NotFound, title: "User not found", detail: $"No user was found for id {id}.") : Ok(updated);
    }
    catch (ArgumentException e)
    {
      return Problem(
          statusCode: StatusCodes.Status400BadRequest,
          title: "Invalid user data!",
          detail: e.Message);
    }
  }
}