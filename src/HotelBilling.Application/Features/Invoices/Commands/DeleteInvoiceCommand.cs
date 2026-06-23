using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;

namespace HotelBilling.Application.Features.Invoices.Commands;

public record DeleteInvoiceCommand(int Id) : IRequest<bool>;

public class DeleteInvoiceCommandHandler(IInvoiceRepository repo) : IRequestHandler<DeleteInvoiceCommand, bool>
{
    public async Task<bool> Handle(DeleteInvoiceCommand cmd, CancellationToken ct)
    {
        _ = await repo.GetByIdAsync(cmd.Id, ct) ?? throw new NotFoundException("Invoice", cmd.Id);
        return await repo.DeleteAsync(cmd.Id, ct);
    }
}

