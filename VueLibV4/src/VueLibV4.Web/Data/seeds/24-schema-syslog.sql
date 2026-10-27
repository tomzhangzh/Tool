-- ---------------- 24. 系统日志表（旧库幂等建表；平台库用 SQLite，字段名/类型与 SQL Server/MySQL 通用） ----------------

CREATE TABLE SysLog (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    LogLevel    TEXT NOT NULL DEFAULT 'Info',
    Category    TEXT NULL,
    Message     TEXT NULL,
    Exception   TEXT NULL,
    TraceId     TEXT NULL,
    UserName    TEXT NULL,
    Path        TEXT NULL,
    Method      TEXT NULL,
    Ip          TEXT NULL,
    CreateTime  TEXT NOT NULL
);
CREATE INDEX IX_SysLog_Time ON SysLog(CreateTime);
CREATE INDEX IX_SysLog_Level ON SysLog(LogLevel);