using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VueLibV4.Platform.Models;

/// <summary>
/// DynWebPage.ParamsJson 的强类型模型（V2 模板用）。
/// 结构：{ provide:{...}, blocks:{ filter:{settingId,model}, list:{...}, detail:{...} } }
/// 同时兼容旧结构：TableName/KeyField/Project/ModalPageId 平铺在顶层。
/// </summary>
public class DynTemplateParams
{
    /// <summary>数据表名</summary>
    [JsonProperty("TableName")]
    public string TableName { get; set; } = "";

    /// <summary>主键字段（默认 Id）</summary>
    [JsonProperty("KeyField")]
    public string KeyField { get; set; } = "Id";

    /// <summary>数据域坐标（__platform__ 或项目 code/Id）</summary>
    [JsonProperty("Project")]
    public string Project { get; set; } = "";

    /// <summary>Detail 弹窗指向的 WebPage Id</summary>
    [JsonProperty("ModalPageId")]
    public string ModalPageId { get; set; } = "";

    /// <summary>是否抽屉模式（OpenWindow 模板用）</summary>
    [JsonProperty("DrawerMode")]
    public bool DrawerMode { get; set; }

    /// <summary>抽屉宽度</summary>
    [JsonProperty("DrawerWidth")]
    public string DrawerWidth { get; set; } = "55%";

    /// <summary>子表外键字段（树形/主从用）</summary>
    [JsonProperty("ChildFkField")]
    public string ChildFkField { get; set; } = "";

    /// <summary>block 槽位配置</summary>
    [JsonProperty("blocks")]
    public TemplateBlocks Blocks { get; set; } = new();

    /// <summary>兼容旧结构：filter/list/detail 平铺在顶层</summary>
    [JsonExtensionData]
    public Dictionary<string, JToken>? Extra { get; set; }

    /// <summary>从 JSON 字符串反序列化，失败返回空实例。</summary>
    public static DynTemplateParams Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new DynTemplateParams();
        try { return JsonConvert.DeserializeObject<DynTemplateParams>(json) ?? new DynTemplateParams(); }
        catch { return new DynTemplateParams(); }
    }

    /// <summary>取 filter 槽位的 settingId（兼容 blocks.filter.settingId 和旧 filter.settingId）。</summary>
    public int FilterSettingId => Blocks?.Filter?.SettingId ?? 0;
    public int ListSettingId => Blocks?.List?.SettingId ?? 0;
    public int DetailSettingId => Blocks?.Detail?.SettingId ?? 0;

    public string ListLoadUrl => Blocks?.List?.LoadUrl ?? "";
    public string ListDeleteUrl => Blocks?.List?.DeleteUrl ?? "";
}

public class TemplateBlocks
{
    [JsonProperty("filter")] public BlockSlot Filter { get; set; } = new();
    [JsonProperty("list")] public BlockSlot List { get; set; } = new();
    [JsonProperty("detail")] public BlockSlot Detail { get; set; } = new();
}

public class BlockSlot
{
    [JsonProperty("settingId")] public int SettingId { get; set; }
    [JsonProperty("model")] public JObject? Model { get; set; }
    [JsonProperty("loadUrl")] public string? LoadUrl { get; set; }
    [JsonProperty("deleteUrl")] public string? DeleteUrl { get; set; }
}
