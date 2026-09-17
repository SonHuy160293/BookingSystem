using BookingSystem.AspNetCore.Controllers;
using BookingSystem.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.Identity.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class AuthController : ApiController
{
    public AuthController(ISender sender)
       : base(sender)
    {
    }
}
