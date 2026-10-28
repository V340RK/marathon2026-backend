using APIHandler.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Tables;

public interface ITableRepository
{
    Task<Table?> GetTableAsync(string id);
    Task<Table?> GetTableByIdAsync(string tableId);
    Task<IEnumerable<Table>> GetAllTablesAsync();
    Task<IEnumerable<Table>> GetTablesByLocationIdAsync(string locationId);
    Task SaveTableAsync(Table table);
    Task DeleteTableAsync(Table table);
}