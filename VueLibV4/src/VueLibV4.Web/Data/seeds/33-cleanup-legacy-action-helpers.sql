-- ---------------- 33. 清理历史坏占位 DB 动作（幂等，旧库迁移；platform.sql 中已同步去除） ----------------
-- reload='ctx.reload();' 在 ctx 上从无实现，且 DB 动作加载时覆盖同名内置 reload
-- （内置 reload：BlockApp 句柄优先 send('reload')，否则片段刷新），导致管道 reload 必报错。
-- 仅精确删除原文占位行；用户自定义的同名动作保留。

DELETE FROM DynActionHelper
 WHERE Code='reload' AND ActionType='script' AND TRIM(COALESCE(Script,''))='ctx.reload();';
