/* ============================================================
 * VueLib 低代码平台 - 组合组件 Demo（40_CompositeDemo.sql）
 * ------------------------------------------------------------
 * 组合组件 = 内部组件树 + 开放属性(exposedProps) + 开放容器(openContainers)
 *   - exposedProps:  开放给外部配置的属性（设计器属性面板显示）
 *   - openContainers: 开放给外部拖入组件的容器（插槽），内容存实例 slots.{key}
 *   - 内部其余部分在设计器中锁定：不可选中/编辑/拖拽
 *
 * Demo1: DynComAddress   地址组合组件（三个输入框，只开放三个绑定字段）
 * Demo2: DynComActionCard 操作卡片（开放标题 + 一个"操作按钮区"开放容器）
 * ============================================================ */
USE [VueLib];
GO

/* ---------- Demo1: DynComAddress 地址组合组件 ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.ComponentMeta WHERE ComponentName = N'DynComAddress')
BEGIN
INSERT INTO dbo.ComponentMeta
    (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl,
     Description, IsEnabled, SortOrder, IsComposite, CompositeConfigJson)
VALUES
    (N'DynComAddress', 1, N'表单', N'地址组件', N'🏠',
     N'{"component":"DynComAddress","modelname":"","options":{"comoptions":{"firstName":"","lastName":"","address":""},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
     N'{"firstName":"","lastName":"","address":""}',
     N'/NutComponent/Container/DivContainer',
     N'组合组件：名/姓/地址 三个输入框，只开放三个绑定字段',
     1, 900, 1,
     N'{
        "tree": {
            "component": "DynNDivContainer",
            "modelname": "",
            "options": {
                "comoptions": {},
                "comlisteners": {},
                "labeloptions": {},
                "itemoptions": {"style": {"padding": "12px", "background": "#fff"}, "class": ""}
            },
            "validators": [],
            "childrenctrls": [
                {
                    "component": "DynNCellGroup",
                    "modelname": "",
                    "options": {
                        "comoptions": {"title": "地址信息"},
                        "comlisteners": {},
                        "labeloptions": {},
                        "itemoptions": {"style": {}, "class": ""}
                    },
                    "validators": [],
                    "childrenctrls": [
                        {
                            "component": "DynNInput",
                            "modelname": "firstName",
                            "options": {
                                "comoptions": {"placeholder": "请输入名", "clearable": true},
                                "comlisteners": {},
                                "labeloptions": {"label": "名", "required": false, "show": true},
                                "itemoptions": {"style": {}, "class": ""}
                            },
                            "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}
                        },
                        {
                            "component": "DynNInput",
                            "modelname": "lastName",
                            "options": {
                                "comoptions": {"placeholder": "请输入姓", "clearable": true},
                                "comlisteners": {},
                                "labeloptions": {"label": "姓", "required": false, "show": true},
                                "itemoptions": {"style": {}, "class": ""}
                            },
                            "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}
                        },
                        {
                            "component": "DynNInput",
                            "modelname": "address",
                            "options": {
                                "comoptions": {"placeholder": "请输入地址", "clearable": true},
                                "comlisteners": {},
                                "labeloptions": {"label": "地址", "required": false, "show": true},
                                "itemoptions": {"style": {}, "class": ""}
                            },
                            "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}
                        }
                    ]
                }
            ],
            "slots": {},
            "extendinfo": {}
        },
        "exposedProps": [
            {"key": "firstName", "target": "childrenctrls.0.childrenctrls.0.modelname", "label": "名-绑定字段", "type": "input", "placeholder": "如: contact.firstname"},
            {"key": "lastName",  "target": "childrenctrls.0.childrenctrls.1.modelname", "label": "姓-绑定字段", "type": "input", "placeholder": "如: contact.lastname"},
            {"key": "address",   "target": "childrenctrls.0.childrenctrls.2.modelname", "label": "地址-绑定字段", "type": "input", "placeholder": "如: contact.address"}
        ],
        "openContainers": []
     }');
PRINT 'DynComAddress 已插入';
END
GO

/* ---------- Demo2: DynComActionCard 操作卡片组合组件 ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.ComponentMeta WHERE ComponentName = N'DynComActionCard')
BEGIN
INSERT INTO dbo.ComponentMeta
    (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl,
     Description, IsEnabled, SortOrder, IsComposite, CompositeConfigJson)
VALUES
    (N'DynComActionCard', 4, N'通用', N'操作卡片', N'🎴',
     N'{"component":"DynComActionCard","modelname":"","options":{"comoptions":{"title":"卡片标题"},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{"actions":[]},"extendinfo":{}}',
     N'{"title":"卡片标题"}',
     N'/NutComponent/Container/DivContainer',
     N'组合组件：卡片带一个可拖入按钮的开放容器（操作按钮区），开放标题属性',
     1, 901, 1,
     N'{
        "tree": {
            "component": "DynNDivContainer",
            "modelname": "",
            "options": {
                "comoptions": {},
                "comlisteners": {},
                "labeloptions": {},
                "itemoptions": {"style": {"padding": "12px", "background": "#fff", "borderRadius": "8px", "border": "1px solid #ebeef5"}, "class": ""}
            },
            "validators": [],
            "childrenctrls": [
                {
                    "component": "DynNCellGroup",
                    "modelname": "",
                    "options": {
                        "comoptions": {"title": "卡片标题"},
                        "comlisteners": {},
                        "labeloptions": {},
                        "itemoptions": {"style": {}, "class": ""}
                    },
                    "validators": [],
                    "childrenctrls": [
                        {
                            "component": "DynNText",
                            "modelname": "",
                            "options": {
                                "comoptions": {"text": "这里是卡片内容，可在下方开放容器中拖入按钮", "size": "base", "color": "#606266"},
                                "comlisteners": {},
                                "labeloptions": {},
                                "itemoptions": {"style": {}, "class": ""}
                            },
                            "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}
                        }
                    ]
                },
                {
                    "component": "DynNCellGroup",
                    "modelname": "",
                    "options": {
                        "comoptions": {"title": ""},
                        "comlisteners": {},
                        "labeloptions": {},
                        "itemoptions": {"style": {}, "class": ""}
                    },
                    "validators": [],
                    "childrenctrls": [],
                    "slots": {}, "extendinfo": {}
                }
            ],
            "slots": {},
            "extendinfo": {}
        },
        "exposedProps": [
            {"key": "title", "target": "childrenctrls.0.options.comoptions.title", "label": "卡片标题", "type": "input"}
        ],
        "openContainers": [
            {"key": "actions", "target": "childrenctrls.1", "label": "操作按钮区", "hint": "可拖入按钮组件"}
        ]
     }');
PRINT 'DynComActionCard 已插入';
END
GO

PRINT '组合组件 Demo 数据全部完成';
GO
