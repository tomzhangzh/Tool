-- ---------------- 5. 解决方案页面（幂等补插：每次启动检查） ----------------

INSERT INTO SysMenu (ParentId, Code, Name, Icon, Url, TargetType, IsAddToDesktop, SortNo, IsActive) SELECT (SELECT Id FROM SysMenu WHERE Code='platform-engine'), 'solution-page', '解决方案', '💡', '/Platform/Shell/Runtime?id=26', 'Iframe', 1, 4, 1 WHERE NOT EXISTS (SELECT 1 FROM SysMenu WHERE Code='solution-page');
