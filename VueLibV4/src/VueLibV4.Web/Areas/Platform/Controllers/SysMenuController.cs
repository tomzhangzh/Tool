using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 系统菜单（树形）：管理页左侧树+右侧表单；桌面快捷方式数据源（IsAddToDesktop=true）。
/// TargetType：FullScreen=全屏 / Iframe=桌面内嵌 iframe / NewWindow=新窗口。
/// PermissionCode 预留权限编码（后续与权限模块对接，树接口可增加按权限过滤）。
/// </summary>
[Area("Platform")]
[Route("api/platform/sysmenu")]
[ApiController]
public class SysMenuController : ControllerBase
{
    private readonly ISysMenuService _menus;

    public SysMenuController(ISysMenuService menus) => _menus = menus;

    /// <summary>完整菜单树（含全部字段），供管理页左侧树渲染</summary>
    [HttpGet("tree")]
    public ApiResult Tree()
    {
        var all = _menus.List(m => true, "SortNo ASC, Id ASC");
        return ApiResult.Ok(new { tree = BuildTree(all, null) });
    }

    /// <summary>单条菜单</summary>
    [HttpGet("get")]
    public ApiResult Get(int id) => ApiResult.Ok(_menus.GetById(id));

    /// <summary>保存（新增/编辑一体；父级循环校验）</summary>
    [HttpPost("save")]
    public ApiResult Save([FromBody] SysMenu data)
    {
        if (data == null) return ApiResult.Fail("参数不能为空");
        if (string.IsNullOrWhiteSpace(data.Name)) return ApiResult.Fail("菜单名称不能为空");
        if (data.Id > 0 && data.ParentId == data.Id) return ApiResult.Fail("父菜单不能是自己");
        if (data.Id > 0 && data.ParentId != null && IsDescendant(data.Id, data.ParentId.Value))
            return ApiResult.Fail("父菜单不能是其子菜单");
        if (string.IsNullOrWhiteSpace(data.TargetType)) data.TargetType = MenuTargetType.Iframe;

        if (data.Id <= 0)
        {
            _menus.Insert(data);
            return ApiResult.Ok(new { data.Id }, "新增成功");
        }
        _menus.Update(data);
        return ApiResult.Ok(new { data.Id }, "保存成功");
    }

    /// <summary>删除（存在子菜单时禁止，避免误删整棵子树）</summary>
    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        var id = keys?["Id"]?.Value<int>() ?? 0;
        if (id <= 0) return ApiResult.Fail("缺少 Id");
        if (_menus.Count(m => m.ParentId == id) > 0) return ApiResult.Fail("存在子菜单，请先删除子菜单");
        _menus.DeleteById(id);
        return ApiResult.Ok(true, "删除成功");
    }

    /// <summary>桌面快捷方式数据源：IsAddToDesktop=true 且 IsActive 的树（含 children，父菜单=文件夹）</summary>
    [HttpGet("desktoplist")]
    public ApiResult DesktopList()
    {
        var all = _menus.List(m => m.IsAddToDesktop && m.IsActive, "SortNo ASC, Id ASC");
        return ApiResult.Ok(BuildTree(all, null));
    }

    // ---------------- 树构建 ----------------

    private static List<JObject> BuildTree(List<SysMenu> all, int? parentId)
    {
        var list = new List<JObject>();
        foreach (var m in all.Where(x => x.ParentId == parentId))
        {
            var node = new JObject
            {
                ["Id"] = m.Id,
                ["ParentId"] = m.ParentId,
                ["Code"] = m.Code,
                ["Name"] = m.Name,
                ["Icon"] = m.Icon,
                ["Url"] = m.Url,
                ["TargetType"] = m.TargetType,
                ["Width"] = m.Width,
                ["Height"] = m.Height,
                ["IsAddToDesktop"] = m.IsAddToDesktop,
                ["IsAddToDesktopRoot"] = m.IsAddToDesktopRoot,
                ["IsAddToStartMenu"] = m.IsAddToStartMenu,
                ["SortNo"] = m.SortNo,
                ["IsActive"] = m.IsActive,
                ["PermissionCode"] = m.PermissionCode,
                ["children"] = JArray.FromObject(BuildTree(all, m.Id))
            };
            list.Add(node);
        }
        return list;
    }

    /// <summary>candidateId 是否位于 rootId 的子树中（用于父级循环校验）</summary>
    private bool IsDescendant(int rootId, int candidateId)
    {
        var all = _menus.List(m => true, null);
        var children = all.Where(x => x.ParentId == rootId).Select(x => x.Id).ToList();
        var stack = new Stack<int>(children);
        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            if (cur == candidateId) return true;
            foreach (var c in all.Where(x => x.ParentId == cur).Select(x => x.Id))
                stack.Push(c);
        }
        return false;
    }
}
