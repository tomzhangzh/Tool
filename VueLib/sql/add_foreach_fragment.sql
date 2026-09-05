-- ============================================================
-- VueLib: ForEach 循环容器 & Fragment 透明容器 组件元数据
-- 目标表: [ComponentMeta]
-- 平台: nut (NutUI) / element (ElementPlus)
-- 说明: 需在组件 View (cshtml) 与 Controller Action 已添加的前提下执行
-- ============================================================

-- ---------- NutUI ----------

-- ForEach 循环容器
IF NOT EXISTS (SELECT 1 FROM [ComponentMeta] WHERE Name = 'DynNForEach')
INSERT INTO [ComponentMeta] (Name, Title, Category, Platform, Icon, CanAccept, Options, IsActive)
VALUES ('DynNForEach', '循环容器 ForEach', '容器', 'nut', 'N', 1,
'{
  "comoptions": {
    "dataSource": { "label": "数据源字段", "type": "text", "default": "items", "tips": "相对当前 modelname 的数组字段路径，如 users" },
    "itemVar":   { "label": "循环变量", "type": "text", "default": "item" },
    "indexVar":  { "label": "索引变量", "type": "text", "default": "index" },
    "emptyText": { "label": "空数据提示", "type": "text", "default": "" }
  }
}', 1);

-- Fragment 透明容器（无外层包装）
IF NOT EXISTS (SELECT 1 FROM [ComponentMeta] WHERE Name = 'DynNFragment')
INSERT INTO [ComponentMeta] (Name, Title, Category, Platform, Icon, CanAccept, Options, IsActive)
VALUES ('DynNFragment', '透明容器 Fragment', '容器', 'nut', 'F', 1, '{}', 1);

-- ---------- ElementPlus ----------

-- ForEach 循环容器
IF NOT EXISTS (SELECT 1 FROM [ComponentMeta] WHERE Name = 'DynEForEach')
INSERT INTO [ComponentMeta] (Name, Title, Category, Platform, Icon, CanAccept, Options, IsActive)
VALUES ('DynEForEach', '循环容器 ForEach', '容器', 'element', 'N', 1,
'{
  "comoptions": {
    "dataSource": { "label": "数据源字段", "type": "text", "default": "items", "tips": "相对当前 modelname 的数组字段路径，如 users" },
    "itemVar":   { "label": "循环变量", "type": "text", "default": "item" },
    "indexVar":  { "label": "索引变量", "type": "text", "default": "index" },
    "emptyText": { "label": "空数据提示", "type": "text", "default": "" }
  }
}', 1);

-- Fragment 透明容器（无外层包装）
IF NOT EXISTS (SELECT 1 FROM [ComponentMeta] WHERE Name = 'DynEFragment')
INSERT INTO [ComponentMeta] (Name, Title, Category, Platform, Icon, CanAccept, Options, IsActive)
VALUES ('DynEFragment', '透明容器 Fragment', '容器', 'element', 'F', 1, '{}', 1);

-- ---------- 查询验证 ----------
-- SELECT Name, Title, Category, Platform, CanAccept FROM [ComponentMeta]
-- WHERE Name IN ('DynNForEach','DynNFragment','DynEForEach','DynEFragment');
