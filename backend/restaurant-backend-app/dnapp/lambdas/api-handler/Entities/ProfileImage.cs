using Amazon.DynamoDBv2.DataModel;

namespace APIHandler.Entities;

[DynamoDBTable("ProfileImages")]
public class ProfileImage
{
    [DynamoDBHashKey("userId")]
    public required string UserId { get; set; }
    
    [DynamoDBProperty("imageKey")]
    public required string ImageKey { get; set; }
}