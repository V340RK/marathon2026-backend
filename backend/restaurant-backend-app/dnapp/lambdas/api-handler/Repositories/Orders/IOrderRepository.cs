using APIHandler.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Orders;

public interface IOrderRepository
{
    Task<Order?> GetOrderByIdAsync(string orderId, string customerId);
    Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(string customerId);
    Task<Order?> GetOrderByReservationIdAsync(string reservationId);
    Task SaveOrderAsync(Order order);
}

