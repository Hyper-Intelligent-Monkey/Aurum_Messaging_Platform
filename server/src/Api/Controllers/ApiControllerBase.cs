using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    // gets the current user ID from the JWT claims
    protected int CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}