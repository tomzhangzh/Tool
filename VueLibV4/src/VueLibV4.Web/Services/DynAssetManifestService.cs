using System.Security.Cryptography;
using System.Text;
using VueLibV4.Services.Dependency;

namespace VueLibV4.Web.Services;

/// <summary>
/// wwwroot/dyn 静态模块的内容哈希清单。
/// 背景：dyn-lib.js 按固定 ?v=version 顺序加载各 dyn 模块，改了 js 必须手工 bump 版本，
/// 否则浏览器磁盘缓存永不失效（高频踩坑）。本清单按文件内容生成短哈希，由 _Layout 内联为
/// window.DYN_ASSET_HASHES，dyn-lib.script() 优先使用内容哈希——文件改动即自动失效，零手工。
/// 单例 + 按文件 LastWrite/长度 快照缓存：仅当文件变更时才重新读内容算哈希。
/// 自动注册：实现 ISingletonDependency，AddVueLibPlatform() 扫描程序集即可发现。
/// </summary>
public class DynAssetManifestService : ISingletonDependency
{
    private readonly IWebHostEnvironment _env;
    private readonly object _gate = new();
    private Dictionary<string, string> _manifest = new(StringComparer.Ordinal);
    private Dictionary<string, (DateTime LastWrite, long Length)> _snapshot = new(StringComparer.Ordinal);

    public DynAssetManifestService(IWebHostEnvironment env) => _env = env;

    /// <summary>dyn 目录物理根（wwwroot/dyn）。</summary>
    private string DynRoot => Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "dyn");

    /// <summary>
    /// 获取最新清单：key=相对 dyn 目录的 POSIX 路径（如 dyn-core.js / blocks/dyn-blocks-common.js），
    /// value=8 位内容短哈希。
    /// </summary>
    public IReadOnlyDictionary<string, string> GetManifest()
    {
        var root = DynRoot;
        if (!Directory.Exists(root)) return _manifest;

        var files = Directory.EnumerateFiles(root, "*.js", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        lock (_gate)
            {
                var current = new Dictionary<string, (DateTime LastWrite, long Length)>(StringComparer.Ordinal);
                foreach (var f in files)
                {
                    var fi = new FileInfo(f);
                    current[RelKey(f, root)] = (fi.LastWriteTimeUtc, fi.Length);
                }
                // 与上次快照逐项比对（新增/删除/修改均触发重建）
                var stale = current.Count != _snapshot.Count;
                if (!stale)
                {
                    foreach (var kv in current)
                    {
                        if (!_snapshot.TryGetValue(kv.Key, out var oldMeta)
                            || oldMeta.LastWrite != kv.Value.LastWrite || oldMeta.Length != kv.Value.Length)
                        { stale = true; break; }
                    }
                }
                if (!stale) return _manifest;

            var next = new Dictionary<string, string>(StringComparer.Ordinal);
            var nextSnap = new Dictionary<string, (DateTime, long)>(StringComparer.Ordinal);
            foreach (var f in files)
            {
                var key = RelKey(f, root);
                try
                {
                    using var sha = SHA1.Create();
                    using var fs = File.OpenRead(f);
                    var hash = sha.ComputeHash(fs);
                    next[key] = Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Substring(0, 8);
                }
                catch
                {
                    // 读不到（占用/瞬时缺失）：沿用旧哈希，避免整页崩溃
                    if (_manifest.TryGetValue(key, out var old)) next[key] = old;
                }
                var fi = new FileInfo(f);
                nextSnap[key] = (fi.LastWriteTimeUtc, fi.Length);
            }
            _manifest = next;
            _snapshot = nextSnap;
            return _manifest;
        }
    }

    /// <summary>输出为内联 JS：window.DYN_ASSET_HASHES = {...}（含 HTML/JS 转义）。</summary>
    public string RenderInlineScript()
    {
        var manifest = GetManifest();
        var sb = new StringBuilder();
        sb.Append("window.DYN_ASSET_HASHES=");
        sb.Append('{');
        var first = true;
        foreach (var kv in manifest)
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append('"').Append(JsEscape(kv.Key)).Append("\":\"").Append(JsEscape(kv.Value)).Append('"');
        }
        sb.Append("};");
        return sb.ToString();
    }

    private static string RelKey(string full, string root)
        => Path.GetRelativePath(root, full).Replace('\\', '/');

    private static string JsEscape(string s)
        => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "");
}
