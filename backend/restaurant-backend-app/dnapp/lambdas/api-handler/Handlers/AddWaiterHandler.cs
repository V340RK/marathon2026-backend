using Amazon.CognitoIdentityProvider.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models.WaiterList;
using APIHandler.Services.Waiters;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers;

public class AddWaiterHandler : IApiRequestHandler
{
    private readonly IWaiterService _waiterService;

    public AddWaiterHandler(IWaiterService waiterService)
    {
        _waiterService = waiterService ?? throw new ArgumentNullException(nameof(waiterService));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!TryDeserializeRequest(request, out var waiterListRequest))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Invalid request body", context.Logger);
        }

        try
        {
            PersonalDataValidator.ValidateEmail(waiterListRequest.Email);
            return await HandlePostRequest(waiterListRequest, context);
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorResponse(400, ex.Message, context.Logger);
        }
    }

    private async Task<APIGatewayProxyResponse> HandlePostRequest(UpdateWaiterListRequest request, ILambdaContext context)
    {
        if (await _waiterService.IsWaiterInListAsync(request.Email))
        {
            return ResponseBuilder.CreateErrorResponse(409, "This user is already added to the waiters list", context.Logger);
        }

        try
        {
            await _waiterService.AddWaiterToListAsync(request);
        }
        catch (UserNotFoundException)
        {
            return ResponseBuilder.CreateErrorResponse(404, $"User with email {request.Email} is not registered", context.Logger);
        }

        return ResponseBuilder.CreateSuccessResponse("Email added successfully");
    }

    private static bool TryDeserializeRequest(APIGatewayProxyRequest request, [NotNullWhen(true)] out UpdateWaiterListRequest? registrationRequest)
    {
        try
        {
            registrationRequest = JsonSerializer.Deserialize<UpdateWaiterListRequest>(request.Body);
            return registrationRequest != null;
        }
        catch (JsonException)
        {
            registrationRequest = null;
            return false;
        }
    }
}