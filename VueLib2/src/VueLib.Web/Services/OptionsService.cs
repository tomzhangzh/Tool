using SqlSugar;
using VueLib.Web.Models;

namespace VueLib.Web.Services;

/// <summary>下拉选项解析：static / dict(字典) / sql(业务库查询) / ajax(前端自行请求)</summary>
public class OptionsService
{
    private readonly ISqlSugarClient _sugar;

    public OptionsService(ISqlSugarClient sugar)
    {
        _sugar = sugar;
    }

    private ISqlSugarClient Business => _sugar.AsTenant().GetConnectionScope("business");

    public record OptionItem(string Label, string Value);

    public async Task<List<OptionItem>> ResolveAsync(OptionResolveRequest req)
    {
        switch (req.Kind?.ToLowerInvariant())
        {
            case "static":
                return (req.Items ?? new List<StaticOption>())
                    .Select(x => new OptionItem(x.Label ?? "", x.Value ?? ""))
                    .Where(x => !string.IsNullOrEmpty(x.Label))
                    .ToList();

            case "dict":
            {
                if (string.IsNullOrEmpty(req.TypeCode)) return new List<OptionItem>();
                var items = await Business.Queryable<DictItem>()
                    .Where(d => d.TypeCode == req.TypeCode)
                    .OrderBy(d => d.Sort)
                    .ToListAsync();
                return items.Select(d => new OptionItem(d.Name, d.Value)).ToList();
            }

            case "sql":
                return await ExecuteSqlAsync(req.Sql, req.ValueField, req.LabelField);

            case "ajax":
                // ajax 模式由前端直接发起请求（url + params），此处仅占位
                return new List<OptionItem>();

            default:
                return new List<OptionItem>();
        }
    }

    /// <summary>执行业务库 SELECT 查询，映射为选项；只允许单条 SELECT，最多 500 行</summary>
    private async Task<List<OptionItem>> ExecuteSqlAsync(string? sql, string? valueField, string? labelField)
    {
        var result = new List<OptionItem>();
        if (string.IsNullOrWhiteSpace(sql)) return result;
        var trimmed = sql.Trim();
        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("options/sql 仅允许 SELECT 查询");
        if (trimmed.Contains(';'))
            throw new InvalidOperationException("options/sql 不允许分号（单条语句）");
        if (trimmed.Contains("INTO ", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("UPDATE", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("DELETE", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("options/sql 仅允许只读查询");

        var vf = string.IsNullOrWhiteSpace(valueField) ? "Value" : valueField!;
        var lf = string.IsNullOrWhiteSpace(labelField) ? "Label" : labelField!;

        var dt = await Business.Ado.GetDataTableAsync(trimmed);
        foreach (System.Data.DataRow row in dt.Rows)
        {
            string label, value;
            try { label = Convert.ToString(row[lf]) ?? ""; }
            catch { label = Convert.ToString(row[0]) ?? ""; }
            try { value = Convert.ToString(row[vf]) ?? ""; }
            catch { value = Convert.ToString(row[1]) ?? ""; }
            result.Add(new OptionItem(label, value));
            if (result.Count >= 500) break;
        }
        return result;
    }
}

public class StaticOption
{
    public string? Label { get; set; }
    public string? Value { get; set; }
}

public class OptionResolveRequest
{
    /// <summary>static / dict / sql / ajax</summary>
    public string? Kind { get; set; }
    public List<StaticOption>? Items { get; set; }
    public string? TypeCode { get; set; }
    public string? Sql { get; set; }
    public string? ValueField { get; set; }
    public string? LabelField { get; set; }
    public string? Url { get; set; }
    public Dictionary<string, object>? Params { get; set; }
}
