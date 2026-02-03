-- 创建股票价格变化视图
CREATE VIEW View_StockPriceChange
AS
WITH BaseData AS (
    SELECT 
        p.Code,
        p.Weight,
        s1.TradeDate AS Date,
        s1.ClosePrice AS DateClosePrice,
        s2.ClosePrice AS BaseClosePrice
    FROM PortfolioPosition p
    JOIN StockPriceHistory s1 ON p.Code = s1.Code
    JOIN StockPriceHistory s2 ON p.Code = s2.Code
    WHERE 
        p.Weight > 0
        AND s2.TradeDate = '2026-01-01' -- 默认基准日期
),
StockChanges AS (
    SELECT 
        Date,
        Code,
        Weight,
        CASE 
            WHEN BaseClosePrice = 0 THEN 0
            ELSE (DateClosePrice - BaseClosePrice) / BaseClosePrice
        END AS ChangePercent
    FROM BaseData
)
SELECT 
    Date,
    SUM(ChangePercent * (Weight / 100)) AS TotalChangePercent
FROM StockChanges
GROUP BY Date
HAVING SUM(ChangePercent * (Weight / 100)) IS NOT NULL
GO

-- 使用示例：
-- SELECT * FROM View_StockPriceChange WHERE Date = '2026-02-01'

-- 带参数的存储过程版本（更灵活）
CREATE PROCEDURE GetStockPriceChange
    @BaseTradeDate DATE = '2026-01-01',
    @TargetDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH BaseData AS (
        SELECT 
            p.Code,
            p.Weight,
            s1.TradeDate AS Date,
            s1.ClosePrice AS DateClosePrice,
            s2.ClosePrice AS BaseClosePrice
        FROM PortfolioPosition p
        JOIN StockPriceHistory s1 ON p.Code = s1.Code
        JOIN StockPriceHistory s2 ON p.Code = s2.Code
        WHERE 
            p.Weight > 0
            AND s2.TradeDate = @BaseTradeDate
            AND (@TargetDate IS NULL OR s1.TradeDate = @TargetDate)
    ),
    StockChanges AS (
        SELECT 
            Date,
            Code,
            Weight,
            CASE 
                WHEN BaseClosePrice = 0 THEN 0
                ELSE (DateClosePrice - BaseClosePrice) / BaseClosePrice
            END AS ChangePercent
        FROM BaseData
    )
    SELECT 
        Date,
        SUM(ChangePercent * (Weight / 100)) AS TotalChangePercent
    FROM StockChanges
    GROUP BY Date
    HAVING SUM(ChangePercent * (Weight / 100)) IS NOT NULL
    ORDER BY Date;
END
GO

-- 存储过程使用示例：
-- EXEC GetStockPriceChange @BaseTradeDate = '2026-01-01', @TargetDate = '2026-02-01'
-- EXEC GetStockPriceChange @BaseTradeDate = '2026-01-01' -- 返回所有日期的变化