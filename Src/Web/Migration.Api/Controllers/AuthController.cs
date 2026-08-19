using Application.Features.Auth.DTOs;
using Application.Features.Auth.Services;
using DataToolkit.Authentication;
using DataToolkit.Authentication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Migration.Api.Controllers;

[ApiController]
[Route("api/auth")]
//[ApiExplorerSettings(IgnoreApi = true)] //no se vea en swagger
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService<ApplicationUser> _authentication;
    private readonly IUserService _userService;

    public AuthController(
        IAuthenticationService<ApplicationUser> authentication,
        IUserService userService)
    {
        _authentication = authentication;
        _userService = userService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user =
            await _userService.ValidateAsync(
                request.UserName,
                request.Password);

        if (user is null)
        {
            return Unauthorized();
        }

        AuthenticationResult result =
            await _authentication.SignInAsync(user);

        return Ok(new
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken?.Value,
            TokenType = result.TokenType,
            ExpiresIn = result.ExpiresIn
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequestDto request,
        CancellationToken cancellationToken)
    {
        await _authentication.SignOutAsync(
            request.RefreshToken);

        return NoContent();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequestDto request,
        CancellationToken cancellationToken)
    {
        string? userId =
            await _authentication.GetUserIdAsync(
                request.RefreshToken);

        if (userId is null)
        {
            return Unauthorized();
        }

        ApplicationUser? user =
            await _userService.GetByIdAsync(userId);

        if (user is null)
        {
            return Unauthorized();
        }

        AuthenticationResult result =
            await _authentication.RefreshAsync(
                user,
                request.RefreshToken);

        return Ok(new
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken?.Value,
            TokenType = result.TokenType,
            ExpiresIn = result.ExpiresIn
        });
    }
}