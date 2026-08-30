USE [VueLib];
GO







IF COL_LENGTH('dbo.PageSetting','CanvasWidth') IS NULL
    ALTER TABLE dbo.PageSetting ADD CanvasWidth INT NULL;
GO

IF COL_LENGTH('dbo.PageSetting','CanvasHeight') IS NULL
    ALTER TABLE dbo.PageSetting ADD CanvasHeight INT NULL;
GO


UPDATE dbo.PageSetting
SET CanvasWidth = ISNULL(CanvasWidth, CASE WHEN Platform = N'desktop' THEN 1366 ELSE 375 END),
    CanvasHeight = ISNULL(CanvasHeight, CASE WHEN Platform = N'desktop' THEN 768 ELSE 667 END),
    Platform = ISNULL(Platform, N'mobile')
WHERE IsEnabled = 1;
GO

PRINT 'PageSetting ????????';
GO
