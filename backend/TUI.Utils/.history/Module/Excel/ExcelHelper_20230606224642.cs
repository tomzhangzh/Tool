// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Data;

namespace TUI.Utils;

public static class ExcelHelper
{
    /*
      XLS  转 XLSX 参考：https://www.cnblogs.com/HelloQLQ/p/16166140.html
     */

    /// <summary>
    /// XLS  转 XLSX
    /// </summary>
    /// <param name="xlsPath">xls 文件路径</param>
    /// <returns></returns>
    public static string XlsToXlsx(string xlsPath)
    {
       return xlsPath.XlsToXlsx();
    }

    ///<summary>
    /// 将excel导入到 List<IDictionary<string, object>
    /// </summary>
    /// <param name="filePath">excel路径</param>
    /// <param name="isFirstRowColumnName">第一行是否是列名,默认true</param>
    /// <returns>返回datatable</returns>
    public static DataTable ExcelToDataTable(string filePath, bool isFirstRowColumnName)
    {
        return filePath.ReadExcelToDataTable(isFirstRowColumnName);
    }
    ///<summary>
    /// 获取excel列名
    /// </summary>
    /// <param name="filePath">excel路径</param>
    /// <returns>返回datatable</returns>
    public static List<string> ReadExcelColumns(string filePath)
    {
        return filePath.ReadExcelColumns();
    }

    
}