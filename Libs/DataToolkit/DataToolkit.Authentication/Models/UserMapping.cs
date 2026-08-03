using System.Security.Claims;

namespace DataToolkit.Authentication.Models;

public sealed class UserMapping<TUser>
{
    public Func<TUser, string> UserId { get; set; } = default!;

    public Func<TUser, IEnumerable<Claim>> Claims { get; set; } = _ => [];
}