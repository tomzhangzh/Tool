using System.Reflection;
using Newtonsoft.Json.Linq;
using VueLibV4.Services.Dependency;

namespace VueLibV4.Services.Emoji;

/// <summary>
/// Emoji 元数据服务：列出平台可用 Emoji（字符 / 英文名 / 中文名 / 中英文分类），
/// 供图标下拉选择等场景使用。数据离线嵌入程序集（emoji-data.json），无外部依赖、无运行时网络请求。
/// 单例：全量约 1900 条，进程内只加载一次。
/// </summary>
public interface IEmojiService : ISingletonDependency
{
    /// <summary>全量 Emoji（按分类原始顺序）</summary>
    IReadOnlyList<EmojiInfo> List();

    /// <summary>关键字检索（匹配 emoji 字符 / 中文名 / 英文名，英文忽略大小写）；关键字为空返回全量</summary>
    List<EmojiInfo> Search(string keyword);
}

public class EmojiService : IEmojiService
{
    private const string ResourceName = "VueLibV4.Services.Emoji.emoji-data.json";
    private static readonly Lazy<IReadOnlyList<EmojiInfo>> _data = new(Load, true);

    public IReadOnlyList<EmojiInfo> List() => _data.Value;

    public List<EmojiInfo> Search(string keyword)
    {
        var all = _data.Value;
        if (string.IsNullOrWhiteSpace(keyword)) return all.ToList();
        var kw = keyword.Trim();
        return all.Where(x =>
            (x.NameZh != null && x.NameZh.Contains(kw, StringComparison.OrdinalIgnoreCase))
            || (x.NameEn != null && x.NameEn.Contains(kw, StringComparison.OrdinalIgnoreCase))
            || (x.Emoji != null && x.Emoji.Contains(kw, StringComparison.Ordinal))).ToList();
    }

    private static IReadOnlyList<EmojiInfo> Load()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"嵌入资源不存在：{ResourceName}");
        using var reader = new StreamReader(stream);
        var arr = JArray.Parse(reader.ReadToEnd());
        var list = new List<EmojiInfo>(arr.Count);
        foreach (var item in arr)
        {
            list.Add(new EmojiInfo
            {
                Emoji = item["e"]?.ToString(),
                NameEn = item["en"]?.ToString(),
                NameZh = item["zh"]?.ToString(),
                Group = item["g"]?.ToString(),
                GroupZh = item["gz"]?.ToString()
            });
        }
        return list;
    }
}
