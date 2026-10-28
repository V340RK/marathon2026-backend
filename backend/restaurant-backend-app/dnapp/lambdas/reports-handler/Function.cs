using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using ReportsHandler.Entities;
using ReportsHandler.Models;
using ReportsHandler.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ReportsHandler;

public class Function
{
    private readonly IReportService _reportService = new ReportService();

    public async Task<SQSBatchResponse> FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        var failures = new List<SQSBatchResponse.BatchItemFailure>();
        foreach (var record in sqsEvent.Records)
        {
            await TryHandleMessageAsync(record, failures, context.Logger);
        }
        return new SQSBatchResponse { BatchItemFailures = failures };
    }

    private async Task TryHandleMessageAsync(SQSEvent.SQSMessage message, List<SQSBatchResponse.BatchItemFailure> failures, ILambdaLogger logger)
    {
        try
        {
            logger.LogLine($"Processing message#{message.MessageId}");
            var processed = await ProcessMessageAsync(message.Body, logger);
            logger.LogLine($"Processed message#{message.MessageId} with result: {processed}");
            if (!processed)
            {
                failures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
                logger.LogError($"Failed to process message#{message.MessageId}");
            }
            else
            {
                logger.LogInformation($"Successfully processed message#{message.MessageId}");
            }
        }
        catch (Exception ex)
        {
            failures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
            logger.LogError($"TRANSIENT_ERROR: Error processing message#{message.MessageId}: {ex.Message}");
        }
    }

    private async Task<bool> ProcessMessageAsync(string message, ILambdaLogger logger)
    {
        if (!TryDeserializeMessage(message, out var report))
        {
            logger.LogError($"PERMANENT_ERROR: Failed to deserialize message: {message}");
            return true; // Returning true to avoid SQS redelivery, as the message is malformed
        }

        DailyAggregate aggregate;
        try
        {
            aggregate = await _reportService.GetOrCreateDailyAggregateAsync(report!.WaiterId, report.ReservationStart.Date);
        }
        catch (Exception ex)
        {
            logger.LogError($"TRANSIENT_ERROR: Error retrieving or creating daily aggregate: {ex.Message}");
            return false;
        }

        try
        {
            UpdateAggregate(aggregate, report);
            await _reportService.UpdateDailyAggregateAsync(aggregate);
            logger.LogInformation($"Successfully updated aggregate for waiter {report.WaiterId} on {report.ReservationStart.Date}");
            return true;
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError($"PERMANENT_ERROR: Validation error while updating daily aggregate: {ex.Message}");
            return true; // Returning true to avoid SQS redelivery, as the message is correctly formatted but the data is inconsistent
        }
        catch (Exception ex)
        {
            logger.LogError($"TRANSIENT_ERROR: Error updating daily aggregate: {ex.Message}");
            return false;
        }
    }

    private static bool TryDeserializeMessage(string message, out Report? report)
    {
        try
        {
            report = System.Text.Json.JsonSerializer.Deserialize<Report>(message);
            return IsValidReport(report);
        }
        catch (Exception)
        {
            report = null;
            return false;
        }
    }

    private static bool IsValidReport(Report? report)
    {
        return report is not null &&
               !string.IsNullOrWhiteSpace(report.WaiterId) &&
               !string.IsNullOrWhiteSpace(report.LocationId) &&
               report.ReservationStart != default &&
               report.ReservationEnd != default &&
               report.ReservationEnd > report.ReservationStart &&
               report.ServiceRating >= 1 && report.ServiceRating <= 5 &&
               report.CuisineRating >= 1 && report.CuisineRating <= 5 &&
               report.Revenue >= 0;
    }

    private static void UpdateAggregate(DailyAggregate aggregate, Report report)
    {
        ValidateAggregate(aggregate, report);
        aggregate.AverageServiceRating = CalculateNewAverage(aggregate.AverageServiceRating, aggregate.OrdersProcessed, report.ServiceRating);
        aggregate.MinimumServiceRating = Math.Min(aggregate.MinimumServiceRating, report.ServiceRating);
        aggregate.AverageCuisineRating = CalculateNewAverage(aggregate.AverageCuisineRating, aggregate.OrdersProcessed, report.CuisineRating);
        aggregate.MinimumCuisineRating = Math.Min(aggregate.MinimumCuisineRating, report.CuisineRating);
        aggregate.TotalRevenue += report.Revenue;
        aggregate.HoursWorked += (report.ReservationEnd - report.ReservationStart).TotalHours;
        aggregate.OrdersProcessed++;
    }

    private static void ValidateAggregate(DailyAggregate aggregate, Report report)
    {
        if (string.IsNullOrWhiteSpace(aggregate.LocationId))
        {
            aggregate.LocationId = report.LocationId;
        }
        else if (aggregate.LocationId != report.LocationId)
        {
            throw new InvalidOperationException($"Location ID mismatch: {aggregate.LocationId} vs {report.LocationId}");
        }

        if (aggregate.WaiterId != report.WaiterId)
        {
            throw new InvalidOperationException($"Waiter ID mismatch: {aggregate.WaiterId} vs {report.WaiterId}");
        }

        if (aggregate.Date.Date != report.ReservationStart.Date)
        {
            throw new InvalidOperationException($"Date mismatch: {aggregate.Date} vs {report.ReservationStart.Date}");
        }
    }

    private static double CalculateNewAverage(double currentAverage, int currentCount, int newValue)
        => (currentAverage * currentCount + newValue) / (currentCount + 1);
}
