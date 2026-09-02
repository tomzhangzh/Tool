-- 修复 home Params 中 emoji 图标（用 NCHAR 代理对转义，避免 GBK 编码问题）
USE VueLib;
GO
DECLARE @icondoc NVARCHAR(4) = NCHAR(0xD83D) + NCHAR(0xDCC4);  -- U+1F4C4 文档
DECLARE @iconpuzz NVARCHAR(4) = NCHAR(0xD83E) + NCHAR(0xDDE9); -- U+1F9E9 拼图
DECLARE @iconclip NVARCHAR(4) = NCHAR(0xD83D) + NCHAR(0xDCCB); -- U+1F4CB 记事本
DECLARE @homeParams NVARCHAR(MAX) = N'{"banner":"欢迎使用 VueLib 示例工程","gridItems":[' +
  N'{"icon":"' + @icondoc + N'","title":"图纸管理","route":"/drawings"},' +
  N'{"icon":"' + @iconpuzz + N'","title":"部件明细","route":"/components"},' +
  N'{"icon":"' + @iconclip + N'","title":"审图记录","route":"/reviewlogs"}' +
  N']}';
UPDATE dbo.DynWebPage SET Params = @homeParams WHERE Route = '/home';
SELECT Params FROM dbo.DynWebPage WHERE Route = '/home';
GO
