using Newtonsoft.Json;
using VueLibV4.Web.Infrastructure;

namespace VueLibV4.Web.Core;

/// <summary>
/// 统一 API 返回结构。
/// 序列化字段名固定为小写（code/msg/data），前端统一使用 res.code / res.data / res.msg；
/// Data 内数据行由 JObject 动态序列化，保持数据库列名原样（大写 PascalCase），
/// 因此前端对数据行仍使用大写字：row.Name、c.Icon、sc.IsActive 等。
/// dyn-actions：后端随 JSON 下发的前端动作链（如保存成功后刷新区块、关 layer、toast），
/// 前端 postback/updateEl/reload 收到后自动按序执行，Controller 无需写任何 JS。
/// </summary>
public class ApiResult
{
    [JsonProperty("code")]
    public int Code { get; set; } = 0;

    [JsonProperty("msg")]
    public string Msg { get; set; } = "ok";

    [JsonProperty("data")]
    public object Data { get; set; }

    /// <summary>后端下发的前端动作数组（dyn-actions），形如 [{"action":"toast","options":{...}}]</summary>
    [JsonProperty("dyn-actions")]
    public List<object> DynActions { get; set; }

    public static ApiResult Ok(object data = null, string msg = "ok") => new() { Code = 0, Msg = msg, Data = data };
    public static ApiResult Fail(string msg, int code = 500) => new() { Code = code, Msg = msg };

    /// <summary>挂载一个或多个服务端动作（保存后刷新列表、关闭弹窗、提示等）</summary>
    public ApiResult WithActions(params DynJavaScript[] actions)
    {
        if (actions == null || actions.Length == 0) return this;
        DynActions ??= new List<object>();
        foreach (var a in actions)
        {
            if (a == null) continue;
            DynActions.Add(new { action = a.Action, @event = a.Event, options = a.Options ?? new { } });
        }
        return this;
    }
}
