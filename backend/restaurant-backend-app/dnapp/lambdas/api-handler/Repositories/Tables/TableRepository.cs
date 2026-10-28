using Amazon.DynamoDBv2.DataModel;
using APIHandler.Constants;
using APIHandler.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Tables;

public class TableRepository(IDynamoDBContext context) : ITableRepository
{
    public async Task<Table?> GetTableAsync(string id) =>
        await context.LoadAsync<Table>(id, new LoadConfig { OverrideTableName = ResourceNames.TablesTableName });

    public async Task<Table?> GetTableByIdAsync(string tableId) =>
        await GetTableAsync(tableId);

    public async Task<IEnumerable<Table>> GetAllTablesAsync()
    {
        return await context
            .ScanAsync<Table>(new List<ScanCondition>(), new ScanConfig { OverrideTableName = ResourceNames.TablesTableName })
            .GetRemainingAsync();
    }

    public async Task<IEnumerable<Table>> GetTablesByLocationIdAsync(string locationId)
    {
        var queryConfig = new QueryConfig
        {
            OverrideTableName = ResourceNames.TablesTableName,
            IndexName = ResourceNames.TablesLocationIdIndex,
            ConsistentRead = false
        };

        return await context.QueryAsync<Table>(locationId, queryConfig).GetRemainingAsync();
    }

    public async Task SaveTableAsync(Table table)
    {
        await context.SaveAsync(table, new SaveConfig { OverrideTableName = ResourceNames.TablesTableName });
    }

    public async Task DeleteTableAsync(Table table)
    {
        await context.DeleteAsync(table, new DeleteConfig { OverrideTableName = ResourceNames.TablesTableName });
    }
}