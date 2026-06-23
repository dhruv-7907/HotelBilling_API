using MediatR;
using FluentValidation.Results;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Application.Features.Rooms.Commands;
public record UpdateRoomStatusCommand(int RoomId, string Status) : IRequest<bool>;
public class UpdateRoomStatusCommandHandler(IRoomRepository repo) : IRequestHandler<UpdateRoomStatusCommand, bool>
{
    public async Task<bool> Handle(UpdateRoomStatusCommand cmd, CancellationToken ct)
    {
        _ = await repo.GetByIdAsync(cmd.RoomId, ct) ?? throw new NotFoundException("Room", cmd.RoomId);

        var parsed = ParseEnumOrNumeric<RoomStatus>(cmd.Status)
            ?? throw new ValidationException([new ValidationFailure("status", "Invalid room status value.")]);

        return await repo.UpdateStatusAsync(cmd.RoomId, (RoomStatus)parsed, ct);
    }

    private static int? ParseEnumOrNumeric<TEnum>(string? value) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (int.TryParse(value, out var numeric) && Enum.IsDefined(typeof(TEnum), numeric)) return numeric;
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? Convert.ToInt32(parsed) : null;
    }
}
