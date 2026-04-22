using System.Text.Json;
using System.Net.Http.Json;

namespace SalesDataAnalyzer.Services.VectorDb;

public class ChromaService : IChromaService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public ChromaService(string baseUrl = "http://localhost:8001")
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Chroma .NET Client");
    }

    public async Task InitializeCollectionAsync(string collectionName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/collection/{collectionName}/count");
            if (response.IsSuccessStatusCode)
                return;
        }
        catch
        {
        }
    }

    public async Task<string> AddDocumentAsync(string collectionName, string document, Dictionary<string, string> metadata)
    {
        var request = new
        {
            documents = new[] { document },
            metadatas = new[] { metadata }
        };

        var response = await _httpClient.PostAsJsonAsync($"/api/collection/{collectionName}/add", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AddResult>();
        return result?.Ids?[0] ?? string.Empty;
    }

    public async Task<List<(string Document, float Score, Dictionary<string, string> Metadata)>> QuerySimilarDocumentsAsync(string collectionName, string query, int nResults = 5)
    {
        var request = new
        {
            query_texts = new[] { query },
            n_results = nResults
        };

        var response = await _httpClient.PostAsJsonAsync($"/api/collection/{collectionName}/query", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<QueryResult>();
        
        var results = new List<(string, float, Dictionary<string, string>)>();
        if (result != null && result.Documents != null && result.Distances != null && result.Metadatas != null)
        {
            for (int i = 0; i < result.Documents[0].Length; i++)
            {
                var metadataDict = new Dictionary<string, string>();
                if (result.Metadatas[0][i] != null)
                {
                    foreach (var kvp in result.Metadatas[0][i])
                    {
                        metadataDict[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
                    }
                }
                
                results.Add((result.Documents[0][i], result.Distances[0][i], metadataDict));
            }
        }

        return results;
    }

    public async Task DeleteDocumentAsync(string collectionName, string documentId)
    {
        throw new NotImplementedException();
    }

    public async Task<int> GetCollectionCountAsync(string collectionName)
    {
        var response = await _httpClient.GetAsync($"/api/collection/{collectionName}/count");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CountResult>();
        return result?.Count ?? 0;
    }
}

public class AddResult
{
    public string[]? Ids { get; set; }
}

public class QueryResult
{
    public string[][]? Documents { get; set; }
    public float[][]? Distances { get; set; }
    public Dictionary<string, object?>[][]? Metadatas { get; set; }
}

public class CountResult
{
    public int Count { get; set; }
}