using FluentValidation;
using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Entities;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Application.Features.Reservations.Commands;

public record CreateReservationCommand(
    int GuestId, int RoomId, DateTime CheckIn, DateTime CheckOut,
    int Adults, int Children, decimal RatePerNight,
    BookingChannel Channel, PaymentMethod PaymentMethod,
    decimal AdvancePaid = 0, string? SpecialRequests = null
) : IRequest<int>;

public class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(x => x.GuestId).GreaterThan(0);
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.CheckIn).GreaterThanOrEqualTo(DateTime.Today);
        RuleFor(x => x.CheckOut).GreaterThan(x => x.CheckIn);
        RuleFor(x => x.RatePerNight).GreaterThan(0);
        RuleFor(x => x.Adults).GreaterThan(0);
    }
}

public class CreateReservationCommandHandler(IReservationRepository repo) : IRequestHandler<CreateReservationCommand, int>
{
    public async Task<int> Handle(CreateReservationCommand cmd, CancellationToken ct)
    {
        var available = await repo.RoomIsAvailableAsync(cmd.RoomId, cmd.CheckIn, cmd.CheckOut, null, ct);
        if (!available) throw new ConflictException("Room is not available for the selected dates.");

        var nights    = (int)(cmd.CheckOut - cmd.CheckIn).TotalDays;
        var subtotal  = cmd.RatePerNight * nights;
        var gst       = Math.Round(subtotal * 0.18m, 2);
        var total     = subtotal + gst;

        var reservation = new Reservation
        {
            ReservationCode = $"RES-{DateTime.UtcNow:yyyyMMddHHmmss}",
            GuestId         = cmd.GuestId,
            RoomId          = cmd.RoomId,
            CheckIn         = cmd.CheckIn,
            CheckOut        = cmd.CheckOut,
            Nights          = nights,
            Adults          = cmd.Adults,
            Children        = cmd.Children,
            RatePerNight    = cmd.RatePerNight,
            Subtotal        = subtotal,
            GstAmount       = gst,
            TotalAmount     = total,
            AdvancePaid     = cmd.AdvancePaid,
            BalanceDue      = total - cmd.AdvancePaid,
            Channel         = cmd.Channel,
            PaymentMethod   = cmd.PaymentMethod,
            SpecialRequests = cmd.SpecialRequests,
            Status          = ReservationStatus.Confirmed,
        };
        return await repo.CreateAsync(reservation, ct);
    }
}
