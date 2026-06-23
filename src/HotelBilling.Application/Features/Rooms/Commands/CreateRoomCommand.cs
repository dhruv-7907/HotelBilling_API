using FluentValidation;
using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Entities;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Application.Features.Rooms.Commands;
public record CreateRoomCommand(string RoomNumber, RoomType RoomType, int Floor, decimal RatePerNight, int MaxOccupancy=2, bool HasMinibar=false, bool HasJacuzzi=false, string? Description=null) : IRequest<int>;
public class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator() { RuleFor(x => x.RoomNumber).NotEmpty(); RuleFor(x => x.RatePerNight).GreaterThan(0); RuleFor(x => x.Floor).GreaterThan(0); }
}
public class CreateRoomCommandHandler(IRoomRepository repo) : IRequestHandler<CreateRoomCommand, int>
{
    public async Task<int> Handle(CreateRoomCommand cmd, CancellationToken ct)
    {
        var exists = await repo.GetByNumberAsync(cmd.RoomNumber, ct);
        if (exists != null) throw new ConflictException($"Room '{cmd.RoomNumber}' already exists.");
        var room = new Room { RoomNumber=cmd.RoomNumber, RoomType=cmd.RoomType, Floor=cmd.Floor, RatePerNight=cmd.RatePerNight, MaxOccupancy=cmd.MaxOccupancy, HasMinibar=cmd.HasMinibar, HasJacuzzi=cmd.HasJacuzzi, Description=cmd.Description };
        return await repo.CreateAsync(room, ct);
    }
}
