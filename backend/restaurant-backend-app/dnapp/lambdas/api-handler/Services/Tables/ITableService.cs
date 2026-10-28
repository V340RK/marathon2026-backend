using APIHandler.Entities;
using APIHandler.Models.Tables;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Services.Tables;

public interface ITableService
{
    Task<Table> CreateTableAsync(CreateTableRequest request);
    Task<IEnumerable<Table>> GetAvailableTablesAsync(string? locationId, int capacity, DateTime startTime);
    Task<IEnumerable<Table>> GetAllTablesForLocationAsync(string locationId);
    Task<Table> UpdateTableAsync(string tableId, UpdateTableRequest request);
    Task DeleteTableAsync(string tableId);
}