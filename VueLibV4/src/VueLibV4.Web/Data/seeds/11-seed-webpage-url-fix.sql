-- ---------------- 11. 页面实例幂等自愈：演示页 Url 回填自身 Id + setting-mgmt 菜单 Url 重算 ----------------

UPDATE DynWebPage SET Url='/Platform/Shell/Runtime?id=' || Id
WHERE Code IN ('page-setting-mgmt','dyn-webpage-list') AND (Url IS NULL OR Url='/Platform/Shell/Runtime?id=');

UPDATE SysMenu SET Url='/Platform/Shell/Runtime?id=' || (SELECT Id FROM DynWebPage WHERE Code='page-setting-mgmt')
WHERE Code='setting-mgmt' AND EXISTS (SELECT 1 FROM DynWebPage WHERE Code='page-setting-mgmt');