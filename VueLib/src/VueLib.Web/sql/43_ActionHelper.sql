/* ============================================================================
 * 43_ActionHelper.sql —— 动作助手（actionhelper）注册表
 * ----------------------------------------------------------------------------
 * 目标：
 *   1. 新增 DynActionHelper 表：统一登记"动作助手"（dyn-click-{code} / dyn-init-{code}）
 *   2. 内置动作 META 目录（IsBuiltin=1，只读参考）：与 dyn-actionhelper.js 对应
 *   3. Demo 自定义动作（IsBuiltin=0，带完整 META + ScriptContent，可编辑/删除/测试）
 *   4. 供 /api/dynactionhelper 返回给前端：前端动态把 DB 动作并入 dyn.actionHelper
 * ============================================================================ */

IF OBJECT_ID('dbo.DynActionHelper', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DynActionHelper
    (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        Name            NVARCHAR(100)  NOT NULL,               -- 显示名
        Code            NVARCHAR(100)  NOT NULL,               -- 动作名（dyn-click-{code}）
        Category        NVARCHAR(50)   NULL,                   -- 分类：flow / data / window / ui / system
        MetaJson        NVARCHAR(MAX)  NULL,                   -- META：{label,doc,events,defaults,params[],i18n{}}
        ScriptContent   NVARCHAR(MAX)  NULL,                   -- JS 函数体 function(ctx){ ... }
        IsBuiltin       BIT            NOT NULL DEFAULT 0,     -- 1=内置动作（只读目录），0=自定义（可编辑）
        IsEnabled       BIT            NOT NULL DEFAULT 1,
        SortOrder       INT            NOT NULL DEFAULT 0,
        Remark          NVARCHAR(500)  NULL,
        CreatedAt       DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_DynActionHelper_Code UNIQUE (Code)
    );
END
GO

/* ============================================================================
 * 内置动作 META 目录（IsBuiltin=1，与 dyn-actionhelper.js 内置动作对应）
 * ============================================================================ */
IF NOT EXISTS (SELECT 1 FROM dbo.DynActionHelper WHERE Code = 'postback')
BEGIN
    INSERT INTO dbo.DynActionHelper (Name, Code, Category, MetaJson, IsBuiltin, IsEnabled, SortOrder, Remark) VALUES
    (N'提交回发', N'postback', N'data', N'{"label":"提交回发","doc":"把最近 dyn-init 祖先的 Model 序列化 POST 给后端；返回 HTML 渲染回容器，返回 JSON 合并进 Model。","events":["click","change"],"defaults":{"method":"POST"},"params":[{"name":"url","type":"string","label":"URL","required":true},{"name":"confirm","type":"string","label":"确认文案"},{"name":"reload","type":"string","label":"刷新选择器"},{"name":"message","type":"string","label":"成功提示"}],"i18n":{"en-US":{"label":"Postback","doc":"POST nearest dyn-init ancestor Model to backend; render returned HTML back."}}}', 1, 1, 10, N'内置：postback'),
    (N'打开窗体', N'openwindow', N'window', N'{"label":"打开窗体","doc":"打开窗体：type=auto(自动)/modal(ElementPlus 模态)/layer(LayUI)/newtab(新标签)/window(轻量窗口)；events.onopen/onclose 可挂动作链。","events":["click"],"defaults":{"type":"auto","width":"60%"},"params":[{"name":"url","type":"string","label":"URL"},{"name":"title","type":"string","label":"标题"},{"name":"type","type":"string","label":"类型 auto|modal|layer|newtab|window"},{"name":"width","type":"string","label":"宽度"},{"name":"height","type":"string","label":"高度"}],"i18n":{"en-US":{"label":"Open Window"}}}', 1, 1, 20, N'内置：openwindow'),
    (N'动作链', N'chain', N'flow', N'{"label":"动作链","doc":"按顺序 await 执行 steps；步骤返回 false 中止；上一步返回值注入下一步 ctx.$result；events.onopen/onclose 在窗体打开/关闭时执行子链。","events":["click"],"defaults":{"steps":[]},"params":[{"name":"steps","type":"json","label":"步骤数组"}],"i18n":{"en-US":{"label":"Action Chain"}}}', 1, 1, 30, N'内置：chain'),
    (N'条件分支', N'switch', N'flow', N'{"label":"条件分支","doc":"按表达式值匹配 cases 执行对应动作链；命中 default 走兜底；无匹配静默。","events":["click","change"],"defaults":{"expr":"","cases":[],"default":null},"params":[{"name":"expr","type":"string","label":"表达式"},{"name":"cases","type":"json","label":"分支表"}],"i18n":{"en-US":{"label":"Switch Action"}}}', 1, 1, 40, N'内置：switch'),
    (N'设置变量', N'setvar', N'data', N'{"label":"设置变量","doc":"设置目标 Vue model 变量：modelName 点路径/数组下标，value 支持 {{params}} 占位；delay 延迟毫秒。","events":["click","change"],"defaults":{"modelName":"","delay":0},"params":[{"name":"modelName","type":"string","label":"Model 路径"},{"name":"value","type":"string","label":"值"},{"name":"delay","type":"number","label":"延迟 ms"}],"i18n":{"en-US":{"label":"Set Variable"}}}', 1, 1, 50, N'内置：setvar'),
    (N'延时', N'delay', N'flow', N'{"label":"延时","doc":"等待指定毫秒后继续（常用于 chain 步骤）。","events":[],"defaults":{"ms":300},"params":[{"name":"ms","type":"number","label":"毫秒"}],"i18n":{"en-US":{"label":"Delay"}}}', 1, 1, 60, N'内置：delay'),
    (N'消息提示', N'notify', N'ui', N'{"label":"消息提示","doc":"ElementPlus 通知：type=success|warning|error|info，title/message 支持 {{params}} 占位。","events":["click","change"],"defaults":{"type":"success"},"params":[{"name":"message","type":"string","label":"内容"},{"name":"title","type":"string","label":"标题"},{"name":"type","type":"string","label":"类型"}],"i18n":{"en-US":{"label":"Notify"}}}', 1, 1, 70, N'内置：notify'),
    (N'触发事件', N'triggerevent', N'flow', N'{"label":"触发事件","doc":"触发全局/窗口事件总线：type=eventBus|window，name 事件名，payload 参数对象。","events":["click","change"],"defaults":{"type":"eventBus","name":""},"params":[{"name":"type","type":"string","label":"eventBus|window"},{"name":"name","type":"string","label":"事件名"},{"name":"payload","type":"json","label":"参数"}],"i18n":{"en-US":{"label":"Trigger Event"}}}', 1, 1, 80, N'内置：triggerevent'),
    (N'关闭窗体', N'close', N'window', N'{"label":"关闭窗体","doc":"关闭最近的模态/轻量窗体。","events":["click"],"defaults":{},"i18n":{"en-US":{"label":"Close Window"}}}', 1, 1, 90, N'内置：close'),
    (N'刷新区域', N'reload', N'data', N'{"label":"刷新区域","doc":"重新加载 data-dyn-url 容器（selector 可指定目标）。","events":["click","change"],"defaults":{},"params":[{"name":"selector","type":"string","label":"选择器"}],"i18n":{"en-US":{"label":"Reload"}}}', 1, 1, 100, N'内置：reload'),
    (N'执行脚本', N'evaljs', N'flow', N'{"label":"执行脚本","doc":"new Function 执行 JS 代码片段（可用 ctx）。","events":["click","change"],"defaults":{},"params":[{"name":"code","type":"text","label":"JS 代码"}],"i18n":{"en-US":{"label":"Eval JS"}}}', 1, 1, 110, N'内置：evaljs'),
    (N'设置模型值', N'setvuemodel', N'data', N'{"label":"设置模型值","doc":"设置 Vue model 路径值（兼容旧属性 dyn-click-setVueModel）。","events":["click","change"],"defaults":{"modelName":"","model":""},"i18n":{"en-US":{"label":"Set Vue Model"}}}', 1, 1, 120, N'内置：setVueModel'),
    (N'设置窗口', N'setwindow', N'window', N'{"label":"设置窗口","doc":"设置所在窗体：title/width/height/fullscreen/minimize/close。","events":["click"],"defaults":{},"i18n":{"en-US":{"label":"Set Window"}}}', 1, 1, 130, N'内置：setwindow'),
    (N'设置组件配置', N'setdyncom', N'data', N'{"label":"设置组件配置","doc":"更新目标 DynCom 组件 configjson/modeljson（merge/replace）。","events":["click"],"defaults":{"mode":"merge"},"i18n":{"en-US":{"label":"Set DynCom"}}}', 1, 1, 140, N'内置：setdyncom'),
    (N'Toast 提示', N'toast', N'ui', N'{"label":"Toast 提示","doc":"轻量提示（ElementPlus Message），type=success|error|warning|info。","events":["click","change"],"defaults":{"type":"success"},"i18n":{"en-US":{"label":"Toast"}}}', 1, 1, 150, N'内置：toast'),
    (N'复制文本', N'copy', N'ui', N'{"label":"复制文本","doc":"把 text 写入剪贴板。","events":["click"],"defaults":{},"i18n":{"en-US":{"label":"Copy"}}}', 1, 1, 160, N'内置：copy'),
    (N'下载文件', N'download', N'ui', N'{"label":"下载文件","doc":"打开 url 下载（新标签）。","events":["click"],"defaults":{},"i18n":{"en-US":{"label":"Download"}}}', 1, 1, 170, N'内置：download'),
    (N'3屏管理', N'grid', N'data', N'{"label":"3屏管理","doc":"Filter+Grid+Detail 管理：action=search|clear|add|edit|delete|load。","events":["click"],"defaults":{"action":"search"},"i18n":{"en-US":{"label":"Grid Manager"}}}', 1, 1, 180, N'内置：grid');
END
GO

/* ============================================================================
 * Demo 自定义动作（IsBuiltin=0）：可直接在管理页编辑/测试，前端动态注册
 * ============================================================================ */
IF NOT EXISTS (SELECT 1 FROM dbo.DynActionHelper WHERE Code = 'confirmpost')
BEGIN
    INSERT INTO dbo.DynActionHelper (Name, Code, Category, MetaJson, ScriptContent, IsBuiltin, IsEnabled, SortOrder, Remark) VALUES
    (N'确认后提交（Demo）', N'confirmpost', N'flow',
     N'{"label":"确认后提交（Demo）","doc":"示例自定义动作：先弹确认框，确认后把 Model 提交到 url，成功后刷新 selector 区域。","events":["click"],"defaults":{"confirm":"确认提交？","method":"POST"},"params":[{"name":"url","type":"string","label":"URL","required":true},{"name":"confirm","type":"string","label":"确认文案"},{"name":"reload","type":"string","label":"刷新选择器"}],"i18n":{"en-US":{"label":"Confirm & Post (Demo)"}}}',
     N'function (ctx) { var o = ctx.options || {}; var msg = o.confirm || "确认提交？"; return dyn.confirmAsync(msg).then(function (ok) { if (!ok) return; return dyn.postback(ctx.element, { url: o.url, reload: o.reload, message: o.message || "已提交" }); }); }',
     0, 1, 10, N'Demo：确认框 + 提交 + 刷新'),
    (N'打开详情窗口（Demo）', N'opendetail', N'window',
     N'{"label":"打开详情窗口（Demo）","doc":"示例自定义动作：打开详情窗口，并把当前行 data-id 拼进 URL；onclose 触发事件总线 detailClosed。","events":["click"],"defaults":{"title":"详情","width":"60%"},"params":[{"name":"url","type":"string","label":"URL"},{"name":"title","type":"string","label":"标题"},{"name":"width","type":"string","label":"宽度"}],"i18n":{"en-US":{"label":"Open Detail (Demo)"}}}',
     N'function (ctx) { var o = ctx.options || {}; var id = ctx.params.id || ""; var url = o.url || ""; if (id) url += (url.indexOf("?") >= 0 ? "&" : "?") + "id=" + encodeURIComponent(id); var host = dyn.openwindow({ url: url, title: o.title || "详情", width: o.width || "60%", type: o.type || "modal", events: { onclose: [{ action: "triggerevent", options: { name: "detailClosed", payload: { id: id } } }] } }, ctx.element); return host; }',
     0, 1, 20, N'Demo：打开详情 + 关闭事件'),
    (N'按状态分支（Demo）', N'statusflow', N'flow',
     N'{"label":"按状态分支（Demo）","doc":"示例自定义动作：按 Model.Status 值分支，draft→弹窗，released→新标签，其它→toast。","events":["click"],"defaults":{},"params":[{"name":"field","type":"string","label":"字段名","default":"Status"}],"i18n":{"en-US":{"label":"Status Flow (Demo)"}}}',
     N'function (ctx) { var o = ctx.options || {}; var f = o.field || "Status"; var v = (ctx.model || {})[f]; if (v === "草稿" || v === "draft") return dyn.actionHelper.openwindow({ url: o.draftUrl, title: "草稿编辑", type: "modal" }, ctx.element); if (v === "已发布" || v === "released") { window.open(o.releaseUrl || "/", "_blank"); return; } return dyn.showMessage("当前状态：" + v + "，无后续动作", "info"); }',
     0, 1, 30, N'Demo：按 Model 字段值分支'),
    (N'保存后刷新并提示（Demo）', N'saveflush', N'data',
     N'{"label":"保存后刷新并提示（Demo）","doc":"示例自定义动作：POST Model 到 url，成功后 toast + 刷新 selector，并触发 window 事件 saved。","events":["click"],"defaults":{"toast":"保存成功"},"params":[{"name":"url","type":"string","label":"URL"},{"name":"reload","type":"string","label":"刷新选择器"},{"name":"toast","type":"string","label":"成功提示"}],"i18n":{"en-US":{"label":"Save & Flush (Demo)"}}}',
     N'function (ctx) { var o = ctx.options || {}; return dyn.postback(ctx.element, { url: o.url, reload: o.reload, message: o.toast || "保存成功" }).then(function () { try { window.dispatchEvent(new CustomEvent("saved", { detail: { url: o.url } })); } catch (e) {} }); }',
     0, 1, 40, N'Demo：保存 + 刷新 + 事件');
END
GO
