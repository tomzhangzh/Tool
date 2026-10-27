-- ---------------- 32. admin 初始账号（初始密码以 SHA256 入库；历史明文在启动时自动升级，用户改过的密码不受影响） ----------------

INSERT OR IGNORE INTO SysUser (UserName, DisplayName, Password) VALUES ('admin', '管理员', @pwd);
UPDATE SysUser SET Password=@pwd WHERE UserName='admin' AND Password='admin123';