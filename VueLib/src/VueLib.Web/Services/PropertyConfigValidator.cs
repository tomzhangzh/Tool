using System.Text.Json;
using System.Text.Json.Nodes;

namespace VueLib.Web.Services;

/// <summary>
/// PropertyConfigJson 结构校验器。
/// 设计器保存组件时调用：校验 JSON 合法、groups/fields 结构正确，
/// 过滤非法字段，并返回规范化后的 JSON。
/// </summary>
public static class PropertyConfigValidator
{
    private static readonly HashSet<string> ValidFieldTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "input", "number", "switch", "select", "textarea",
        "color", "slider", "radio", "checkbox", "icon", "date"
    };

    /// <summary>
    /// 校验并规范化 PropertyConfigJson。
    /// </summary>
    /// <param name="json">原始 JSON</param>
    /// <param name="normalized">规范化后的 JSON（校验失败时为 null）</param>
    /// <returns>(是否合法, 错误消息)</returns>
    public static (bool Ok, string Message) Validate(string? json, out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            normalized = "{}";
            return (true, string.Empty);
        }

        JsonNode? root;
        try { root = JsonNode.Parse(json); }
        catch (JsonException ex)
        {
            return (false, $"PropertyConfigJson 不是合法 JSON: {ex.Message}");
        }

        if (root is not JsonObject obj)
            return (false, "PropertyConfigJson 必须是 JSON 对象");

        // 解析 groups
        var groups = new JsonArray();
        if (obj["groups"] is JsonArray srcGroups)
        {
            foreach (var g in srcGroups)
            {
                if (g is not JsonObject groupObj) continue;
                var title = groupObj["title"]?.GetValue<string>()
                    ?? groupObj["name"]?.GetValue<string>()
                    ?? "属性";

                var fields = new JsonArray();
                if (groupObj["fields"] is JsonArray srcFields)
                {
                    foreach (var f in srcFields)
                    {
                        if (f is not JsonObject fieldObj) continue;
                        var key = fieldObj["key"]?.GetValue<string>();
                        if (string.IsNullOrWhiteSpace(key)) continue;

                        var field = new JsonObject { ["key"] = key };
                        field["label"] = fieldObj["label"]?.GetValue<string>() ?? key;
                        var ftype = fieldObj["type"]?.GetValue<string>() ?? "input";
                        field["type"] = ValidFieldTypes.Contains(ftype) ? ftype.ToLowerInvariant() : "input";
                        if (fieldObj["default"] != null) field["default"] = CloneNode(fieldObj["default"]!);
                        if (fieldObj["placeholder"] != null) field["placeholder"] = CloneNode(fieldObj["placeholder"]!);
                        if (fieldObj["options"] is JsonArray opts) field["options"] = CloneNode(opts);
                        fields.Add(field);
                    }
                }
                if (fields.Count == 0) continue;
                groups.Add(new JsonObject { ["title"] = title, ["fields"] = fields });
            }
        }

        var result = new JsonObject { ["groups"] = groups };
        normalized = result.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        return (true, string.Empty);
    }

    /// <summary>兼容 .NET 7：无 JsonNode.DeepClone，用序列化往返克隆</summary>
    private static JsonNode CloneNode(JsonNode node)
    {
        return JsonNode.Parse(node.ToJsonString())!;
    }
}
