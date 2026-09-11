/* ============================================================
 * dyn-actionhelpers.generated.js —— 由 ActionHelperController 自动生成
 * 动作注入文件：dyn-actionhelper.js 启动时动态加载，把 DB 中的
 * 自定义动作注册进动作系统（无需修改主 js，重建即注入）
 * 生成时间: 2026-09-11 00:08:14
 * ============================================================ */
(function (global) {
    'use strict';
    var rows = [
  {
    "id": 19,
    "code": "confirmpost",
    "name": "确认后提交（Demo）",
    "category": "flow",
    "metaJson": "{\"label\":\"确认后提交（Demo）\",\"doc\":\"示例自定义动作：先弹确认框，确认后把 Model 提交到 url，成功后刷新 selector 区域。\",\"events\":[\"click\"],\"defaults\":{\"confirm\":\"确认提交？\",\"method\":\"POST\"},\"params\":[{\"name\":\"url\",\"type\":\"string\",\"label\":\"URL\",\"required\":true},{\"name\":\"confirm\",\"type\":\"string\",\"label\":\"确认文案\"},{\"name\":\"reload\",\"type\":\"string\",\"label\":\"刷新选择器\"}],\"i18n\":{\"en-US\":{\"label\":\"Confirm & Post (Demo)\"}}}",
    "scriptContent": "function (ctx) { var o = ctx.options || {}; var msg = o.confirm || \"确认提交？\"; return dyn.confirmAsync(msg).then(function (ok) { if (!ok) return; return dyn.postback(ctx.element, { url: o.url, reload: o.reload, message: o.message || \"已提交\" }); }); }",
    "isBuiltin": false
  },
  {
    "id": 20,
    "code": "opendetail",
    "name": "打开详情窗口（Demo）",
    "category": "window",
    "metaJson": "{\"label\":\"打开详情窗口（Demo）\",\"doc\":\"示例自定义动作：打开详情窗口，并把当前行 data-id 拼进 URL；onclose 触发事件总线 detailClosed。\",\"events\":[\"click\"],\"defaults\":{\"title\":\"详情\",\"width\":\"60%\"},\"params\":[{\"name\":\"url\",\"type\":\"string\",\"label\":\"URL\"},{\"name\":\"title\",\"type\":\"string\",\"label\":\"标题\"},{\"name\":\"width\",\"type\":\"string\",\"label\":\"宽度\"}],\"i18n\":{\"en-US\":{\"label\":\"Open Detail (Demo)\"}}}",
    "scriptContent": "function (ctx) { var o = ctx.options || {}; var id = ctx.params.id || \"\"; var url = o.url || \"\"; if (id) url += (url.indexOf(\"?\") >= 0 ? \"&\" : \"?\") + \"id=\" + encodeURIComponent(id); var host = dyn.openwindow({ url: url, title: o.title || \"详情\", width: o.width || \"60%\", type: o.type || \"modal\", events: { onclose: [{ action: \"triggerevent\", options: { name: \"detailClosed\", payload: { id: id } } }] } }, ctx.element); return host; }",
    "isBuiltin": false
  },
  {
    "id": 21,
    "code": "statusflow",
    "name": "按状态分支（Demo）",
    "category": "flow",
    "metaJson": "{\"label\":\"按状态分支（Demo）\",\"doc\":\"示例自定义动作：按 Model.Status 值分支，draft→弹窗，released→新标签，其它→toast。\",\"events\":[\"click\"],\"defaults\":{},\"params\":[{\"name\":\"field\",\"type\":\"string\",\"label\":\"字段名\",\"default\":\"Status\"}],\"i18n\":{\"en-US\":{\"label\":\"Status Flow (Demo)\"}}}",
    "scriptContent": "function (ctx) { var o = ctx.options || {}; var f = o.field || \"Status\"; var v = (ctx.model || {})[f]; if (v === \"草稿\" || v === \"draft\") return dyn.actionHelper.openwindow({ url: o.draftUrl, title: \"草稿编辑\", type: \"modal\" }, ctx.element); if (v === \"已发布\" || v === \"released\") { window.open(o.releaseUrl || \"/\", \"_blank\"); return; } return dyn.showMessage(\"当前状态：\" + v + \"，无后续动作\", \"info\"); }",
    "isBuiltin": false
  },
  {
    "id": 22,
    "code": "saveflush",
    "name": "保存后刷新并提示（Demo）",
    "category": "data",
    "metaJson": "{\"label\":\"保存后刷新并提示（Demo）\",\"doc\":\"示例自定义动作：POST Model 到 url，成功后 toast + 刷新 selector，并触发 window 事件 saved。\",\"events\":[\"click\"],\"defaults\":{\"toast\":\"保存成功\"},\"params\":[{\"name\":\"url\",\"type\":\"string\",\"label\":\"URL\"},{\"name\":\"reload\",\"type\":\"string\",\"label\":\"刷新选择器\"},{\"name\":\"toast\",\"type\":\"string\",\"label\":\"成功提示\"}],\"i18n\":{\"en-US\":{\"label\":\"Save & Flush (Demo)\"}}}",
    "scriptContent": "function (ctx) { var o = ctx.options || {}; return dyn.postback(ctx.element, { url: o.url, reload: o.reload, message: o.toast || \"保存成功\" }).then(function () { try { window.dispatchEvent(new CustomEvent(\"saved\", { detail: { url: o.url } })); } catch (e) {} }); }",
    "isBuiltin": false
  }
];
    function boot() {
        if (!global.dyn || typeof global.dyn.registerActionHelper !== 'function') { setTimeout(boot, 250); return; }
        var added = 0;
        rows.forEach(function (r) {
            if (r && global.dyn.registerActionHelper(r)) added++;
        });
        if (global.dyn.autoBindActions) { try { global.dyn.autoBindActions(); } catch (e) {} }
        if (global.__DYN_DEBUG) console.log('[dyn] 已从 generated.js 注入动作助手 ' + added + ' 个');
    }
    if (typeof window !== 'undefined') boot();
})(typeof window !== 'undefined' ? window : this);
