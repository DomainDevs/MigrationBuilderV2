using Application.Features.Auth.DTOs;
using MediatR;

namespace Application.Features.Auth.Commands;

public sealed record LoginCommand(
    string UserName,
    string Password)
    : IRequest<LoginResponse?>;