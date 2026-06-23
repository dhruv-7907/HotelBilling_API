namespace HotelBilling.Domain.Enums;
public enum ReservationStatus { Pending=1, Confirmed, CheckedIn, CheckedOut, Cancelled, NoShow }
public enum RoomType          { Standard=1, Deluxe, Suite, Presidential }
public enum RoomStatus        { Available=1, Occupied, Dirty, Clean, Maintenance, Inspecting, DND }
public enum InvoiceStatus     { Draft=1, Pending, Paid, Overdue, Cancelled }
public enum PaymentMethod     { Cash=1, Card, UPI, BankTransfer, CityLedger, OTACollect }
public enum BookingChannel    { Direct=1, BookingCom, MakeMyTrip, Agoda, WalkIn, Corporate }
public enum UserRole          { SuperAdmin=1, Admin, FrontDesk, Housekeeping, AccountsManager }
public enum GstSlab           { Exempt=0, Slab12=12, Slab18=18 }
