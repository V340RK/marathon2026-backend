using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Helpers;
using APIHandler.Models.Users;
using APIHandler.Services;
using System;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Users;

public class GetUserProfileImageHandler : IApiRequestHandler
{
    private static readonly string ProfileImagesTableName = ResourceNames.ProfileImagesTableName;
    private static readonly string BucketName = ResourceNames.ProfileImagesBucketName;

    private readonly IDynamoDBContext _dbContext;
    private readonly S3ImageService _s3ImageService;

    public GetUserProfileImageHandler(IDynamoDBContext dbContext, S3ImageService s3ImageService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _s3ImageService = s3ImageService ?? throw new ArgumentNullException(nameof(s3ImageService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!request.PathParameters.TryGetValue("id", out var userId) || string.IsNullOrEmpty(userId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "User ID is required.", context.Logger);
        }

        var userImage = await _dbContext.LoadAsync<ProfileImage>(userId, new DynamoDBOperationConfig
        {
            OverrideTableName = ProfileImagesTableName
        });

        if (userImage == null || string.IsNullOrEmpty(userImage.ImageKey))
        {
            return ResponseBuilder.CreateSuccessResponse("Profile image is not uploaded so far.", 200);
        }

        var presignedUrl = _s3ImageService.GetPreSignedImageUrl(BucketName, userImage.ImageKey);

        return ResponseBuilder.CreateJsonResponse(200, new ProfileImageResponse()
        {
            PresignedUrl = presignedUrl
        });
    }

}