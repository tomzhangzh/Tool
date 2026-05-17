using SalesDataAnalyzer.Services.VectorDb;

namespace SalesDataAnalyzer.Services.AI;

public class RAGQuestionAnsweringService
{
    private readonly IChromaService _chromaService;
    private readonly ILLMService _llmService;

    public RAGQuestionAnsweringService(IChromaService chromaService, ILLMService llmService)
    {
        _chromaService = chromaService;
        _llmService = llmService;
    }

    public async Task<string> AskAsync(string question, string? siteName = null, int topK = 5)
    {
        var searchResults = await _chromaService.QuerySimilarDocumentsAsync(
            "sales_data_collection",
            question,
            siteName,
            topK
        );

        if (!searchResults.Any())
        {
            // 抱歉，我在向量数据库中没有找到与您问题相关的信息。
            return "Sorry, I didn't find any information related to your question in the vector database.";
        }

        var context = BuildContext(searchResults, siteName);

        var answer = await _llmService.GenerateResponseAsync(question, context);

        return answer;
    }

    private string BuildContext(List<(string Document, float Score, Dictionary<string, string> Metadata)> results, string? siteName)
    {
        var sb = new System.Text.StringBuilder();

        if (!string.IsNullOrEmpty(siteName))
        {
            // 以下是门店【{siteName}】检索到的相关销售数据：
            sb.AppendLine($"Below are the related sales data retrieved for site [{siteName}]:");
        }
        else
        {
            // 以下是检索到的相关销售数据：
            sb.AppendLine("Below are the related sales data retrieved:");
        }
        sb.AppendLine();

        int index = 1;
        foreach (var (document, score, metadata) in results)
        {
            // 【数据 {index}】
            sb.AppendLine($"[Data {index}]");
            // 相关内容：
            sb.AppendLine($"Related Content: {document}");
            // 匹配度：
            sb.AppendLine($"Match Score: {score:F4}");

            if (metadata.Any())
            {
                // 详细信息：
                sb.AppendLine("Detailed Information:");
                foreach (var kvp in metadata)
                {
                    sb.AppendLine($"  - {kvp.Key}: {kvp.Value}");
                }
            }
            sb.AppendLine();
            index++;
        }

        return sb.ToString();
    }
}