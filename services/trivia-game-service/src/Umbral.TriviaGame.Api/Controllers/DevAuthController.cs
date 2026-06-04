using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Umbral.TriviaGame.Api.Controllers;

[ApiController]
[Route("api/dev")]
public class DevAuthController : ControllerBase
{
    [HttpGet("token")]
    public IActionResult GetDevToken()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Operador"),
            new Claim("sub", Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: null
        );

        var handler = new JwtSecurityTokenHandler();
        var tokenString = handler.WriteToken(token);

        return Ok(new { token = tokenString });
    }
}
