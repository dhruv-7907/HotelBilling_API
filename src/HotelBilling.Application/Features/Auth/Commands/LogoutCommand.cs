using MediatR;
using HotelBilling.Application.Common.Interfaces;
namespace HotelBilling.Application.Features.Auth.Commands;
public record LogoutCommand(int UserId) : IRequest<bool>;
public class LogoutCommandHandler(IUserRepository users) : IRequestHandler<LogoutCommand, bool>
{
    public async Task<bool> Handle(LogoutCommand request, CancellationToken ct)
        => await users.UpdateRefreshTokenAsync(request.UserId, null, null, ct);
}
