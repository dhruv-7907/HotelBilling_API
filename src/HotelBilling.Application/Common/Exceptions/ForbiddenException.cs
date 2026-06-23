namespace HotelBilling.Application.Common.Exceptions;
public class ForbiddenException(string message = "Access forbidden.") : Exception(message);
