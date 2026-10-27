-- 路由迁移脚本：2026-10-27 控制器拆分（Page / DynWebPage / Tool）
-- 适用：已存在数据的 Platform.db（SQLite）
-- 执行一次即可；脚本幂等，重复执行不会产生副作用。
-- 注意：新代码不再提供旧 URL 别名，未执行本脚本的存量菜单会 404。

CREATE TEMP TABLE IF NOT EXISTS __route_map(old TEXT PRIMARY KEY, new TEXT NOT NULL);
DELETE FROM __route_map;

INSERT INTO __route_map(old, new) VALUES
('/Platform/Page/Desktop',           '/Platform/Shell/Desktop'),
('/Platform/Page/Designer',          '/Platform/Shell/Designer'),
('/Platform/Page/WebPageRender',     '/Platform/Shell/PageRender'),
('/Platform/Page/DynWebPage',        '/Platform/Shell/Runtime'),
('/Platform/Page/DetailModalV2',     '/Platform/Detail/ModalV2'),
('/Platform/Page/DetailModal',       '/Platform/Detail/Modal'),
('/Platform/Page/DynDetail',         '/Platform/Detail/Form'),
('/Platform/Page/LoadBlock',         '/Platform/Block/Load'),
('/Platform/Page/ParamsPanel',       '/Platform/Block/ParamsPanel'),
('/Platform/Page/SaveParams',        '/Platform/Block/SaveParams'),
('/Platform/Page/InspectorData',     '/Platform/Block/Inspector'),
('/Platform/Page/PermissionGrid',    '/Platform/Permission/Grid'),
('/Platform/Page/EChartData',        '/Platform/Chart/Data'),
('/Platform/Page/ComponentCheck',    '/Platform/Tools/ComponentCheck'),
('/Platform/Page/PageGraphDemo',     '/Platform/Tools/PageGraph'),
('/Platform/Page/PageGraphData',     '/Platform/Tools/PageGraphData'),
('/Platform/Page/DbErGraph',         '/Platform/Tools/DbErGraph'),
('/Platform/Page/MdViewer',          '/Platform/Tools/MdViewer');

-- 1) 系统菜单
UPDATE SysMenu
SET Url = (SELECT new || substr(Url, length(old) + 1) FROM __route_map WHERE Url LIKE old || '%')
WHERE EXISTS (SELECT 1 FROM __route_map WHERE SysMenu.Url LIKE old || '%');

-- 2) 页面实例运行地址
UPDATE DynWebPage
SET Url = (SELECT new || substr(Url, length(old) + 1) FROM __route_map WHERE Url LIKE old || '%')
WHERE Url IS NOT NULL
  AND EXISTS (SELECT 1 FROM __route_map WHERE DynWebPage.Url LIKE old || '%');

-- 3) 桌面快捷方式（若表存在）
UPDATE DesktopShortcut
SET Url = (SELECT new || substr(Url, length(old) + 1) FROM __route_map WHERE Url LIKE old || '%')
WHERE Url IS NOT NULL
  AND EXISTS (SELECT 1 FROM __route_map WHERE DesktopShortcut.Url LIKE old || '%');

DROP TABLE __route_map;
