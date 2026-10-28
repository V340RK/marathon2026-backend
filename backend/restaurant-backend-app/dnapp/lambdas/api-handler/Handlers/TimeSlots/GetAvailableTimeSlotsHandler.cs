using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Helpers;
using APIHandler.Models.TimeSlots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.TimeSlots;

public class GetAvailableTimeSlotsHandler : IApiRequestHandler
{
    private static readonly string ReservationsTableName = ResourceNames.ReservationsTableName;
    private static readonly string ReservationsTableGlobalIndex = ResourceNames.ReservationsTableGlobalIndex;

    private static readonly TimeSpan ReservationDuration = TimeSpan.FromMinutes(90);
    private static readonly TimeSpan GapDuration = TimeSpan.FromMinutes(15);

    private readonly IDynamoDBContext _dbContext;

    public GetAvailableTimeSlotsHandler(IDynamoDBContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        // /tables/{id}/available-time-slots?date
        var pathParams = request.PathParameters;
        var queryParams = request.QueryStringParameters ?? new Dictionary<string, string>();

        if (pathParams == null ||
            !pathParams.TryGetValue("id", out var tableId) ||
            string.IsNullOrWhiteSpace(tableId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "No table id was found in request.", context.Logger);
        }

        if (!queryParams.TryGetValue("startTime", out var startTimeStr) || !DateTime.TryParse(startTimeStr, out var startTime))
        {
            return ResponseBuilder.CreateErrorResponse(
                400,
                "Missing or invalid 'startTime' parameter. Format: ISO 8601 (e.g., 2023-10-01T00:00:00Z).",
                context.Logger);
        }

        var queryConfig = DynamoConfig.QueryConfig(ReservationsTableName);
        queryConfig.IndexName = ReservationsTableGlobalIndex;
        queryConfig.ConsistentRead = false;
        var reservations = await _dbContext
            .QueryAsync<Reservation>(tableId, queryConfig)
            .GetRemainingAsync();

        context.Logger.Log($"Table id: {tableId}");
        context.Logger.Log($"Reservations: {JsonSerializer.Serialize(reservations)}");

        var workingDayStart = new DateTime(startTime.Year, startTime.Month, startTime.Day, 10, 0, 0, DateTimeKind.Unspecified);
        var workingDayEnd = new DateTime(startTime.Year, startTime.Month, startTime.Day, 22, 0, 0, DateTimeKind.Unspecified);

        var slots = new List<TimeSlotResponse>();
        var marginTo90Minutes =
            ReservationDuration.TotalMinutes - ((startTime - workingDayStart).TotalMinutes % ReservationDuration.TotalMinutes);
        DateTime current;
        if (marginTo90Minutes < ReservationDuration.TotalMinutes)
        {
            current = startTime.AddMinutes(marginTo90Minutes);
        }
        else
        {
            current = startTime;
        }

        while (current + ReservationDuration <= workingDayEnd)
        {
            var potentialStart = current;
            var potentialEnd = potentialStart + ReservationDuration;

            var overlappingReservation = reservations
                .FirstOrDefault(r => IsOverlapping(current, current + ReservationDuration, r.StartTime, r.EndTime));

            if (overlappingReservation == null)
            {
                slots.Add(new TimeSlotResponse
                {
                    Start = potentialStart,
                    End = potentialEnd
                });

                current = potentialEnd + GapDuration;
            }
            else
            {
                // end time of reservation + gap
                current = overlappingReservation.EndTime.Add(GapDuration);
            }
        }

        return ResponseBuilder.CreateSuccessResponse(slots, 200);
    }

    bool IsOverlapping(DateTime slotStart, DateTime slotEnd, DateTime reservationStart, DateTime reservationEnd)
    {
        return slotStart < reservationEnd && reservationStart < slotEnd;
    }
}