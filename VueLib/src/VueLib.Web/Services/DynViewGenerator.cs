using VueLib.Web.Models;

namespace VueLib.Web.Services;

/// <summary>
/// 页面视图生成器：为某个已生成的页面产出"独立视图源码"（自包含 HTML，可直接用浏览器打开 / 集成到其它工程）
/// 生成物与 /DynRun 预览运行时共用同一套 dyn-lib 动态引擎。
/// </summary>
public static class DynViewGenerator
{
    public static string BuildStandaloneHtml(DynProject project, DynPage page, DynPageDefinition def, string host)
    {
        var isSummary = page.PageType == "Summary";
        var summaryUrl = $"{host}/DynRun/Summary?projectId={project.Id}&pageId={page.Id}";
        var detailUrl = $"{host}/DynRun/Detail?projectId={project.Id}&pageId={page.Id}&id=0";

        var containerAttrs = isSummary
            ? $"dyn-init data-dyn-url=\"{summaryUrl}\" data-dyn-load=\"true\""
            : $"dyn-init='{{\"model\":{{}} }}' data-dyn-url=\"{detailUrl}\" data-dyn-load=\"true\"";

        return $$"""
<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>{{project.DisplayName ?? project.Name}} - {{page.Title ?? page.Name}}</title>
<link rel="stylesheet" href="{{host}}/css/element-plus.css">
<style>
  body { margin:0; font-family:'Segoe UI','Microsoft YaHei',sans-serif; background:#f0f2f5; }
  .view-topbar { background:#fff; border-bottom:1px solid #e4e7ed; padding:10px 16px; display:flex; align-items:center; gap:12px; }
  .view-topbar .proj { font-weight:600; font-size:15px; }
  .view-topbar .page { color:#409eff; font-size:13px; background:#ecf5ff; padding:2px 10px; border-radius:10px; }
  .dyn-page { padding:16px; }
</style>
</head>
<body>
<div class="view-topbar">
  <span class="proj">{{project.DisplayName ?? project.Name}}</span>
  <span class="page">{{page.Title ?? page.Name}}（{{(isSummary ? "汇总屏" : "细节屏")}}）</span>
  <span style="color:#909399;font-size:12px;">Table: {{page.TableName}}</span>
</div>
<div id="dynHost" {{containerAttrs}} class="dyn-page dyn-loading"></div>

<script src="{{host}}/lib/jquery/dist/jquery.min.js"></script>
<script src="{{host}}/js/vue.global.prod.js"></script>
<script src="{{host}}/js/element-plus.full.min.js"></script>
<script src="{{host}}/js/element-plus-icons.min.js"></script>
<script src="{{host}}/js/lodash.min.js"></script>
<script src="{{host}}/js/dyn-lib.js"></script>
<script>
$(function () {
    if (window.dynLib) { dynLib.initAll(); }
});
</script>
</body>
</html>
""";
    }
}
