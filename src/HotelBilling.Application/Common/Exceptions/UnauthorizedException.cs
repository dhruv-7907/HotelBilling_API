namespace HotelBilling.Application.Common.Exceptions;
public class UnauthorizedException(string message = "You are not authorized to perform this action.") : Exception(message);
