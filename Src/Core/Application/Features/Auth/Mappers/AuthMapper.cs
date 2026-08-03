using Application.Features.Auth.Commands;
using Domain.DTOs.Auth;
using Riok.Mapperly.Abstractions;
//using WebApi.Contracts.Auth;

namespace Application.Features.Auth.Mappers;

[Mapper]
public static partial class AuthMapper
{
    public static partial LoginCommand ToCommand(this AuthenticateRequest request);
}