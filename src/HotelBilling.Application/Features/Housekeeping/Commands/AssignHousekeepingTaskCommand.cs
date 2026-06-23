using FluentValidation;
using MediatR;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Entities;
namespace HotelBilling.Application.Features.Housekeeping.Commands;
public record AssignHousekeepingTaskCommand(int RoomId, int? AssignedToId, string TaskType, string? Notes) : IRequest<int>;
public class AssignHousekeepingTaskCommandValidator : AbstractValidator<AssignHousekeepingTaskCommand>
{
    public AssignHousekeepingTaskCommandValidator() { RuleFor(x => x.RoomId).GreaterThan(0); RuleFor(x => x.TaskType).NotEmpty(); }
}
public class AssignHousekeepingTaskCommandHandler(IHousekeepingRepository repo) : IRequestHandler<AssignHousekeepingTaskCommand, int>
{
    public Task<int> Handle(AssignHousekeepingTaskCommand cmd, CancellationToken ct)
    {
        var task = new HousekeepingTask { RoomId=cmd.RoomId, AssignedToId=cmd.AssignedToId, TaskType=cmd.TaskType, Notes=cmd.Notes, Status="Pending" };
        return repo.CreateAsync(task, ct);
    }
}
