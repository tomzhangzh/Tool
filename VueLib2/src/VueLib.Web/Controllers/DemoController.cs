using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using VueLib.Web.Models;

namespace VueLib.Web.Controllers;

/// <summary>演示业务接口：商品 CRUD、字典、客户(Ajax下拉)、窗口内容、Grid3 分部视图</summary>
[Route("api/demo")]
public class DemoController : Controller
{
    private readonly ISqlSugarClient _sugar;

    public DemoController(ISqlSugarClient sugar) => _sugar = sugar;

    private ISqlSugarClient Business => _sugar.AsTenant().GetConnectionScope("business");

    // ---------- 字典（select dict 模式） ----------
    [HttpGet("dict")]
    public async Task<IActionResult> Dict(string type)
    {
        var items = await Business.Queryable<DictItem>()
            .Where(d => d.TypeCode == type)
            .OrderBy(d => d.Sort)
            .ToListAsync();
        return Json(new
        {
            success = true,
            data = items.Select(x => new { label = x.Name, value = x.Value }).ToList()
        });
    }

    // ---------- 客户（select ajax 模式） ----------
    [HttpGet("customers")]
    public async Task<IActionResult> Customers(string? keyword)
    {
        var q = Business.Queryable<Customer>();
        if (!string.IsNullOrWhiteSpace(keyword))
            q = q.Where(c => c.Name.Contains(keyword) || c.Company.Contains(keyword));
        var list = await q.OrderBy(c => c.Name).Take(20).ToListAsync();
        return Json(new
        {
            success = true,
            data = list.Select(x => new { label = $"{x.Name}({x.Company})", value = x.Id.ToString() }).ToList()
        });
    }

    // ---------- 商品分页（Grid3 列表数据） ----------
    [HttpGet("products")]
    public async Task<IActionResult> Products(string? keyword, string? category, int page = 1, int pageSize = 5)
    {
        var q = Business.Queryable<Product>();
        if (!string.IsNullOrWhiteSpace(keyword)) q = q.Where(p => p.Name.Contains(keyword));
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(p => p.Category == category);

        var total = await q.CountAsync();
        var rows = await q.OrderBy(p => p.Id, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var result = rows.Select(p => new
        {
            p.Id, p.Name, p.Category, p.Price, p.Stock,
            Status = p.Status == 1 ? "上架" : "下架",
            StatusVal = p.Status,
            p.Remark
        });
        return Json(new { success = true, data = new { total, page, pageSize, rows = result } });
    }

    // ---------- 商品保存（upsert） ----------
    [HttpPost("products/save")]
    public async Task<IActionResult> SaveProduct([FromBody] ProductReq req)
    {
        Product p;
        if (req.Id > 0)
        {
            p = await Business.Queryable<Product>().InSingleAsync(req.Id);
            if (p == null) return Json(new { success = false, message = "记录不存在" });
        }
        else
        {
            p = new Product { CreatedAt = DateTime.Now };
        }
        p.Name = req.Name ?? "";
        p.Category = req.Category ?? "";
        p.Price = req.Price;
        p.Stock = req.Stock;
        p.Status = req.Status;
        p.Remark = req.Remark;
        if (req.Id > 0)
        {
            await Business.Updateable(p).ExecuteCommandAsync();
        }
        else
        {
            await Business.Insertable(p).ExecuteCommandAsync();
        }
        return Json(new { success = true, data = new { id = p.Id } });
    }

    [HttpDelete("products/{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var p = await Business.Queryable<Product>().InSingleAsync(id);
        if (p != null)
        {
            await Business.Deleteable(p).ExecuteCommandAsync();
        }
        return Json(new { success = true });
    }

    // ---------- 窗口内容（openwindow url 内容） ----------
    [HttpGet("window-content")]
    public IActionResult WindowContent(string? from)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("<div class=\"p-4 space-y-3\">");
        sb.Append("<div class=\"text-lg font-bold text-gray-800\">窗口内容（由 updateEl 加载）</div>");
        sb.Append("<div class=\"text-sm text-gray-600\">来源参数 from = <b>").Append(from ?? "-").Append("</b>。此内容由后端返回 HTML 片段，窗口组件通过 updateEl 注入。</div>");
        sb.Append("<div class=\"p-3 bg-blue-50 border border-blue-200 rounded text-blue-700 text-sm\">");
        sb.Append("支持在内容中使用动作属性：<button type=\"button\" dyn-click-chain='{\"steps\":[{\"action\":\"notify\",\"options\":{\"type\":\"success\",\"message\":\"窗口内按钮动作生效！\"}}]}' class=\"px-2 py-1 bg-blue-600 text-white rounded text-xs\">点我(dyn-click-chain)</button>");
        sb.Append("</div></div>");
        return Content(sb.ToString(), "text/html; charset=utf-8");
    }

    // ---------- Grid3 分部视图：Filter 面板 ----------
    [HttpGet("partial/filter")]
    public IActionResult FilterPartial()
        => Content(
"""
<div class="p-2 flex flex-wrap items-end gap-2 bg-gray-50 rounded border border-gray-200">
  <label class="text-xs text-gray-600">关键词
    <input id="grid3-filter-keyword" class="ml-1 px-2 py-1 border border-gray-300 rounded text-sm" placeholder="商品名称" />
  </label>
  <label class="text-xs text-gray-600">分类
    <select id="grid3-filter-category" class="ml-1 px-2 py-1 border border-gray-300 rounded text-sm">
      <option value="">全部</option>
      <option value="电子产品">电子产品</option>
      <option value="办公用品">办公用品</option>
      <option value="生活家电">生活家电</option>
    </select>
  </label>
  <button type="button" class="px-3 py-1 bg-blue-600 text-white rounded text-sm"
          dyn-click-chain='{"steps":[{"action":"collect","options":{"mapping":{"filter.keyword":{"selector":"#grid3-filter-keyword"},"filter.category":{"selector":"#grid3-filter-category"}}}},{"action":"postback","options":{"url":"/api/demo/partial/list","target":"#grid3-list","params":{"keyword":"{{filter.keyword}}","category":"{{filter.category}}"}}},{"action":"notify","options":{"type":"success","message":"查询完成"}}]}'>查询</button>
</div>
""", "text/html; charset=utf-8");

    // ---------- Grid3 分部视图：List 区域 ----------
    [HttpGet("partial/list")]
    public async Task<IActionResult> ListPartial(string? keyword, string? category, int page = 1)
    {
        var q = Business.Queryable<Product>();
        if (!string.IsNullOrWhiteSpace(keyword)) q = q.Where(p => p.Name.Contains(keyword));
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(p => p.Category == category);
        var total = await q.CountAsync();
        var rows = await q.OrderBy(p => p.Id, OrderByType.Desc).Skip((page - 1) * 5).Take(5).ToListAsync();

        var html = new System.Text.StringBuilder();
        html.Append("<div class='p-2'>");
        html.Append("<table class='w-full text-sm border-collapse'><thead>");
        html.Append("<tr class='bg-gray-100 text-left text-gray-600'><th class='p-2 border border-gray-200'>名称</th><th class='p-2 border border-gray-200'>分类</th><th class='p-2 border border-gray-200'>价格</th><th class='p-2 border border-gray-200'>库存</th><th class='p-2 border border-gray-200'>状态</th><th class='p-2 border border-gray-200'>操作</th></tr></thead><tbody>");
        foreach (var p in rows)
        {
            html.Append($"<tr class='hover:bg-gray-50'><td class='p-2 border border-gray-200'>{p.Name}</td><td class='p-2 border border-gray-200'>{p.Category}</td><td class='p-2 border border-gray-200'>{p.Price:F2}</td><td class='p-2 border border-gray-200'>{p.Stock}</td><td class='p-2 border border-gray-200'>{(p.Status == 1 ? "<span class='text-green-600'>上架</span>" : "<span class='text-gray-400'>下架</span>")}</td>");
            html.Append($"<td class='p-2 border border-gray-200'><button type='button' dyn-click-chain='{{\"steps\":[{{\"action\":\"postback\",\"options\":{{\"url\":\"/api/demo/partial/detail/{p.Id}\",\"target\":\"#grid3-detail\"}}}}]}}' class='text-blue-600 hover:underline'>详情</button> ");
            html.Append($"<button type='button' dyn-click-chain='{{\"steps\":[{{\"action\":\"postback\",\"options\":{{\"url\":\"/api/demo/products/{p.Id}\",\"method\":\"DELETE\"}}}},{{\"action\":\"postback\",\"options\":{{\"url\":\"/api/demo/partial/list\",\"target\":\"#grid3-list\",\"params\":{{\"keyword\":\"{{filter.keyword}}\",\"category\":\"{{filter.category}}\"}}}}}},{{\"action\":\"notify\",\"options\":{{\"type\":\"warning\",\"message\":\"已删除\"}}}}]}}' class='text-red-600 hover:underline'>删除</button></td></tr>");
        }
        html.Append("</tbody></table>");
        html.Append($"<div class='mt-2 text-xs text-gray-500'>共 {total} 条记录（服务端分部视图渲染）</div>");
        html.Append("</div>");
        return Content(html.ToString(), "text/html; charset=utf-8");
    }

    // ---------- Grid3 分部视图：Detail ----------
    [HttpGet("partial/detail/{id:int}")]
    public async Task<IActionResult> DetailPartial(int id)
    {
        var p = await Business.Queryable<Product>().InSingleAsync(id);
        if (p == null) return Content("<div class='p-3 text-gray-500 text-sm'>未找到记录</div>", "text/html; charset=utf-8");
        var sb = new System.Text.StringBuilder();
        sb.Append("<div class='p-3 space-y-2'>");
        sb.Append("<div class='font-bold text-gray-800'>商品详情 #").Append(p.Id).Append("</div>");
        sb.Append("<div class='text-sm text-gray-600 grid grid-cols-2 gap-2'>");
        sb.Append("<div>名称：").Append(p.Name).Append("</div><div>分类：").Append(p.Category).Append("</div>");
        sb.Append("<div>价格：").Append(p.Price.ToString("F2")).Append("</div><div>库存：").Append(p.Stock).Append("</div>");
        sb.Append("<div>状态：").Append(p.Status == 1 ? "上架" : "下架").Append("</div><div>备注：").Append(p.Remark).Append("</div>");
        sb.Append("</div></div>");
        return Content(sb.ToString(), "text/html; charset=utf-8");
    }
}

public class ProductReq
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int Status { get; set; } = 1;
    public string? Remark { get; set; }
}
