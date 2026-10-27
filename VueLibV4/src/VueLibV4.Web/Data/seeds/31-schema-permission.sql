-- ---------------- 31. 轻量 RBAC 权限表（旧库幂等建表）：角色 / 用户角色 / 资源目录树 / 角色授权 ----------------

CREATE TABLE IF NOT EXISTS SysUser (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    UserName    TEXT NOT NULL UNIQUE,
    DisplayName TEXT NULL,
    Password    TEXT NULL,
    Email       TEXT NULL,
    IsActive    INTEGER NOT NULL DEFAULT 1,
    CreateTime  TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS SysRole (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT NOT NULL,
    Remark      TEXT NULL,
    ProjectId   INTEGER NOT NULL DEFAULT 0,
    IsActive    INTEGER NOT NULL DEFAULT 1,
    CreateTime  TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);
CREATE TABLE IF NOT EXISTS SysUserRole (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    UserName    TEXT NOT NULL,
    RoleId      INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS SysResource (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    ParentId    INTEGER NOT NULL DEFAULT 0,
    Key         TEXT NOT NULL,
    Name        TEXT NOT NULL,
    Type        TEXT NOT NULL DEFAULT 'Operation',
    ProjectId   INTEGER NOT NULL DEFAULT 0,
    HasRead     INTEGER NOT NULL DEFAULT 1,
    HasEdit     INTEGER NOT NULL DEFAULT 1,
    HasDelete   INTEGER NOT NULL DEFAULT 0,
    TableNames  TEXT NULL,
    SortNo      INTEGER NOT NULL DEFAULT 0,
    IsActive    INTEGER NOT NULL DEFAULT 1
);
CREATE TABLE IF NOT EXISTS SysResourcePermission (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    RoleId      INTEGER NOT NULL,
    ResourceId  INTEGER NOT NULL,
    Read        INTEGER NOT NULL DEFAULT 0,
    Edit        INTEGER NOT NULL DEFAULT 0,
    CanDelete   INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS IX_SysUserRole_User ON SysUserRole(UserName);
CREATE INDEX IF NOT EXISTS IX_SysResourcePermission_Role ON SysResourcePermission(RoleId);
CREATE INDEX IF NOT EXISTS IX_SysResource_Parent ON SysResource(ParentId);