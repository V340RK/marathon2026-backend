using Amazon.DynamoDBv2.DataModel;
using APIHandler.Constants;
using APIHandler.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Reservations;

public class ReservationRepository : IReservationRepository
{
    private static readonly string ReservationsTableName = ResourceNames.ReservationsTableName;
    private readonly IDynamoDBContext _dynamoDbContext;

    public ReservationRepository(IDynamoDBContext dynamoDbContext)
    {
        _dynamoDbContext = dynamoDbContext ?? throw new ArgumentNullException(nameof(dynamoDbContext));
    }

    public async Task<Reservation?> GetReservationByIdAsync(string reservationId)
    {
        return await _dynamoDbContext.LoadAsync<Reservation>(reservationId, DynamoConfig.LoadConfig(ReservationsTableName));
    }

    public async Task<List<Reservation>> GetReservationsByTableIdAsync(string tableId)
    {
        var queryConfig = DynamoConfig.QueryConfig(ReservationsTableName);
        queryConfig.IndexName = ResourceNames.ReservationsTableGlobalIndex;
        queryConfig.ConsistentRead = false;

        return await _dynamoDbContext.QueryAsync<Reservation>(tableId, queryConfig).GetRemainingAsync();
    }

    public async Task<IEnumerable<Reservation>> GetReservationsByWaiterIdAsync(string waiterId)
    {
        var queryConfig = DynamoConfig.QueryConfig(ReservationsTableName);
        queryConfig.IndexName = ResourceNames.ReservationsWaiterIdGlobalIndex;
        queryConfig.ConsistentRead = false;

        return await _dynamoDbContext
            .QueryAsync<Reservation>(waiterId, queryConfig)
            .GetRemainingAsync();
    }

    public async Task<IEnumerable<Reservation>> GetReservationsByCustomerIdAsync(string userId)
    {
        var queryConfig = DynamoConfig.QueryConfig(ReservationsTableName);
        queryConfig.IndexName = ResourceNames.ReservationsCustomerIdGlobalIndex;
        queryConfig.ConsistentRead = false;

        return await _dynamoDbContext
            .QueryAsync<Reservation>(userId, queryConfig)
            .GetRemainingAsync();
    }

    public async Task SaveReservationAsync(Reservation reservation)
    {
        await _dynamoDbContext.SaveAsync(reservation, DynamoConfig.SaveConfig(ReservationsTableName));
    }
}