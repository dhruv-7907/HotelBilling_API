using MediatR;
using FluentValidation.Results;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Application.Features.Invoices.Commands;
public record UpdateInvoiceStatusCommand(int Id, string Status) : IRequest<bool>;
public class UpdateInvoiceStatusCommandHandler(IInvoiceRepository repo) : IRequestHandler<UpdateInvoiceStatusCommand, bool>
{
    public async Task<bool> Handle(UpdateInvoiceStatusCommand cmd, CancellationToken ct)
    {
        _ = await repo.GetByIdAsync(cmd.Id, ct) ?? throw new NotFoundException("Invoice", cmd.Id);

        var parsed = ParseEnumOrNumeric<InvoiceStatus>(cmd.Status)
            ?? throw new ValidationException([new ValidationFailure("status", "Invalid invoice status value.")]);

        return await repo.UpdateStatusAsync(cmd.Id, (InvoiceStatus)parsed, ct);
    }

    private static int? ParseEnumOrNumeric<TEnum>(string? value) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (int.TryParse(value, out var numeric) && Enum.IsDefined(typeof(TEnum), numeric)) return numeric;
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? Convert.ToInt32(parsed) : null;
    }
}
