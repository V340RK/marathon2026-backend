using Amazon.DynamoDBv2.DataModel;

namespace APIHandler.Constants;

public static class DynamoConfig
{
    public static LoadConfig LoadConfig(string tableName) => new LoadConfig
    {
        ConsistentRead = true,
        OverrideTableName = tableName
    };

    public static SaveConfig SaveConfig(string tableName) => new SaveConfig
    {
        OverrideTableName = tableName
    };

    public static DeleteConfig DeleteConfig(string tableName) => new DeleteConfig
    {
        OverrideTableName = tableName
    };

    public static ScanConfig ScanConfig(string tableName) => new ScanConfig
    {
        OverrideTableName = tableName
    };

    public static QueryConfig QueryConfig(string tableName) => new QueryConfig
    {
        OverrideTableName = tableName,
        ConsistentRead = true
    };
}