-- ---------------- 6. 能力桥 RPC Demo（幂等补插：仅当 Code 不存在时插入，桌面根级图标） ----------------

INSERT INTO SysMenu (ParentId, Code, Name, Icon, Url, TargetType, IsAddToDesktop, SortNo, IsActive) SELECT NULL, 'demo-rpc', '能力桥 RPC', '🌉', '/Platform/Page/Demo/Rpc', 'FullScreen', 1, 70, 1 WHERE NOT EXISTS (SELECT 1 FROM SysMenu WHERE Code='demo-rpc');
