/* ============================================================
 * VueLib 低代码平台 - 组件属性配置种子数据
 * 为常用组件生成 PropertyConfigJson（动态属性面板配置）
 * ============================================================ */
USE VueLib;
GO

/* ==================== 通用属性字段模板 ==================== */
/* 标签配置 */
DECLARE @labelFields NVARCHAR(MAX) = N'[
    {"key":"options.labeloptions.label","label":"标签文字","type":"input","default":""},
    {"key":"options.labeloptions.required","label":"必填","type":"switch","default":false},
    {"key":"options.labeloptions.show","label":"显示标签","type":"switch","default":true}
]';

/* 样式配置 */
DECLARE @styleFields NVARCHAR(MAX) = N'[
    {"key":"options.itemoptions.style.width","label":"宽度","type":"input","default":"100%"},
    {"key":"options.itemoptions.style.marginTop","label":"上边距","type":"input","default":""},
    {"key":"options.itemoptions.style.marginBottom","label":"下边距","type":"input","default":""},
    {"key":"options.itemoptions.class","label":"自定义类名","type":"input","default":""}
]';

/* ==================== NInput 输入框 ==================== */
UPDATE dbo.ComponentMeta SET PropertyConfigJson = N'{
  "groups": [
    {
      "title": "基础属性",
      "fields": [
        {"key":"options.comoptions.placeholder","label":"占位符","type":"input","default":"请输入"},
        {"key":"options.comoptions.type","label":"输入类型","type":"select","default":"text","options":[{"label":"文本","value":"text"},{"label":"密码","value":"password"},{"label":"数字","value":"number"},{"label":"电话","value":"tel"},{"label":"邮箱","value":"email"}]},
        {"key":"options.comoptions.clearable","label":"可清除","type":"switch","default":true},
        {"key":"options.comoptions.disabled","label":"禁用","type":"switch","default":false},
        {"key":"options.comoptions.readonly","label":"只读","type":"switch","default":false},
        {"key":"options.comoptions.maxlength","label":"最大长度","type":"number","default":200},
        {"key":"options.comoptions.showCount","label":"显示字数","type":"switch","default":false}
      ]
    },
    {
      "title": "标签配置",
      "fields": ' + @labelFields + N'
    },
    {
      "title": "样式",
      "fields": ' + @styleFields + N'
    }
  ]
}' WHERE ComponentName = 'NInput';

/* ==================== NTextarea 文本域 ==================== */
UPDATE dbo.ComponentMeta SET PropertyConfigJson = N'{
  "groups": [
    {
      "title": "基础属性",
      "fields": [
        {"key":"options.comoptions.placeholder","label":"占位符","type":"input","default":"请输入"},
        {"key":"options.comoptions.rows","label":"行数","type":"number","default":3},
        {"key":"options.comoptions.maxlength","label":"最大长度","type":"number","default":500},
        {"key":"options.comoptions.showCount","label":"显示字数","type":"switch","default":true},
        {"key":"options.comoptions.clearable","label":"可清除","type":"switch","default":true},
        {"key":"options.comoptions.disabled","label":"禁用","type":"switch","default":false},
        {"key":"options.comoptions.autosize","label":"自适应高度","type":"switch","default":false}
      ]
    },
    {
      "title": "标签配置",
      "fields": ' + @labelFields + N'
    },
    {
      "title": "样式",
      "fields": ' + @styleFields + N'
    }
  ]
}' WHERE ComponentName = 'NTextarea';

/* ==================== NSwitch 开关 ==================== */
UPDATE dbo.ComponentMeta SET PropertyConfigJson = N'{
  "groups": [
    {
      "title": "基础属性",
      "fields": [
        {"key":"options.comoptions.disabled","label":"禁用","type":"switch","default":false},
        {"key":"options.comoptions.activeText","label":"开启文字","type":"input","default":""},
        {"key":"options.comoptions.inactiveText","label":"关闭文字","type":"input","default":""},
        {"key":"options.comoptions.activeValue","label":"开启值","type":"input","default":true},
        {"key":"options.comoptions.inactiveValue","label":"关闭值","type":"input","default":false}
      ]
    },
    {
      "title": "标签配置",
      "fields": ' + @labelFields + N'
    },
    {
      "title": "样式",
      "fields": ' + @styleFields + N'
    }
  ]
}' WHERE ComponentName = 'NSwitch';

/* ==================== NStepper 步进器 ==================== */
UPDATE dbo.ComponentMeta SET PropertyConfigJson = N'{
  "groups": [
    {
      "title": "基础属性",
      "fields": [
        {"key":"options.comoptions.min","label":"最小值","type":"number","default":0},
        {"key":"options.comoptions.max","label":"最大值","type":"number","default":999},
        {"key":"options.comoptions.step","label":"步长","type":"number","default":1},
        {"key":"options.comoptions.disabled","label":"禁用","type":"switch","default":false},
        {"key":"options.comoptions.readonly","label":"只读","type":"switch","default":false},
        {"key":"options.comoptions.integer","label":"仅整数","type":"switch","default":true}
      ]
    },
    {
      "title": "标签配置",
      "fields": ' + @labelFields + N'
    },
    {
      "title": "样式",
      "fields": ' + @styleFields + N'
    }
  ]
}' WHERE ComponentName = 'NStepper';

/* ==================== NRate 评分 ==================== */
UPDATE dbo.ComponentMeta SET PropertyConfigJson = N'{
  "groups": [
    {
      "title": "基础属性",
      "fields": [
        {"key":"options.comoptions.count","label":"星星数量","type":"number","default":5},
        {"key":"options.comoptions.disabled","label":"禁用","type":"switch","default":false},
        {"key":"options.comoptions.readonly","label":"只读","type":"switch","default":false},
        {"key":"options.comoptions.allowHalf","label":"允许半选","type":"switch","default":false},
        {"key":"options.comoptions.activeColor","label":"选中颜色","type":"color","default":"#fa200c"},
        {"key":"options.comoptions.inactiveColor","label":"未选颜色","type":"color","default":"#c8c9cc"}
      ]
    },
    {
      "title": "标签配置",
      "fields": ' + @labelFields + N'
    },
    {
      "title": "样式",
      "fields": ' + @styleFields + N'
    }
  ]
}' WHERE ComponentName = 'NRate';

/* ==================== NSlider 滑块 ==================== */
UPDATE dbo.ComponentMeta SET PropertyConfigJson = N'{
  "groups": [
    {
      "title": "基础属性",
      "fields": [
        {"key":"options.comoptions.min","label":"最小值","type":"number","default":0},
        {"key":"options.comoptions.max","label":"最大值","type":"number","default":100},
        {"key":"options.comoptions.step","label":"步长","type":"number","default":1},
        {"key":"options.comoptions.disabled","label":"禁用","type":"switch","default":false},
        {"key":"options.comoptions.activeColor","label":"进度颜色","type":"color","default":"#4A90D9"},
        {"key":"options.comoptions.inactiveColor","label":"轨道颜色","type":"color","default":"#e4e7ed"}
      ]
    },
    {
      "title": "标签配置",
      "fields": ' + @labelFields + N'
    },
    {
      "title": "样式",
      "fields": ' + @styleFields + N'
    }
  ]
}' WHERE ComponentName = 'NSlider';

/* ==================== NButton 按钮 ==================== */
UPDATE dbo.ComponentMeta SET PropertyConfigJson = N'{
  "groups": [
    {
      "title": "基础属性",
      "fields": [
        {"key":"options.comoptions.text","label":"按钮文字","type":"input","default":"按钮"},
        {"key":"options.comoptions.type","label":"类型","type":"select","default":"default","options":[{"label":"主要","value":"primary"},{"label":"成功","value":"success"},{"label":"警告","value":"warning"},{"label":"危险","value":"danger"},{"label":"默认","value":"default"}]},
        {"key":"options.comoptions.size","label":"尺寸","type":"select","default":"normal","options":[{"label":"大","value":"large"},{"label":"正常","value":"normal"},{"label":"小","value":"small"}]},
        {"key":"options.comoptions.plain","label":"朴素","type":"switch","default":false},
        {"key":"options.comoptions.round","label":"圆角","type":"switch","default":false},
        {"key":"options.comoptions.block","label":"块级","type":"switch","default":false},
        {"key":"options.comoptions.disabled","label":"禁用","type":"switch","default":false},
        {"key":"options.comoptions.loading","label":"加载中","type":"switch","default":false},
        {"key":"options.comoptions.color","label":"自定义颜色","type":"color","default":""}
      ]
    },
    {
      "title": "样式",
      "fields": ' + @styleFields + N'
    }
  ]
}' WHERE ComponentName = 'NButton';

/* ==================== NText 文本 ==================== */
UPDATE dbo.ComponentMeta SET PropertyConfigJson = N'{
  "groups": [
    {
      "title": "基础属性",
      "fields": [
        {"key":"options.comoptions.text","label":"文本内容","type":"textarea","default":""},
        {"key":"options.comoptions.style.fontSize","label":"字号","type":"input","default":"14px"},
        {"key":"options.comoptions.style.fontWeight","label":"字重","type":"select","default":"normal","options":[{"label":"正常","value":"normal"},{"label":"加粗","value":"bold"},{"label":"更粗","value":"bolder"}]},
        {"key":"options.comoptions.style.color","label":"文字颜色","type":"color","default":"#323233"},
        {"key":"options.comoptions.style.textAlign","label":"对齐方式","type":"select","default":"left","options":[{"label":"左对齐","value":"left"},{"label":"居中","value":"center"},{"label":"右对齐","value":"right"}]},
        {"key":"options.comoptions.style.lineHeight","label":"行高","type":"input","default":""}
      ]
    },
    {
      "title": "样式",
      "fields": ' + @styleFields + N'
    }
  ]
}' WHERE ComponentName = 'NText';

/* ==================== NDivContainer 容器 ==================== */
UPDATE dbo.ComponentMeta SET PropertyConfigJson = N'{
  "groups": [
    {
      "title": "布局",
      "fields": [
        {"key":"options.itemoptions.style.display","label":"显示方式","type":"select","default":"block","options":[{"label":"块级","value":"block"},{"label":"弹性","value":"flex"},{"label":"行内","value":"inline-block"},{"label":"网格","value":"grid"}]},
        {"key":"options.itemoptions.style.flexDirection","label":"弹性方向","type":"select","default":"row","options":[{"label":"横向","value":"row"},{"label":"纵向","value":"column"}]},
        {"key":"options.itemoptions.style.justifyContent","label":"主轴对齐","type":"select","default":"flex-start","options":[{"label":"起始","value":"flex-start"},{"label":"居中","value":"center"},{"label":"末尾","value":"flex-end"},{"label":"两端","value":"space-between"},{"label":"环绕","value":"space-around"}]},
        {"key":"options.itemoptions.style.alignItems","label":"交叉轴对齐","type":"select","default":"stretch","options":[{"label":"拉伸","value":"stretch"},{"label":"起始","value":"flex-start"},{"label":"居中","value":"center"},{"label":"末尾","value":"flex-end"}]},
        {"key":"options.itemoptions.style.gap","label":"间距","type":"input","default":""},
        {"key":"options.itemoptions.style.flexWrap","label":"换行","type":"select","default":"nowrap","options":[{"label":"不换行","value":"nowrap"},{"label":"换行","value":"wrap"}]}
      ]
    },
    {
      "title": "外观",
      "fields": [
        {"key":"options.itemoptions.style.background","label":"背景色","type":"color","default":""},
        {"key":"options.itemoptions.style.padding","label":"内边距","type":"input","default":""},
        {"key":"options.itemoptions.style.margin","label":"外边距","type":"input","default":""},
        {"key":"options.itemoptions.style.borderRadius","label":"圆角","type":"input","default":""},
        {"key":"options.itemoptions.style.border","label":"边框","type":"input","default":""},
        {"key":"options.itemoptions.style.boxShadow","label":"阴影","type":"input","default":""},
        {"key":"options.itemoptions.style.minHeight","label":"最小高度","type":"input","default":""},
        {"key":"options.itemoptions.class","label":"自定义类名","type":"input","default":""}
      ]
    }
  ]
}' WHERE ComponentName = 'NDivContainer';

/* ==================== NImage 图片 ==================== */
UPDATE dbo.ComponentMeta SET PropertyConfigJson = N'{
  "groups": [
    {
      "title": "基础属性",
      "fields": [
        {"key":"options.comoptions.src","label":"图片地址","type":"input","default":""},
        {"key":"options.comoptions.alt","label":"替代文本","type":"input","default":""},
        {"key":"options.comoptions.fit","label":"填充模式","type":"select","default":"cover","options":[{"label":"填充","value":"fill"},{"label":"包含","value":"contain"},{"label":"覆盖","value":"cover"},{"label":"无缩放","value":"none"},{"label":"缩放下降","value":"scale-down"}]},
        {"key":"options.comoptions.round","label":"圆角","type":"switch","default":false},
        {"key":"options.comoptions.radius","label":"圆角大小","type":"input","default":""},
        {"key":"options.comoptions.width","label":"宽度","type":"input","default":"100%"},
        {"key":"options.comoptions.height","label":"高度","type":"input","default":""},
        {"key":"options.comoptions.lazyLoad","label":"懒加载","type":"switch","default":false}
      ]
    },
    {
      "title": "样式",
      "fields": ' + @styleFields + N'
    }
  ]
}' WHERE ComponentName = 'NImage';

/* ==================== NTag 标签 ==================== */
UPDATE dbo.ComponentMeta SET PropertyConfigJson = N'{
  "groups": [
    {
      "title": "基础属性",
      "fields": [
        {"key":"options.comoptions.text","label":"标签文字","type":"input","default":"标签"},
        {"key":"options.comoptions.type","label":"类型","type":"select","default":"default","options":[{"label":"主要","value":"primary"},{"label":"成功","value":"success"},{"label":"警告","value":"warning"},{"label":"危险","value":"danger"},{"label":"默认","value":"default"}]},
        {"key":"options.comoptions.plain","label":"朴素","type":"switch","default":false},
        {"key":"options.comoptions.round","label":"圆角","type":"switch","default":false},
        {"key":"options.comoptions.closeable","label":"可关闭","type":"switch","default":false},
        {"key":"options.comoptions.color","label":"自定义颜色","type":"color","default":""},
        {"key":"options.comoptions.textColor","label":"文字颜色","type":"color","default":""}
      ]
    },
    {
      "title": "样式",
      "fields": ' + @styleFields + N'
    }
  ]
}' WHERE ComponentName = 'NTag';

/* ==================== NNoticeBar 通知栏 ==================== */
UPDATE dbo.ComponentMeta SET PropertyConfigJson = N'{
  "groups": [
    {
      "title": "基础属性",
      "fields": [
        {"key":"options.comoptions.text","label":"通知内容","type":"textarea","default":""},
        {"key":"options.comoptions.type","label":"类型","type":"select","default":"default","options":[{"label":"默认","value":"default"},{"label":"主要","value":"primary"},{"label":"成功","value":"success"},{"label":"警告","value":"warning"},{"label":"危险","value":"danger"}]},
        {"key":"options.comoptions.scrollable","label":"可滚动","type":"switch","default":true},
        {"key":"options.comoptions.leftIcon","label":"左侧图标","type":"input","default":"volume"},
        {"key":"options.comoptions.color","label":"文字颜色","type":"color","default":""},
        {"key":"options.comoptions.background","label":"背景颜色","type":"color","default":""}
      ]
    },
    {
      "title": "样式",
      "fields": ' + @styleFields + N'
    }
  ]
}' WHERE ComponentName = 'NNoticeBar';

PRINT N'组件属性配置种子数据更新完成 (12 个组件)';
GO
