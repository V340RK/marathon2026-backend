using System.Threading.Tasks;

namespace ReportSender.Services;

public interface ILocationService
{
    Task<string> GetLocationAddressByIdAsync(string locationId);
}