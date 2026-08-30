using System.Text.Json;
using System.Text.Json.Nodes;

namespace VueLib.Web.Services;

/// <summary>
/// 低代码组件树 Schema 版本迁移器。
/// configJson 根节点携带 schemaVersion，读取时自动升级到最新版本。
/// 未来变更数据结构时，只需追加新的迁移函数（MigrateV{n}ToV{n+1}）并更新 LatestVersion。
/// </summary>
public static class ConfigMigrator
{
    /// <summary>当前最新 schema 版本</summary>
    public const int LatestVersion = 1;

    /// <summary>
    /// 确保 configJson 带 schemaVersion；若版本落后则链式迁移到最新。
    /// 返回迁移后的 JSON 字符串。
    /// </summary>
    public static string Migrate(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson)) return "{}";

        JsonNode? root;
        try { root = JsonNode.Parse(configJson); }
        catch { return configJson; }
        if (root is not JsonObject obj) return configJson;

        var versionNode = obj["schemaVersion"];
        int version = versionNode?.GetValue<int>() ?? 0;
        if (version >= LatestVersion) return configJson;

        // 链式迁移：v0 -> v1 -> ... -> Latest
        while (version < LatestVersion)
        {
            version = MigrateStep(obj, version);
        }

        obj["schemaVersion"] = LatestVersion;
        return obj.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    /// <summary>单步迁移：把 obj 从 version 升级到 version+1</summary>
    private static int MigrateStep(JsonObject obj, int version)
    {
        switch (version)
        {
            case 0:
                MigrateV0ToV1(obj);
                return 1;
            // case 1: MigrateV1ToV2(obj); return 2;
            default:
                // 未知版本直接跳到最新
                return LatestVersion;
        }
    }

    /// <summary>
    /// v0 -> v1：初始版本。
    /// 1) 根节点补充 schemaVersion
    /// 2) 为根容器补齐默认 options.labelcontext / contexts 结构
    /// </summary>
    private static void MigrateV0ToV1(JsonObject obj)
    {
        // 确保 options 结构存在
        if (obj["options"] is not JsonObject options)
        {
            options = new JsonObject();
            obj["options"] = options;
        }
        // 通用上下文结构（label/data/event/style）
        if (options["contexts"] is not JsonObject)
        {
            options["contexts"] = new JsonObject
            {
                ["label"] = new JsonObject { ["width"] = "", ["align"] = "left" },
                ["data"] = new JsonObject(),
                ["event"] = new JsonObject(),
                ["style"] = new JsonObject()
            };
        }
        // childrenctrls 递归迁移
        if (obj["childrenctrls"] is JsonArray children)
        {
            foreach (var child in children)
            {
                if (child is JsonObject childObj)
                    EnsureNodeBasics(childObj);
            }
        }
    }

    /// <summary>为子节点补齐基础结构</summary>
    private static void EnsureNodeBasics(JsonObject node)
    {
        if (node["options"] is not JsonObject options)
        {
            options = new JsonObject();
            node["options"] = options;
        }
        if (options["contexts"] is not JsonObject)
        {
            options["contexts"] = new JsonObject();
        }
        if (node["childrenctrls"] is JsonArray children)
        {
            foreach (var child in children)
            {
                if (child is JsonObject childObj)
                    EnsureNodeBasics(childObj);
            }
        }
    }
}
