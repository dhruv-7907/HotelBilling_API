using FluentValidation;
using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Application.Features.Reservations.Commands;

public record UpdateReservationCommand(
    int Id, int Adults, int Children, decimal RatePerNight,
    BookingChannel Channel, PaymentMethod PaymentMethod,
    ReservationStatus Status, string? SpecialRequests
) : IRequest<bool>;

public class UpdateReservationCommandHandler(IReservationRepository repo) : IRequestHandler<UpdateReservationCommand, bool>
{
    public async Task<bool> Handle(UpdateReservationCommand cmd, CancellationToken ct)
    {
        var res = await repo.GetByIdAsync(cmd.Id, ct) ?? throw new NotFoundException("Reservation", cmd.Id);
        res.Adults          = cmd.Adults;
        res.Children        = cmd.Children;
        res.RatePerNight    = cmd.RatePerNight;
        res.Channel         = cmd.Channel;
        res.PaymentMethod   = cmd.PaymentMethod;
        res.Status          = cmd.Status;
        res.SpecialRequests = cmd.SpecialRequests;
        res.UpdatedAt       = DateTime.UtcNow;
        return await repo.UpdateAsync(res, ct);
    }
}
