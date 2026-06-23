using FluentValidation;
using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Entities;
namespace HotelBilling.Application.Features.Guests.Commands;

public record CreateGuestCommand(string FullName, string Email, string Phone, string? City, string? Address, string? IdType, string? IdNumber, string? Nationality, DateTime? DateOfBirth, string? Notes) : IRequest<int>;

public class CreateGuestCommandValidator : AbstractValidator<CreateGuestCommand>
{
    public CreateGuestCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
    }
}

public class CreateGuestCommandHandler(IGuestRepository repo) : IRequestHandler<CreateGuestCommand, int>
{
    public async Task<int> Handle(CreateGuestCommand cmd, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(cmd.Email))
        {
            var exists = await repo.GetByEmailAsync(cmd.Email, ct);
            if (exists != null) throw new ConflictException($"Guest with email '{cmd.Email}' already exists.");
        }
        var guest = new Guest { FullName=cmd.FullName, Email=cmd.Email??string.Empty, Phone=cmd.Phone, City=cmd.City, Address=cmd.Address, IdType=cmd.IdType, IdNumber=cmd.IdNumber, Nationality=cmd.Nationality, DateOfBirth=cmd.DateOfBirth, Notes=cmd.Notes };
        return await repo.CreateAsync(guest, ct);
    }
}
