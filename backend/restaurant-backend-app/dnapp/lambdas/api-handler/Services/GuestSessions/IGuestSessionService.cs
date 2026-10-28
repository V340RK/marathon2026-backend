using APIHandler.Entities;
using APIHandler.Models.GuestSessions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Services.GuestSessions;

public interface IGuestSessionService
{
    Task<CreatGuestSessionResponse> CreateGuestSessionAsync(CreateGuestSessionRequest request);
    Task AddDishAsync(string guestSessionId, string dishId, int quantity);
    Task RemoveDishAsync(string guestSessionId, string dishId, int quantity);
    Task<GroupReservationStatusResponse> GetGroupReservationStatusAsync(string groupReservationId);
    Task<bool> IsAuthorizedAsync(string guestSessionToken, string guestSessionId);
    Task SubmitCartAsync(string guestSessionId);
    Task<GuestMenuResponse> GetGuestMenuAsync(string guestSessionId);
    Task<GetAllSessionsResponse> DeleteGuestSessionAsync(string userId, string groupReservationId, string guestSessionId);
    Task<GetAllSessionsResponse> GetAllSessionsAsync(string userId, string groupReservationId);
    Task<OwnerGuestSessionResponse> GetOwnerGuestSessionIdAsync(string userId, string reservationId);
    Task<IReadOnlyList<SubmittedDishResponse>> SubmitGroupPreOrderAsync(string userId, string groupReservationId, string reservationId);
    Task<GetAllSessionsResponse> SetGuestBillTypeAsync(string userId, string groupReservationId, string guestSessionId, BillType billType);
    Task PayOwnOrderAsync(string guestSessionId);
    Task<GetGuestSessionResponse> GetGuestSessionInfoAsync(string guestSessionId);
}

