using Amazon.DynamoDBv2.DataModel;
using APIHandler.Constants;
using APIHandler.Models.Feedbacks;
using System;
using System.Collections.Generic;

using System.Threading.Tasks;

namespace APIHandler.Repositories.Feedbacks;

public class FeedbackRepository : IFeedbackRepository
{
    private static readonly string FeedbackTableName = ResourceNames.FeedbackTableName;
    private readonly IDynamoDBContext _dbContext;

    public FeedbackRepository(IDynamoDBContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<List<Feedback>> GetFeedbackByLocationIdAsync(string locationId)
    {
        var queryConfig = DynamoConfig.QueryConfig(FeedbackTableName);
        queryConfig.IndexName = ResourceNames.FeedbackGlobalIndex;
        queryConfig.ConsistentRead = false;

        return await _dbContext.QueryAsync<Feedback>(locationId, queryConfig).GetRemainingAsync();
    }

    public async Task<List<Feedback>> GetFeedbackByReservationIdAsync(string reservationId)
    {
        var queryConfig = DynamoConfig.QueryConfig(FeedbackTableName);
        var feedback = await _dbContext.QueryAsync<Feedback>(reservationId, queryConfig).GetRemainingAsync();

        return feedback ?? new List<Feedback>();
    }

    public async Task SaveFeedbackAsync(Feedback feedback)
    {
        var saveConfig = DynamoConfig.SaveConfig(FeedbackTableName);
        await _dbContext.SaveAsync(feedback, saveConfig);
    }
}
