using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Web.Core;
using VueLibV4.Services.Data;
using VueLibV4.Web.Infrastructure;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 平台库免模型动态数据控制器（M5 管理页基座）：
/// 与 /api/business/dyndata 共用 DynamicCrudService，但固定操作 PlatformDb
/// （DesktopShortcut / DesktopSolution / DynProject / DynTemplate / DynWebPage / DynActionHelper / 字典 / 组件元配置等平台表）。
/// save/delete 返回 ApiResult + dyn-actions（提示 / 刷新表格 / 关弹窗），管理页表单与行操作零业务 JS。
/// 路由：api/platform/dyndata
/// </summary>
[Area("Platform")]
[Route("api/platform/dyndata")]
[ApiController]
public class DynPlatformDataController : ControllerBase
{
    private readonly DbFactory _dbs;
    private readonly DynamicCrudService _svc;
    public DynPlatformDataController(DbFactory dbs, DynamicCrudService svc) { _dbs = dbs; _svc = svc; }

    /// <summary>分页查询：{table, page, size, filter:{field:{op,value}}}（DynTable / 筛选下拉数据源）</summary>
    [HttpPost("search")]
    public ApiResult Search([FromBody] JObject req)
    {
        var table = req["table"]?.ToString();
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
        var page = req["page"]?.Value<int>() ?? 1;
        var size = req["size"]?.Value<int>() ?? 20;
        var filter = req["filter"] as JObject;
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(_svc.Page(db, table, page, size, filter));
    }

    /// <summary>按主键取单行（表单片段服务端预载行数据用）</summary>
    [HttpGet("get")]
    public ApiResult Get(string table, string id)
    {
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
        using var db = _dbs.PlatformDb();
        var pks = _svc.PrimaryKeys(db, table);
        if (pks.Count == 0) return ApiResult.Fail("表没有主键");
        return ApiResult.Ok(_svc.First(db, table, $"[{pks[0]}]=@v", new { v = id }));
    }

    /// <summary>
    /// 表列元数据（DynTable 启动时取主键名用）：返回 ColumnInfo 数组（元素含 IsPrimaryKey）。
    /// 与前端 DynTable 的 res.data 数组契约对齐。
    /// </summary>
    [HttpGet("meta")]
    public ApiResult Meta(string table)
    {
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(_svc.Columns(db, table));
    }

    /// <summary>
    /// 保存（updateel 提交约定：body 为扁平实体字段）：有主键→更新，无主键→新增。
    /// 返回 dyn-actions：提示 + 刷新 gridId 表格 + 关弹窗。
    /// </summary>
    [HttpPost("save")]
    public ApiResult Save(string table, string gridId, [FromBody] JObject data)
    {
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
        using var db = _dbs.PlatformDb();
        try
        {
            var pks = _svc.PrimaryKeys(db, table);
            var hasPk = pks.Count > 0
                && pks.All(p => data?[p] != null && data[p].Type != JTokenType.Null && !string.IsNullOrEmpty(data[p].ToString()));
            var result = hasPk ? _svc.Update(db, table, data) : _svc.Insert(db, table, data);
            var msg = hasPk ? "更新成功" : "新增成功";
            return ApiResult.Ok(new { result }, msg).WithActions(BuildActions(msg, gridId, close: true));
        }
        catch (Exception ex)
        {
            // 反射调用（DynamicCrudService）抛 TargetInvocationException 时取最内层真实信息
            return ApiResult.Fail("保存失败：" + ex.GetBaseException().Message);
        }
    }

    /// <summary>删除（id=主键值，URL 传参配合 DynTable 行操作 {{Id}} 模板）；返回提示 + 表格刷新动作</summary>
    [HttpPost("delete")]
    public ApiResult Delete(string table, string id, string gridId)
    {
        if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(id)) return ApiResult.Fail("缺少 table 或 id");
        using var db = _dbs.PlatformDb();
        try
        {
            var pks = _svc.PrimaryKeys(db, table);
            if (pks.Count == 0) return ApiResult.Fail("表没有主键");
            var result = _svc.Delete(db, table, new JObject { [pks[0]] = id });
            return ApiResult.Ok(new { result }, "删除成功").WithActions(BuildActions("删除成功", gridId, close: false));
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("删除失败：" + ex.GetBaseException().Message);
        }
    }

    private static DynJavaScript[] BuildActions(string msg, string gridId, bool close)
    {
        var list = new List<DynJavaScript> { new FlashMessageJavaScript { Message = msg, Type = "success" } };
        if (!string.IsNullOrWhiteSpace(gridId)) list.Add(new GridReloadJavaScript { GridId = gridId });
        if (close) list.Add(new CloseDialogJavaScript());
        return list.ToArray();
    }
}
