﻿using OpenTraceability.Interfaces;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using TraceabilityDriver.Models.MongoDB;

namespace TraceabilityDriver.Services
{
    public interface IDatabaseService
    {
        Task<DatabaseReport> GetDatabaseReport();
        Task<List<LogModel>> GetLastErrors(int top = 10);
        Task<List<SyncHistoryItem>> GetLatestSyncs(int top = 10);
        Task InitializeDatabase();
        Task<EPCISQueryDocument> QueryEvents(EPCISQueryParameters options);
        Task<IVocabularyElement?> QueryMasterData(string identifier);
        Task StoreEventsAsync(List<IEvent> events);
        Task StoreMasterDataAsync(List<IVocabularyElement> masterData);
        Task StoreSyncHistory(SyncHistoryItem syncHistory);
        Task ClearDatabaseAsync();
    }
}