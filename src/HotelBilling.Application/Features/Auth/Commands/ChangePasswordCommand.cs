using FluentValidation;
using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using BCrypt.Net;
namespace HotelBilling.Application.Features.Auth.Commands;

public record ChangePasswordCommand(int UserId, string CurrentPassword, string NewPassword) : IRequest<bool>;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6).Matches("[A-Z]").Matches("[0-9]");
    }
}

public class ChangePasswordCommandHandler(IUserRepository users) : IRequestHandler<ChangePasswordCommand, bool>
{
    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException("User", request.UserId);
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Current password is incorrect.");
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        return await users.UpdateAsync(user, ct);
    }
}
