using MediatR;
using HotelBilling.Application.Common.Interfaces;
namespace HotelBilling.Application.Features.Housekeeping.Commands;
public record UpdateTaskStatusCommand(int TaskId, string Status, int? UserId) : IRequest<bool>;
public class UpdateTaskStatusCommandHandler(IHousekeepingRepository repo) : IRequestHandler<UpdateTaskStatusCommand, bool>
{
    public Task<bool> Handle(UpdateTaskStatusCommand cmd, CancellationToken ct) => repo.UpdateStatusAsync(cmd.TaskId, cmd.Status, cmd.UserId, ct);
}
