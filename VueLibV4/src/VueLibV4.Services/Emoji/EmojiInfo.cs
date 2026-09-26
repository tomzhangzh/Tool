using Newtonsoft.Json;

namespace VueLibV4.Services.Emoji;

/// <summary>
/// Emoji 元数据（数据源：unicode-emoji-json 英文名/分类 + Unicode CLDR zh 中文名，离线嵌入）。
/// 序列化为 camelCase（emoji/nameEn/nameZh/group/groupZh），与前端公共接口 {value,label} 风格一致。
/// </summary>
public class EmojiInfo
{
    /// <summary>Emoji 字符（落库值，如 😀）</summary>
    [JsonProperty("emoji")]
    public string Emoji { get; set; }

    /// <summary>英文名（CLDR short name，如 grinning face）</summary>
    [JsonProperty("nameEn")]
    public string NameEn { get; set; }

    /// <summary>中文名（CLDR zh tts；新 emoji 暂未收录时为空，前端回退英文名）</summary>
    [JsonProperty("nameZh")]
    public string NameZh { get; set; }

    /// <summary>分类英文（Smileys &amp; Emotion / People &amp; Body / …）</summary>
    [JsonProperty("group")]
    public string Group { get; set; }

    /// <summary>分类中文（笑脸与情感 / 人物与身体 / …）</summary>
    [JsonProperty("groupZh")]
    public string GroupZh { get; set; }
}
