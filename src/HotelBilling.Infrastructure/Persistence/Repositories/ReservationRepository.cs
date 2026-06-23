using Dapper;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Application.Common.Models;
using HotelBilling.Domain.Entities;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Infrastructure.Persistence.Repositories;

public class ReservationRepository(DapperContext ctx) : IReservationRepository
{
    public async Task<PagedResult<Reservation>> GetAllAsync(PaginationQuery query, string? status, string? channel, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        var conditions = new List<string> { "r.IsDeleted = 0" };
        var statusId = ParseEnumOrNumeric<ReservationStatus>(status);
        var channelId = ParseEnumOrNumeric<BookingChannel>(channel);

        if (!string.IsNullOrWhiteSpace(status))
            conditions.Add(statusId.HasValue ? "r.Status = @StatusId" : "1 = 0");

        if (!string.IsNullOrWhiteSpace(channel))
            conditions.Add(channelId.HasValue ? "r.Channel = @ChannelId" : "1 = 0");

        if (!string.IsNullOrEmpty(query.Search))
            conditions.Add("(g.FullName LIKE @Search OR r.ReservationCode LIKE @Search)");

        var where = "WHERE " + string.Join(" AND ", conditions);
        var p = new { StatusId = statusId, ChannelId = channelId, Search = $"%{query.Search}%", Offset = (query.Page-1)*query.PageSize, query.PageSize };

        var countSql = $"SELECT COUNT(*) FROM Reservations r JOIN Guests g ON r.GuestId=g.Id {where}";
        var dataSql  = $@"
            SELECT r.*, g.FullName AS GuestName, g.Phone AS GuestPhone, rm.RoomNumber, rm.RoomType AS RoomTypeName
            FROM Reservations r
            JOIN Guests  g  ON r.GuestId = g.Id
            JOIN Rooms   rm ON r.RoomId  = rm.Id
            {where}
            ORDER BY r.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var total = await conn.ExecuteScalarAsync<int>(countSql, p);
        var items = await conn.QueryAsync<Reservation>(dataSql, p);
        return new PagedResult<Reservation> { Items = items, TotalCount = total, Page = query.Page, PageSize = query.PageSize };
    }

    public async Task<Reservation?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = @"
            SELECT r.*, g.FullName AS GuestName, g.Phone AS GuestPhone, rm.RoomNumber, rm.RoomType AS RoomTypeName
            FROM Reservations r
            JOIN Guests  g  ON r.GuestId = g.Id
            JOIN Rooms   rm ON r.RoomId  = rm.Id
            WHERE r.Id=@Id AND r.IsDeleted=0";
        return await conn.QueryFirstOrDefaultAsync<Reservation>(sql, new { Id = id });
    }

    public async Task<Reservation?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Reservation>(
            "SELECT * FROM Reservations WHERE ReservationCode=@Code AND IsDeleted=0", new { Code = code });
    }

    public async Task<int> CreateAsync(Reservation entity, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = @"
            INSERT INTO Reservations
                (ReservationCode,GuestId,RoomId,CheckIn,CheckOut,Nights,Adults,Children,
                 RatePerNight,Subtotal,GstAmount,TotalAmount,AdvancePaid,BalanceDue,
                 Status,Channel,PaymentMethod,SpecialRequests,CreatedAt)
            OUTPUT INSERTED.Id
            VALUES
                (@ReservationCode,@GuestId,@RoomId,@CheckIn,@CheckOut,@Nights,@Adults,@Children,
                 @RatePerNight,@Subtotal,@GstAmount,@TotalAmount,@AdvancePaid,@BalanceDue,
                 @Status,@Channel,@PaymentMethod,@SpecialRequests,@CreatedAt)";
        return await conn.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<bool> UpdateAsync(Reservation entity, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = @"
            UPDATE Reservations SET Adults=@Adults, Children=@Children, RatePerNight=@RatePerNight,
            Channel=@Channel, PaymentMethod=@PaymentMethod, Status=@Status,
            SpecialRequests=@SpecialRequests, CancellationReason=@CancellationReason,
            UpdatedAt=@UpdatedAt WHERE Id=@Id AND IsDeleted=0";
        return await conn.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<bool> UpdateReservationAndRoomStatusAsync(int reservationId, ReservationStatus reservationStatus, RoomStatus roomStatus, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var roomId = await conn.ExecuteScalarAsync<int?>(
                "SELECT RoomId FROM Reservations WHERE Id=@Id AND IsDeleted=0",
                new { Id = reservationId }, tx);

            if (!roomId.HasValue)
            {
                tx.Rollback();
                return false;
            }

            var now = DateTime.UtcNow;
            var updatedRes = await conn.ExecuteAsync(
                "UPDATE Reservations SET Status=@Status, UpdatedAt=@Now WHERE Id=@Id AND IsDeleted=0",
                new { Status = (int)reservationStatus, Now = now, Id = reservationId }, tx);

            if (updatedRes == 0)
            {
                tx.Rollback();
                return false;
            }

            var updatedRoom = await conn.ExecuteAsync(
                "UPDATE Rooms SET Status=@Status, UpdatedAt=@Now WHERE Id=@RoomId AND IsDeleted=0",
                new { Status = (int)roomStatus, Now = now, RoomId = roomId.Value }, tx);

            if (updatedRoom == 0)
            {
                tx.Rollback();
                return false;
            }

            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        return await conn.ExecuteAsync("UPDATE Reservations SET IsDeleted=1, UpdatedAt=@Now WHERE Id=@Id",
            new { Now = DateTime.UtcNow, Id = id }) > 0;
    }

    public async Task<bool> RoomIsAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeId = null, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = @"
            SELECT COUNT(*) FROM Reservations
            WHERE RoomId=@RoomId AND IsDeleted=0
              AND Status NOT IN (4,5,6) -- CheckedOut=4, Cancelled=5, NoShow=6
              AND (@CheckIn  < CheckOut)
              AND (@CheckOut > CheckIn)
              AND (@ExcludeId IS NULL OR Id <> @ExcludeId)";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { RoomId=roomId, CheckIn=checkIn, CheckOut=checkOut, ExcludeId=excludeId });
        return count == 0;
    }

    private static int? ParseEnumOrNumeric<TEnum>(string? value) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (int.TryParse(value, out var numeric) && Enum.IsDefined(typeof(TEnum), numeric)) return numeric;
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? Convert.ToInt32(parsed) : null;
    }
}
