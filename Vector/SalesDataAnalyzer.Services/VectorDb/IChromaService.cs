using SalesDataAnalyzer.Models;

namespace SalesDataAnalyzer.Services.VectorDb;

public interface IChromaService
{
    Task InitializeCollectionAsync(string collectionName);
    Task<string> AddDocumentAsync(string collectionName, string document, Dictionary<string, string> metadata);
    Task<List<(string Document, float Score, Dictionary<string, string> Metadata)>> QuerySimilarDocumentsAsync(string collectionName, string query, int nResults = 5);
    Task DeleteDocumentAsync(string collectionName, string documentId);
    Task<int> GetCollectionCountAsync(string collectionName);
}