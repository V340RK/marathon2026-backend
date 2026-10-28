using Amazon.CognitoIdentityProvider.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReportSender.Services;

public interface IUserService
{
    Task<List<UserType>> GetUsersByGroupAsync(string groupName = "Waiter");
}