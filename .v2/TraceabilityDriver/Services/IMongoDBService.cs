using OpenTraceability.Interfaces;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;

namespace TraceabilityDriver.Services
{
    public interface IMongoDBService
    {
        Task InitializeDatabase();
        Task<EPCISQueryDocument> QueryEvents(EPCISQueryParameters options);
        Task<IVocabularyElement> QueryMasterData(string identifier);
        Task StoreEventsAsync(List<IEvent> events);
        Task StoreMasterDataAsync(List<IVocabularyElement> masterData);
    }
}