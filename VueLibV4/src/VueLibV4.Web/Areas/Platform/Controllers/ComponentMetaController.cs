using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;
using VueLibV4.Web.Core;
using VueLibV4.Web.Models;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 组件元数据：设计器组件库的数据来源（强类型 Model）。
/// 驱动：组件面板分组、拖拽准入规则（allowDrop/acceptAll）、属性面板自动渲染（PropsMeta）。
/// </summary>
[Area("Platform")]
[Route("api/platform/componentmeta")]
[ApiController]
public class ComponentMetaController : ControllerBase
{
    private readonly DbFactory _dbs;
    public ComponentMetaController(DbFactory dbs) { _dbs = dbs; }

    [HttpGet("all")]
    public ApiResult All(string category = null)
    {
        using var db = _dbs.PlatformDb();
        var q = db.Queryable<ComponentMeta>()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Category)
            .OrderBy(c => c.Id);
        if (!string.IsNullOrEmpty(category))
            q = q.Where(c => c.Category == category);
        return ApiResult.Ok(q.ToList());
    }

    /// <summary>按分类分组，供设计器左侧组件库渲染</summary>
    [HttpGet("grouped")]
    public ApiResult Grouped()
    {
        using var db = _dbs.PlatformDb();
        var rows = db.Queryable<ComponentMeta>()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Category)
            .OrderBy(c => c.Id)
            .ToList();
        var groups = rows.GroupBy(r => r.Category ?? "未分类")
            .Select(g => new { category = g.Key, components = g.ToList() })
            .ToList();
        // 补充 ElementPlus 内置组件（DB 无元数据行）：ElFormItem 供设计器拖入，defaultCfg 自动填 label
        if (!rows.Any(r => r.ComponentName == "ElFormItem"))
        {
            var formGroup = groups.FirstOrDefault(g => g.category == "表单");
            var fake = new ComponentMeta
            {
                ComponentName = "ElFormItem",
                Label = "表单项",
                Category = "表单",
                Icon = "🏷️",
                IsActive = true,
                AcceptAll = true, // 表单项可接收任意表单控件/组件作为子项
                AllowDrop = "[]",
                SlotsDefine = "[\"default\"]",
                PropsMeta = "[]"
            };
            if (formGroup != null)
            {
                var list = formGroup.components.ToList();
                list.Add(fake);
                groups.Remove(formGroup);
                groups.Add(new { category = "表单", components = list });
            }
            else
            {
                groups.Add(new { category = "表单", components = new List<ComponentMeta> { fake } });
            }
        }
        return ApiResult.Ok(groups.OrderBy(g => g.category));
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
    {
        using var db = _dbs.PlatformDb();
        var row = FirstByIdOrCode(db, id);
        return ApiResult.Ok(row);
    }

    /// <summary>按组件名取元数据（运行时/校验用）</summary>
    [HttpGet("byName")]
    public ApiResult ByName(string name)
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(db.Queryable<ComponentMeta>().First(c => c.ComponentName == name));
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] ComponentMeta data)
    {
        using var db = _dbs.PlatformDb();
        if (data.Id <= 0)
        {
            db.Insertable(data).ExecuteCommand();
            return ApiResult.Ok(new { data.Id }, "新增成功");
        }
        db.Updateable(data).ExecuteCommand();
        return ApiResult.Ok(new { data.Id }, "保存成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        using var db = _dbs.PlatformDb();
        var id = keys["Id"]?.Value<int>() ?? 0;
        if (id <= 0) return ApiResult.Fail("缺少 Id");
        db.Deleteable<ComponentMeta>(id).ExecuteCommand();
        return ApiResult.Ok(true, "删除成功");
    }

    /// <summary>取单个组件元配置（PropertyConfigJson / DefaultConfigJson 原始 JSON），供元配置编辑器加载</summary>
    [HttpGet("getmeta")]
    public ApiResult GetMeta(string id)
    {
        using var db = _dbs.PlatformDb();
        var row = FirstByIdOrCode(db, id);
        if (row == null) return ApiResult.Fail("未找到组件元数据");
        return ApiResult.Ok(new
        {
            id = row.Id,
            componentName = row.ComponentName,
            propertyConfigJson = row.PropertyConfigJson,
            defaultConfigJson = row.DefaultConfigJson
        });
    }

    /// <summary>保存组件元配置（前端已把 DSL 解析为 JSON；后端再做 JSON 合法性校验，通过才更新数据库）</summary>
    [HttpPost("savemeta")]
    public ApiResult SaveMeta([FromBody] SaveMetaReq req)
    {
        if (req == null || req.ComponentId <= 0) return ApiResult.Fail("缺少 componentId");
        // JSON 合法性校验：非法不写库
        try
        {
            if (!string.IsNullOrWhiteSpace(req.PropertyConfigJson))
                JToken.Parse(req.PropertyConfigJson);
            if (!string.IsNullOrWhiteSpace(req.DefaultConfigJson))
                JToken.Parse(req.DefaultConfigJson);
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("JSON 不合法，未保存：" + ex.Message);
        }
        using var db = _dbs.PlatformDb();
        var affected = db.Updateable<ComponentMeta>()
            .SetColumns(c => new ComponentMeta
            {
                PropertyConfigJson = string.IsNullOrWhiteSpace(req.PropertyConfigJson) ? null : req.PropertyConfigJson,
                DefaultConfigJson = string.IsNullOrWhiteSpace(req.DefaultConfigJson) ? null : req.DefaultConfigJson
            })
            .Where(c => c.Id == req.ComponentId)
            .ExecuteCommand();
        if (affected <= 0) return ApiResult.Fail("未找到该组件元数据，未更新");
        return ApiResult.Ok(new { id = req.ComponentId }, "保存成功");
    }

    private ComponentMeta FirstByIdOrCode(SqlSugarClient db, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id))
            return db.Queryable<ComponentMeta>().First(c => c.Id == id);
        return db.Queryable<ComponentMeta>().First(c => c.ComponentName == key);
    }
}

/// <summary>保存组件元配置请求体（前端 DSL → JSON 后提交）</summary>
public class SaveMetaReq
{
    public int ComponentId { get; set; }
    public string PropertyConfigJson { get; set; } = "";
    public string DefaultConfigJson { get; set; } = "";
}
