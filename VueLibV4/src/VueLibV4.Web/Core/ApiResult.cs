using Newtonsoft.Json;

namespace VueLibV4.Web.Core;

/// <summary>
/// 统一 API 返回结构。
/// 序列化字段名固定为小写（code/msg/data），前端统一使用 res.code / res.data / res.msg；
/// Data 内数据行由 JObject 动态序列化，保持数据库列名原样（大写 PascalCase），
/// 因此前端对数据行仍使用大写字：row.Name、c.Icon、sc.IsActive 等。
/// </summary>
public class ApiResult
{
    [JsonProperty("code")]
    public int Code { get; set; } = 0;

    [JsonProperty("msg")]
    public string Msg { get; set; } = "ok";

    [JsonProperty("data")]
    public object Data { get; set; }

    public static ApiResult Ok(object data = null, string msg = "ok") => new() { Code = 0, Msg = msg, Data = data };
    public static ApiResult Fail(string msg, int code = 500) => new() { Code = code, Msg = msg };
}
