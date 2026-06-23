using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
namespace HotelBilling.Application.Features.Guests.Commands;
public record UpdateGuestCommand(int Id, string FullName, string Phone, string? City, string? Address, string? IdType, string? IdNumber, string? Nationality, string? Notes) : IRequest<bool>;
public class UpdateGuestCommandHandler(IGuestRepository repo) : IRequestHandler<UpdateGuestCommand, bool>
{
    public async Task<bool> Handle(UpdateGuestCommand cmd, CancellationToken ct)
    {
        var g = await repo.GetByIdAsync(cmd.Id, ct) ?? throw new NotFoundException("Guest", cmd.Id);
        g.FullName=cmd.FullName; g.Phone=cmd.Phone; g.City=cmd.City; g.Address=cmd.Address;
        g.IdType=cmd.IdType; g.IdNumber=cmd.IdNumber; g.Nationality=cmd.Nationality; g.Notes=cmd.Notes;
        g.UpdatedAt=DateTime.UtcNow;
        return await repo.UpdateAsync(g, ct);
    }
}
