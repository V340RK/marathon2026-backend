using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.S3;
using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Helpers;
using APIHandler.Models.Users;
using APIHandler.Services;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Users;

public class UploadUserProfileImageHandler : IApiRequestHandler
{
    private static readonly string ProfileImagesTableName = ResourceNames.ProfileImagesTableName;
    private static readonly string UsersImageFolderName = "profile-img";
    private readonly string _bucketName = ResourceNames.ProfileImagesBucketName;

    private readonly IDynamoDBContext _dbContext;
    private readonly S3ImageService _s3ImageService;

    public UploadUserProfileImageHandler(IDynamoDBContext dbContext, S3ImageService s3ImageService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _s3ImageService = s3ImageService ?? throw new ArgumentNullException(nameof(s3ImageService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var body = JsonSerializer.Deserialize<UploadProfileImageRequest>(request.Body!);
        if (body == null)
            return ResponseBuilder.CreateErrorResponse(400, "Invalid request.", context.Logger);

        context.Logger.Log(JsonSerializer.Serialize(request.RequestContext.Authorizer));
        if (request.RequestContext.Authorizer is null ||
            request.RequestContext.Authorizer.Claims is null ||
            !request.RequestContext.Authorizer.Claims.TryGetValue("sub", out var userId))
        {
            return ResponseBuilder.CreateErrorResponse(401, "User is not authenticated.", context.Logger);
        }

        var fileName = $"user_{userId}";
        var imageBytes = S3ImageService.GetImageBytesFromBase64(body.Base64Image);

        var userImage = await _dbContext.LoadAsync<ProfileImage>(userId, new DynamoDBOperationConfig
        {
            OverrideTableName = ProfileImagesTableName
        });

        var oldKey = userImage?.ImageKey;

        if (!string.IsNullOrEmpty(oldKey))
        {
            try
            {
                var getObject = await _s3ImageService.DownloadImageAsync(_bucketName, oldKey);
                if (S3ImageService.CompareImagesHash(imageBytes, getObject))
                {
                    var url = _s3ImageService.GetPreSignedImageUrl(_bucketName, oldKey);
                    return ResponseBuilder.CreateJsonResponse(200, new ProfileImageResponse { PresignedUrl = url });
                }

                // delete old img before uploading a new one
                await _s3ImageService.DeleteImageAsync(_bucketName, oldKey);
            }
            catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                // old image doesn't exist, so just skip
            }
        }

        // upload new image
        var key = await _s3ImageService.UploadImageAsync(imageBytes, _bucketName, fileName, context.Logger, UsersImageFolderName);

        await _dbContext.SaveAsync(new ProfileImage
        {
            UserId = userId,
            ImageKey = key
        }, new DynamoDBOperationConfig { OverrideTableName = ProfileImagesTableName });

        return ResponseBuilder.CreateJsonResponse(200, "Photo was uploaded successfully!");
    }
}