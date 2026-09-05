/* ============================================================
 * VueLib 低代码平台 - ForEach 循环容器 + 透明容器（Fragment）
 * 1) 新增 4 个组件：DynNFragment / DynNForEach / DynElFragment / DynElForEach
 * 2) 新增 demo 页面：foreach-demo（ForEach 列表 + Fragment 透明容器）
 * ============================================================ */
USE VueLib;
GO

/* ==================== 1) 组件元数据 ==================== */

-- 透明容器（NutUI，运行时不产生外层 div）
IF NOT EXISTS (SELECT 1 FROM dbo.ComponentMeta WHERE ComponentName = N'DynNFragment')
BEGIN
    INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
    VALUES (N'DynNFragment', 2, N'布局', N'透明容器', N'🫥',
        N'{"component":"DynNFragment","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""},"wrapperoptions":{}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
        N'{}',
        N'/NutComponent/Container/Fragment',
        N'无外层 div 的透明容器，直接透出子组件（适合分组不产生多余嵌套）', 1, 3);
    PRINT 'DynNFragment 已添加';
END
GO

-- 循环容器（NutUI，按 model 数组循环渲染子组件）
IF NOT EXISTS (SELECT 1 FROM dbo.ComponentMeta WHERE ComponentName = N'DynNForEach')
BEGIN
    INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
    VALUES (N'DynNForEach', 2, N'布局', N'循环容器', N'🔁',
        N'{"component":"DynNForEach","modelname":"","options":{"comoptions":{"dataSource":"items","itemVar":"item","indexVar":"index","emptyText":""},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""},"wrapperoptions":{}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
        N'{}',
        N'/NutComponent/Container/ForEach',
        N'ForEach 循环容器：按 model 数组字段循环渲染子组件（子组件 modelname 相对当前项绑定）', 1, 4);
    PRINT 'DynNForEach 已添加';
END
GO

-- 透明容器（ElementUI 版）
IF NOT EXISTS (SELECT 1 FROM dbo.ComponentMeta WHERE ComponentName = N'DynElFragment')
BEGIN
    INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
    VALUES (N'DynElFragment', 2, N'布局', N'透明容器', N'🫥',
        N'{"component":"DynElFragment","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""},"wrapperoptions":{}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
        N'{}',
        N'/ElementComponent/Container/Fragment',
        N'无外层 div 的透明容器（ElementUI），直接透出子组件', 1, 3);
    PRINT 'DynElFragment 已添加';
END
GO

-- 循环容器（ElementUI 版）
IF NOT EXISTS (SELECT 1 FROM dbo.ComponentMeta WHERE ComponentName = N'DynElForEach')
BEGIN
    INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
    VALUES (N'DynElForEach', 2, N'布局', N'循环容器', N'🔁',
        N'{"component":"DynElForEach","modelname":"","options":{"comoptions":{"dataSource":"items","itemVar":"item","indexVar":"index","emptyText":""},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""},"wrapperoptions":{}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
        N'{}',
        N'/ElementComponent/Container/ForEach',
        N'ForEach 循环容器（ElementUI）：按 model 数组字段循环渲染子组件', 1, 4);
    PRINT 'DynElForEach 已添加';
END
GO

/* ==================== 2) 属性配置（属性设计器可编辑） ==================== */

-- ForEach：数据源 + 空文案 + 容器样式
UPDATE dbo.ComponentMeta SET PropertyConfigJson = N'{"groups":[{"title":"循环配置","fields":[
    {"key":"options.comoptions.dataSource","label":"数据源字段","type":"input","default":"items","placeholder":"model 中的数组字段，如 items"},
    {"key":"options.comoptions.itemVar","label":"循环变量名","type":"input","default":"item"},
    {"key":"options.comoptions.indexVar","label":"索引变量名","type":"input","default":"index"},
    {"key":"options.comoptions.emptyText","label":"空数据文案","type":"input","default":""}
]},{"title":"容器样式","fields":[
    {"key":"options.itemoptions.style.display","label":"显示方式","type":"select","default":"block","options":[{"label":"块级","value":"block"},{"label":"弹性","value":"flex"},{"label":"行内","value":"inline-block"}]},
    {"key":"options.itemoptions.style.flexDirection","label":"弹性方向","type":"select","default":"row","options":[{"label":"横向","value":"row"},{"label":"纵向","value":"column"}]},
    {"key":"options.itemoptions.style.gap","label":"间距","type":"input","default":""},
    {"key":"options.itemoptions.style.padding","label":"内边距","type":"input","default":""},
    {"key":"options.itemoptions.style.marginBottom","label":"下边距","type":"input","default":""},
    {"key":"options.itemoptions.class","label":"自定义类名","type":"input","default":""}
]}]}'
WHERE ComponentName IN (N'DynNForEach', N'DynElForEach');
PRINT 'ForEach 属性配置已更新';

-- Fragment：容器样式
UPDATE dbo.ComponentMeta SET PropertyConfigJson = N'{"groups":[{"title":"容器样式","fields":[
    {"key":"options.itemoptions.style.display","label":"显示方式","type":"select","default":"block","options":[{"label":"块级","value":"block"},{"label":"弹性","value":"flex"},{"label":"行内","value":"inline-block"}]},
    {"key":"options.itemoptions.style.flexDirection","label":"弹性方向","type":"select","default":"row","options":[{"label":"横向","value":"row"},{"label":"纵向","value":"column"}]},
    {"key":"options.itemoptions.style.gap","label":"间距","type":"input","default":""},
    {"key":"options.itemoptions.style.padding","label":"内边距","type":"input","default":""},
    {"key":"options.itemoptions.class","label":"自定义类名","type":"input","default":""}
]}]}'
WHERE ComponentName IN (N'DynNFragment', N'DynElFragment');
PRINT 'Fragment 属性配置已更新';
GO

/* ==================== 3) Demo 页面（ForEach 列表 + Fragment 透明容器） ==================== */

IF NOT EXISTS (SELECT 1 FROM dbo.PageSetting WHERE PageCode = N'foreach-demo')
BEGIN
    INSERT INTO dbo.PageSetting (PageName, PageCode, Category, Icon, ConfigJson, DefaultModelJson, Description, IsEnabled, SortOrder)
    VALUES (N'ForEach 循环容器 Demo', N'foreach-demo', N'Demo', N'🔁',
        N'{
  "component": "DynNDivContainer",
  "modelname": "",
  "options": {
    "comoptions": {},
    "comlisteners": {},
    "labeloptions": {},
    "itemoptions": { "style": { "padding": "12px" }, "class": "" },
    "wrapperoptions": {}
  },
  "validators": [],
  "childrenctrls": [
    {
      "component": "DynNForEach",
      "modelname": "",
      "options": {
        "comoptions": { "dataSource": "users", "itemVar": "item", "indexVar": "index", "emptyText": "暂无用户数据" },
        "comlisteners": {},
        "labeloptions": {},
        "itemoptions": { "style": { "border": "1px dashed #ccc", "borderRadius": "8px", "padding": "8px", "marginBottom": "8px" }, "class": "" },
        "wrapperoptions": {}
      },
      "validators": [],
      "childrenctrls": [
        {
          "component": "DynNInput",
          "modelname": "name",
          "options": {
            "comoptions": { "placeholder": "姓名" },
            "comlisteners": {},
            "labeloptions": { "label": "姓名", "required": false, "show": true },
            "itemoptions": { "style": { "marginBottom": "6px" }, "class": "" },
            "wrapperoptions": {}
          },
          "validators": [],
          "childrenctrls": [],
          "slots": {},
          "extendinfo": {}
        },
        {
          "component": "DynNInput",
          "modelname": "age",
          "options": {
            "comoptions": { "placeholder": "年龄", "type": "number" },
            "comlisteners": {},
            "labeloptions": { "label": "年龄", "required": false, "show": true },
            "itemoptions": { "style": { "marginBottom": "6px" }, "class": "" },
            "wrapperoptions": {}
          },
          "validators": [],
          "childrenctrls": [],
          "slots": {},
          "extendinfo": {}
        }
      ],
      "slots": {},
      "extendinfo": {}
    },
    {
      "component": "DynNFragment",
      "modelname": "",
      "options": {
        "comoptions": {},
        "comlisteners": {},
        "labeloptions": {},
        "itemoptions": { "style": {}, "class": "" },
        "wrapperoptions": {}
      },
      "validators": [],
      "childrenctrls": [
        {
          "component": "DynNButton",
          "modelname": "",
          "options": {
            "comoptions": { "text": "透明容器里的按钮（无外层 div）", "type": "primary", "block": true },
            "comlisteners": {},
            "labeloptions": {},
            "itemoptions": { "style": {}, "class": "" },
            "wrapperoptions": {}
          },
          "validators": [],
          "childrenctrls": [],
          "slots": {},
          "extendinfo": {}
        }
      ],
      "slots": {},
      "extendinfo": {}
    }
  ],
  "slots": {},
  "extendinfo": {}
}',
        N'{"users":[{"name":"张三","age":18},{"name":"李四","age":20},{"name":"王五","age":22}]}',
        N'演示 DynNForEach 循环渲染列表 + DynNFragment 透明容器（无外层 div）', 1, 10);
    PRINT 'foreach-demo 页面已添加';
END
GO
