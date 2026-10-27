-- ---------------- 3. 系统菜单初始种子（仅当表为空时插入根菜单与示例子菜单；根菜单先插，子菜单再按 Code 解析父 Id） ----------------

-- 根菜单先插；子菜单下一条语句再按 Code 解析父 Id（同一 INSERT 内子查询读不到本语句的行）
INSERT INTO SysMenu (ParentId, Code, Name, Icon, Url, TargetType, IsAddToDesktop, SortNo, IsActive, PermissionCode) VALUES
(NULL, 'system', '系统管理', '📁', NULL, 'Iframe', 1, 1, 1, 'system'),
(NULL, 'apps',   '应用中心', '🗂️', NULL, 'Iframe', 1, 2, 1, 'apps');
INSERT INTO SysMenu (ParentId, Code, Name, Icon, Url, TargetType, IsAddToDesktop, SortNo, IsActive, PermissionCode) VALUES
((SELECT Id FROM SysMenu WHERE Code='system'), 'menu-mgmt',   '菜单管理', '🧭', '/Platform/Mgmt/SysMenuMgmt', 'FullScreen', 1, 1, 1, 'system.menu'),
((SELECT Id FROM SysMenu WHERE Code='system'), 'shortcut-mgmt','桌面快捷管理', '🖥️', '/Platform/Mgmt/DesktopShortcut', 'Iframe', 1, 2, 1, 'system.shortcut'),
((SELECT Id FROM SysMenu WHERE Code='apps'), 'designer', '页面设计器', '🎨', '/Platform/Shell/Designer', 'FullScreen', 1, 1, 1, 'apps.designer'),
((SELECT Id FROM SysMenu WHERE Code='apps'), 'student',  '学生管理', '🎓', '/Platform/Shell/PageRender?code=student-manage', 'Iframe', 1, 2, 1, 'apps.student');