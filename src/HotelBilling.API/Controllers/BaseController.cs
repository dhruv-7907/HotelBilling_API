using MediatR;
using Microsoft.AspNetCore.Mvc;
using HotelBilling.Application.Common.Models;
namespace HotelBilling.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseController(IMediator mediator) : ControllerBase
{
    protected readonly IMediator Mediator = mediator;

    protected IActionResult OkResult<T>(T data, string message = "Success")
        => Ok(ApiResponse<T>.Ok(data, message));

    protected IActionResult CreatedResult<T>(T data, string message = "Created successfully")
        => StatusCode(201, ApiResponse<T>.Ok(data, message));
}
