using MirDev.ChromaDB.Client.V2;
using SalesDataAnalyzer.Services.AI;

namespace SalesDataAnalyzer.Services.VectorDb;

public class ChromaService : IChromaService
{
    private readonly ChromaClient _chromaClient;
    private readonly IEmbeddingService _embeddingService;
    private const string Tenant = "default_tenant";
    private const string Database = "default_database";

    public ChromaService(IEmbeddingService embeddingService)
    {
        _chromaClient = new ChromaClient("http://localhost:8000");
        _embeddingService = embeddingService;
    }

    private async Task<Guid> GetOrCreateCollectionIdAsync(string collectionName)
    {
        try
        {
            var collections = await _chromaClient.ListCollectionsAsync(Database, Tenant);
            var existing = collections.FirstOrDefault(c => c.Name == collectionName);
            if (existing != null)
            {
                return existing.Id;
            }

            var newCollection = await _chromaClient.CreateCollectionAsync(
                name: collectionName,
                database: Database,
                tenant: Tenant
            );
            return newCollection.Id;
        }
        catch
        {
            var collections = await _chromaClient.ListCollectionsAsync(Database, Tenant);
            var existing = collections.FirstOrDefault(c => c.Name == collectionName);
            return existing?.Id ?? Guid.Empty;
        }
    }

    public async Task InitializeCollectionAsync(string collectionName)
    {
        await GetOrCreateCollectionIdAsync(collectionName);
    }

    public async Task<string> AddDocumentAsync(string collectionName, string document, Dictionary<string, string> metadata)
    {
        var collectionId = await GetOrCreateCollectionIdAsync(collectionName);
        if (collectionId == Guid.Empty)
        {
            throw new Exception($"Collection {collectionName} not found");
        }

        var count = await _chromaClient.CountRecordsAsync(Database, Tenant, collectionId);
        var id = (count + 1).ToString();

        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(document);

        var metadatas = new List<Dictionary<string, object>>
        {
            metadata.ToDictionary(k => k.Key, k => (object)k.Value)
        };

        await _chromaClient.AddRecordsAsync(
            database: Database,
            tenant: Tenant,
            collectionId: collectionId,
            ids: new List<string> { id },
            embeddings: new List<List<float>> { embeddings },
            documents: new List<string> { document },
            metadatas: metadatas
        );

        return id;
    }

    public async Task<List<(string Document, float Score, Dictionary<string, string> Metadata)>> QuerySimilarDocumentsAsync(string collectionName, string query, string? siteName = null, int nResults = 5)
    {
        var collectionId = await GetOrCreateCollectionIdAsync(collectionName);
        if (collectionId == Guid.Empty)
        {
            return new List<(string, float, Dictionary<string, string>)>();
        }

        var queryEmbeddings = await _embeddingService.GenerateEmbeddingsAsync(query);

        Dictionary<string, object>? whereClause = null;
        if (!string.IsNullOrEmpty(siteName))
        {
            whereClause = new Dictionary<string, object>
            {
                ["site_name"] = new Dictionary<string, object> { ["$eq"] = siteName }
            };
        }

        var results = await _chromaClient.QueryRecordsAsync(
            database: Database,
            tenant: Tenant,
            collectionId: collectionId,
            queryEmbeddings: new List<List<float>> { queryEmbeddings },
            nResults: nResults,
            where: whereClause,
            include: new List<Include> { Include.documents, Include.metadatas, Include.distances }
        );

        var queryResults = new List<(string, float, Dictionary<string, string>)>();
        if (results?.Ids != null && results.Ids.Count > 0)
        {
            for (int i = 0; i < results.Ids[0].Count; i++)
            {
                var metadataDict = new Dictionary<string, string>();
                if (results.Metadatas != null && results.Metadatas.Count > 0 && results.Metadatas[0] != null && results.Metadatas[0].Count > i)
                {
                    foreach (var kvp in results.Metadatas[0][i])
                    {
                        metadataDict[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
                    }
                }

                float distance = results.Distances != null && results.Distances.Count > 0 && results.Distances[0].Count > i
                    ? (float)results.Distances[0][i]
                    : float.MaxValue;

                string doc = results.Documents != null && results.Documents.Count > 0 && results.Documents[0].Count > i
                    ? results.Documents[0][i] ?? string.Empty
                    : string.Empty;

                queryResults.Add((doc, distance, metadataDict));
            }
        }

        return queryResults;
    }

    public async Task DeleteDocumentAsync(string collectionName, string documentId)
    {
        var collectionId = await GetOrCreateCollectionIdAsync(collectionName);
        if (collectionId != Guid.Empty)
        {
            await _chromaClient.DeleteRecordsAsync(Database, Tenant, collectionId, new List<string> { documentId });
        }
    }

    public async Task<int> GetCollectionCountAsync(string collectionName)
    {
        var collectionId = await GetOrCreateCollectionIdAsync(collectionName);
        if (collectionId == Guid.Empty) return 0;

        return await _chromaClient.CountRecordsAsync(Database, Tenant, collectionId);
    }

    public async Task<List<string>> GetAllSitesAsync(string collectionName)
    {
        var collectionId = await GetOrCreateCollectionIdAsync(collectionName);
        if (collectionId == Guid.Empty) return new List<string>();

        var results = await _chromaClient.QueryRecordsAsync(
            database: Database,
            tenant: Tenant,
            collectionId: collectionId,
            queryEmbeddings: new List<List<float>> { new List<float>() },
            nResults: 1000,
            include: new List<Include> { Include.metadatas }
        );

        var sites = new HashSet<string>();
        if (results?.Metadatas != null)
        {
            foreach (var metadataList in results.Metadatas)
            {
                if (metadataList != null)
                {
                    foreach (var metadata in metadataList)
                    {
                        if (metadata.TryGetValue("site_name", out var site))
                        {
                            sites.Add(site?.ToString() ?? string.Empty);
                        }
                    }
                }
            }
        }

        return sites.Where(s => !string.IsNullOrEmpty(s)).ToList();
    }
}