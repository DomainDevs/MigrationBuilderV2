using DataToolkit.Authentication.Models;

namespace DataToolkit.Authentication.Refresh;

public sealed class RefreshTokenEntry
{
    public string UserId { get; init; } = string.Empty;

    public RefreshToken Token { get; init; } = default!;
}