-- 创建StockPriceCurrent表的SQLite3语句
CREATE TABLE IF NOT EXISTS StockPriceCurrent (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Code TEXT,
    TradeDate DATETIME NOT NULL,
    CurrentPrice NUMERIC,
    CurrentChange TEXT,
    CurrentPercent TEXT,
    RealPrice NUMERIC,
    RealChange TEXT,
    RealPercent TEXT,
    RealTime TEXT,
    Type TEXT,
    ChangePercent NUMERIC,
    UpdateTime DATETIME NOT NULL
);

-- 创建索引以提高查询性能
CREATE INDEX IF NOT EXISTS IX_StockPriceCurrent_Code ON StockPriceCurrent(Code);
CREATE INDEX IF NOT EXISTS IX_StockPriceCurrent_TradeDate ON StockPriceCurrent(TradeDate);
CREATE INDEX IF NOT EXISTS IX_StockPriceCurrent_Code_TradeDate ON StockPriceCurrent(Code, TradeDate);