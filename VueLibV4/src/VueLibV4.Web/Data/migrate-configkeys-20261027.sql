-- ============================================================================
-- 2026-10-27 配置键统一为「全小写无分隔」
-- ----------------------------------------------------------------------------
-- 背景：options 下框架定义的配置组历史上存在驼峰/全小写双轨（comOptions 与
--       comoptions、labelOptions 与 labeloptions），旧数据靠 _BaseLayout 运行时
--       原地迁移兜底。自本次起运行时只认全小写，兜底代码已删除，故存量库必须先
--       执行本脚本，否则旧配置会读不到。
--
-- 契约（DynNodeViewOptions / _BaseLayout / 设计器统一遵守）：
--   comoptions  labeloptions  itemoptions  wrapperoptions  compassthrough  evalcondition
--   labeloptions 下：label  show  required  labelposition  labelwidth
--
-- 说明：
--   1) 幂等——执行多次结果一致。
--   2) 只改「框架定义的键」，不动 comoptions 里 Element 组件的原生属性
--      （如 DynForm 穿透配置里的 labelWidth/labelPosition 属于 Element 属性名，
--      必须保持驼峰，因此 DynTemplate.ConfigJson 不做 labelWidth 全局替换）。
--   3) 非空 comInnerInfo 已不再被任何代码读取（历史上只承载 labelWidth，且同节点
--      labeloptions.labelwidth 恒优先），保留为惰性死键，不影响渲染；这里只清理空对象。
--   4) 执行前请备份 Platform.db。
-- ============================================================================

BEGIN;

-- ---- 1. ComponentMeta.PropertyConfigJson：设计器属性表单里的配置点路径 ----
UPDATE ComponentMeta SET PropertyConfigJson = replace(PropertyConfigJson, 'options.comInnerInfo.size', 'options.comoptions.size')
 WHERE PropertyConfigJson LIKE '%options.comInnerInfo.size%';
UPDATE ComponentMeta SET PropertyConfigJson = replace(PropertyConfigJson, 'options.comInnerInfo.labelPosition', 'options.labeloptions.labelposition')
 WHERE PropertyConfigJson LIKE '%options.comInnerInfo.labelPosition%';
UPDATE ComponentMeta SET PropertyConfigJson = replace(PropertyConfigJson, 'options.comInnerInfo.labelWidth', 'options.labeloptions.labelwidth')
 WHERE PropertyConfigJson LIKE '%options.comInnerInfo.labelWidth%';
UPDATE ComponentMeta SET PropertyConfigJson = replace(PropertyConfigJson, 'options.comInnerInfo.comPassThrough', 'options.compassthrough')
 WHERE PropertyConfigJson LIKE '%options.comInnerInfo.comPassThrough%';

-- ---- 2. 驼峰配置组 → 全小写（全部元数据列） ----
UPDATE ComponentMeta SET PropertyConfigJson = replace(replace(replace(replace(replace(replace(PropertyConfigJson,
        '"comOptions"', '"comoptions"'), '"labelOptions"', '"labeloptions"'),
        '"itemOptions"', '"itemoptions"'), '"wrapperOptions"', '"wrapperoptions"'),
        '"comPassThrough"', '"compassthrough"'), '"evalCondition"', '"evalcondition"')
 WHERE PropertyConfigJson LIKE '%comOptions%' OR PropertyConfigJson LIKE '%labelOptions%'
    OR PropertyConfigJson LIKE '%itemOptions%' OR PropertyConfigJson LIKE '%wrapperOptions%'
    OR PropertyConfigJson LIKE '%comPassThrough%' OR PropertyConfigJson LIKE '%evalCondition%';

UPDATE ComponentMeta SET DefaultConfigJson = replace(replace(replace(replace(replace(replace(DefaultConfigJson,
        '"comOptions"', '"comoptions"'), '"labelOptions"', '"labeloptions"'),
        '"itemOptions"', '"itemoptions"'), '"wrapperOptions"', '"wrapperoptions"'),
        '"comPassThrough"', '"compassthrough"'), '"evalCondition"', '"evalcondition"')
 WHERE DefaultConfigJson LIKE '%comOptions%' OR DefaultConfigJson LIKE '%labelOptions%'
    OR DefaultConfigJson LIKE '%itemOptions%' OR DefaultConfigJson LIKE '%wrapperOptions%'
    OR DefaultConfigJson LIKE '%comPassThrough%' OR DefaultConfigJson LIKE '%evalCondition%';

UPDATE DynTemplate SET ConfigJson = replace(replace(replace(replace(replace(replace(ConfigJson,
        '"comOptions"', '"comoptions"'), '"labelOptions"', '"labeloptions"'),
        '"itemOptions"', '"itemoptions"'), '"wrapperOptions"', '"wrapperoptions"'),
        '"comPassThrough"', '"compassthrough"'), '"evalCondition"', '"evalcondition"')
 WHERE ConfigJson LIKE '%comOptions%' OR ConfigJson LIKE '%labelOptions%'
    OR ConfigJson LIKE '%itemOptions%' OR ConfigJson LIKE '%wrapperOptions%'
    OR ConfigJson LIKE '%comPassThrough%' OR ConfigJson LIKE '%evalCondition%';

UPDATE DynTemplate SET DefaultJson = replace(replace(replace(replace(replace(replace(DefaultJson,
        '"comOptions"', '"comoptions"'), '"labelOptions"', '"labeloptions"'),
        '"itemOptions"', '"itemoptions"'), '"wrapperOptions"', '"wrapperoptions"'),
        '"comPassThrough"', '"compassthrough"'), '"evalCondition"', '"evalcondition"')
 WHERE DefaultJson LIKE '%comOptions%' OR DefaultJson LIKE '%labelOptions%'
    OR DefaultJson LIKE '%itemOptions%' OR DefaultJson LIKE '%wrapperOptions%'
    OR DefaultJson LIKE '%comPassThrough%' OR DefaultJson LIKE '%evalCondition%';

UPDATE PageSetting SET ConfigJson = replace(replace(replace(replace(replace(replace(ConfigJson,
        '"comOptions"', '"comoptions"'), '"labelOptions"', '"labeloptions"'),
        '"itemOptions"', '"itemoptions"'), '"wrapperOptions"', '"wrapperoptions"'),
        '"comPassThrough"', '"compassthrough"'), '"evalCondition"', '"evalcondition"')
 WHERE ConfigJson LIKE '%comOptions%' OR ConfigJson LIKE '%labelOptions%'
    OR ConfigJson LIKE '%itemOptions%' OR ConfigJson LIKE '%wrapperOptions%'
    OR ConfigJson LIKE '%comPassThrough%' OR ConfigJson LIKE '%evalCondition%';

UPDATE PageSetting SET DefaultJson = replace(replace(replace(replace(replace(replace(DefaultJson,
        '"comOptions"', '"comoptions"'), '"labelOptions"', '"labeloptions"'),
        '"itemOptions"', '"itemoptions"'), '"wrapperOptions"', '"wrapperoptions"'),
        '"comPassThrough"', '"compassthrough"'), '"evalCondition"', '"evalcondition"')
 WHERE DefaultJson LIKE '%comOptions%' OR DefaultJson LIKE '%labelOptions%'
    OR DefaultJson LIKE '%itemOptions%' OR DefaultJson LIKE '%wrapperOptions%'
    OR DefaultJson LIKE '%comPassThrough%' OR DefaultJson LIKE '%evalCondition%';

UPDATE DynWebPage SET ParamsJson = replace(replace(replace(replace(replace(replace(ParamsJson,
        '"comOptions"', '"comoptions"'), '"labelOptions"', '"labeloptions"'),
        '"itemOptions"', '"itemoptions"'), '"wrapperOptions"', '"wrapperoptions"'),
        '"comPassThrough"', '"compassthrough"'), '"evalCondition"', '"evalcondition"')
 WHERE ParamsJson LIKE '%comOptions%' OR ParamsJson LIKE '%labelOptions%'
    OR ParamsJson LIKE '%itemOptions%' OR ParamsJson LIKE '%wrapperOptions%'
    OR ParamsJson LIKE '%comPassThrough%' OR ParamsJson LIKE '%evalCondition%';

UPDATE DynWebPage SET SpecJson = replace(replace(replace(replace(replace(replace(SpecJson,
        '"comOptions"', '"comoptions"'), '"labelOptions"', '"labeloptions"'),
        '"itemOptions"', '"itemoptions"'), '"wrapperOptions"', '"wrapperoptions"'),
        '"comPassThrough"', '"compassthrough"'), '"evalCondition"', '"evalcondition"')
 WHERE SpecJson LIKE '%comOptions%' OR SpecJson LIKE '%labelOptions%'
    OR SpecJson LIKE '%itemOptions%' OR SpecJson LIKE '%wrapperOptions%'
    OR SpecJson LIKE '%comPassThrough%' OR SpecJson LIKE '%evalCondition%';

UPDATE DynCom SET ConfigJson = replace(replace(replace(replace(replace(replace(ConfigJson,
        '"comOptions"', '"comoptions"'), '"labelOptions"', '"labeloptions"'),
        '"itemOptions"', '"itemoptions"'), '"wrapperOptions"', '"wrapperoptions"'),
        '"comPassThrough"', '"compassthrough"'), '"evalCondition"', '"evalcondition"')
 WHERE ConfigJson LIKE '%comOptions%' OR ConfigJson LIKE '%labelOptions%'
    OR ConfigJson LIKE '%itemOptions%' OR ConfigJson LIKE '%wrapperOptions%'
    OR ConfigJson LIKE '%comPassThrough%' OR ConfigJson LIKE '%evalCondition%';

UPDATE DynBlock SET ParamConfigJson = replace(replace(replace(replace(replace(replace(ParamConfigJson,
        '"comOptions"', '"comoptions"'), '"labelOptions"', '"labeloptions"'),
        '"itemOptions"', '"itemoptions"'), '"wrapperOptions"', '"wrapperoptions"'),
        '"comPassThrough"', '"compassthrough"'), '"evalCondition"', '"evalcondition"')
 WHERE ParamConfigJson LIKE '%comOptions%' OR ParamConfigJson LIKE '%labelOptions%'
    OR ParamConfigJson LIKE '%itemOptions%' OR ParamConfigJson LIKE '%wrapperOptions%'
    OR ParamConfigJson LIKE '%comPassThrough%' OR ParamConfigJson LIKE '%evalCondition%';

UPDATE DynBlock SET ParamDefaultJson = replace(replace(replace(replace(replace(replace(ParamDefaultJson,
        '"comOptions"', '"comoptions"'), '"labelOptions"', '"labeloptions"'),
        '"itemOptions"', '"itemoptions"'), '"wrapperOptions"', '"wrapperoptions"'),
        '"comPassThrough"', '"compassthrough"'), '"evalCondition"', '"evalcondition"')
 WHERE ParamDefaultJson LIKE '%comOptions%' OR ParamDefaultJson LIKE '%labelOptions%'
    OR ParamDefaultJson LIKE '%itemOptions%' OR ParamDefaultJson LIKE '%wrapperOptions%'
    OR ParamDefaultJson LIKE '%comPassThrough%' OR ParamDefaultJson LIKE '%evalCondition%';

-- ---- 3. labeloptions 内驼峰子键 → 全小写（实例配置，不含 DynTemplate.ConfigJson 的 Element 穿透） ----
UPDATE PageSetting SET ConfigJson = replace(replace(ConfigJson, '"labelWidth"', '"labelwidth"'), '"labelPosition"', '"labelposition"')
 WHERE ConfigJson LIKE '%"labelWidth"%' OR ConfigJson LIKE '%"labelPosition"%';
UPDATE PageSetting SET DefaultJson = replace(replace(DefaultJson, '"labelWidth"', '"labelwidth"'), '"labelPosition"', '"labelposition"')
 WHERE DefaultJson LIKE '%"labelWidth"%' OR DefaultJson LIKE '%"labelPosition"%';
UPDATE DynTemplate SET DefaultJson = replace(replace(DefaultJson, '"labelWidth"', '"labelwidth"'), '"labelPosition"', '"labelposition"')
 WHERE DefaultJson LIKE '%"labelWidth"%' OR DefaultJson LIKE '%"labelPosition"%';
UPDATE DynWebPage SET ParamsJson = replace(replace(ParamsJson, '"labelWidth"', '"labelwidth"'), '"labelPosition"', '"labelposition"')
 WHERE ParamsJson LIKE '%"labelWidth"%' OR ParamsJson LIKE '%"labelPosition"%';
UPDATE DynWebPage SET SpecJson = replace(replace(SpecJson, '"labelWidth"', '"labelwidth"'), '"labelPosition"', '"labelposition"')
 WHERE SpecJson LIKE '%"labelWidth"%' OR SpecJson LIKE '%"labelPosition"%';
UPDATE DynCom SET ConfigJson = replace(replace(ConfigJson, '"labelWidth"', '"labelwidth"'), '"labelPosition"', '"labelposition"')
 WHERE ConfigJson LIKE '%"labelWidth"%' OR ConfigJson LIKE '%"labelPosition"%';
UPDATE DynBlock SET ParamConfigJson = replace(replace(ParamConfigJson, '"labelWidth"', '"labelwidth"'), '"labelPosition"', '"labelposition"')
 WHERE ParamConfigJson LIKE '%"labelWidth"%' OR ParamConfigJson LIKE '%"labelPosition"%';
UPDATE DynBlock SET ParamDefaultJson = replace(replace(ParamDefaultJson, '"labelWidth"', '"labelwidth"'), '"labelPosition"', '"labelposition"')
 WHERE ParamDefaultJson LIKE '%"labelWidth"%' OR ParamDefaultJson LIKE '%"labelPosition"%';

-- ---- 4. 清理空 comInnerInfo 死键（非空者已无消费方，保留不影响渲染） ----
UPDATE PageSetting SET ConfigJson = replace(replace(ConfigJson, ', "comInnerInfo": {}', ''), ',"comInnerInfo":{}', '') WHERE ConfigJson LIKE '%"comInnerInfo": {}%' OR ConfigJson LIKE '%"comInnerInfo":{}%';
UPDATE PageSetting SET DefaultJson = replace(replace(DefaultJson, ', "comInnerInfo": {}', ''), ',"comInnerInfo":{}', '') WHERE DefaultJson LIKE '%"comInnerInfo": {}%' OR DefaultJson LIKE '%"comInnerInfo":{}%';
UPDATE DynTemplate SET ConfigJson = replace(replace(ConfigJson, ', "comInnerInfo": {}', ''), ',"comInnerInfo":{}', '') WHERE ConfigJson LIKE '%"comInnerInfo": {}%' OR ConfigJson LIKE '%"comInnerInfo":{}%';
UPDATE DynTemplate SET DefaultJson = replace(replace(DefaultJson, ', "comInnerInfo": {}', ''), ',"comInnerInfo":{}', '') WHERE DefaultJson LIKE '%"comInnerInfo": {}%' OR DefaultJson LIKE '%"comInnerInfo":{}%';
UPDATE ComponentMeta SET DefaultConfigJson = replace(replace(DefaultConfigJson, ', "comInnerInfo": {}', ''), ',"comInnerInfo":{}', '') WHERE DefaultConfigJson LIKE '%"comInnerInfo": {}%' OR DefaultConfigJson LIKE '%"comInnerInfo":{}%';
UPDATE DynCom SET ConfigJson = replace(replace(ConfigJson, ', "comInnerInfo": {}', ''), ',"comInnerInfo":{}', '') WHERE ConfigJson LIKE '%"comInnerInfo": {}%' OR ConfigJson LIKE '%"comInnerInfo":{}%';

COMMIT;

-- ============================================================================
-- 执行后自检（应返回 0 行；仅 DynTemplate.ConfigJson 的 Element 穿透白名单例外）：
--   SELECT 'PageSetting', Id FROM PageSetting
--    WHERE ConfigJson LIKE '%comOptions%' OR ConfigJson LIKE '%labelOptions%'
--       OR ConfigJson LIKE '%"labelWidth"%' OR ConfigJson LIKE '%"labelPosition"%'
--       OR ConfigJson LIKE '%"evalCondition"%';
-- ============================================================================
