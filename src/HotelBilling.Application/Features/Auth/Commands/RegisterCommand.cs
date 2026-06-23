using FluentValidation;
using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Entities;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Application.Features.Auth.Commands;

public record RegisterCommand(string FullName, string Email, string Phone, string Password, UserRole Role = UserRole.FrontDesk) : IRequest<int>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).Matches("[A-Z]").Matches("[0-9]");
    }
}

public class RegisterCommandHandler(IUserRepository users, ICurrentUserService currentUser) : IRequestHandler<RegisterCommand, int>
{
    public async Task<int> Handle(RegisterCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            throw new ForbiddenException("Only authenticated administrators can register users.");

        if (!Enum.TryParse<UserRole>(currentUser.Role, true, out var callerRole) ||
            (callerRole != UserRole.SuperAdmin && callerRole != UserRole.Admin))
            throw new ForbiddenException("Only administrators can register users.");

        if (callerRole == UserRole.Admin && request.Role == UserRole.SuperAdmin)
            throw new ForbiddenException("Admin cannot create SuperAdmin users.");

        var exists = await users.GetByEmailAsync(request.Email, ct);
        if (exists != null) throw new ConflictException($"Email '{request.Email}' is already registered.");

        var user = new User
        {
            FullName     = request.FullName,
            Email        = request.Email.ToLowerInvariant(),
            Phone        = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role         = request.Role,
            IsActive     = true,
        };
        return await users.CreateAsync(user, ct);
    }
}
