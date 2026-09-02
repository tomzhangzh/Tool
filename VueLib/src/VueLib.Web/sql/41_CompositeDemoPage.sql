/* ============================================================
 * VueLib 低代码平台 - 组合组件 Demo 页面（41_CompositeDemoPage.sql）
 * 演示 DynComAddress（开放属性）+ DynComActionCard（开放容器）
 * ============================================================ */
USE [VueLib];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PageSetting WHERE PageCode = N'composite-demo')
BEGIN
INSERT INTO dbo.PageSetting
    (PageName, PageCode, Category, Icon, ConfigJson, DefaultModelJson, ApiBaseUrl, Description, IsEnabled, SortOrder)
VALUES
    (N'组合组件Demo', N'composite-demo', N'demo', N'🧩',
     N'{
        "component": "DynNDivContainer",
        "modelname": "",
        "options": {
            "comoptions": {},
            "comlisteners": {},
            "labeloptions": {},
            "itemoptions": {"style": {"padding": "12px", "background": "#f5f6fa"}, "class": ""}
        },
        "validators": [],
        "childrenctrls": [
            {
                "component": "DynComAddress",
                "modelname": "",
                "options": {
                    "comoptions": {"firstName": "contact.firstname", "lastName": "contact.lastname", "address": "contact.address"},
                    "comlisteners": {},
                    "labeloptions": {},
                    "itemoptions": {"style": {}, "class": ""}
                },
                "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}
            },
            {
                "component": "DynComActionCard",
                "modelname": "",
                "options": {
                    "comoptions": {"title": "联系人操作"},
                    "comlisteners": {},
                    "labeloptions": {},
                    "itemoptions": {"style": {}, "class": ""}
                },
                "validators": [], "childrenctrls": [],
                "slots": {
                    "actions": [
                        {
                            "component": "DynNButton",
                            "modelname": "",
                            "options": {
                                "comoptions": {"text": "保存", "type": "primary", "block": true},
                                "comlisteners": {"click": ""},
                                "labeloptions": {},
                                "itemoptions": {"style": {}, "class": ""}
                            },
                            "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}
                        }
                    ]
                },
                "extendinfo": {}
            }
        ],
        "slots": {},
        "extendinfo": {}
     }',
     N'{"contact": {"firstname": "", "lastname": "", "address": ""}}',
     NULL,
     N'组合组件演示：地址组件(开放属性) + 操作卡片(开放容器)',
     1, 100);
PRINT 'composite-demo 页面已插入';
END
GO
PRINT '完成';
GO
