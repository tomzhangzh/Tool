// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using AngleSharp.Dom;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.Util.Collections;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Abc.Utils;
public static class ExcelExtension
{
    ///<summary>
    /// 将excel导入到 List<IDictionary<string, object>
    /// </summary>
    /// <param name="filePath">excel路径</param>
    /// <param name="isFirstRowColumnName">第一行是否是列名,默认true</param>
    /// <returns>返回datatable</returns>
    public static DataTable ReadExcelToDataTable(this string filePath, bool isFirstRowColumnName = true)
    {
        DataTable dataTable = null;
        int startRow = 0;
        try
        {
            // using (fs = File.OpenRead(filePath))
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            {
                IWorkbook workbook = null;
                // 2007版本
                if (filePath.IndexOf(".xlsx") > 0)
                    workbook = new XSSFWorkbook(fs);
                // 2003版本
                else if (filePath.IndexOf(".xls") > 0)
                    workbook = new HSSFWorkbook(fs);

                if (workbook != null)
                {
                    var sheet = workbook.GetSheetAt(0); //读取第一个sheet，当然也可以循环读取每个sheet
                    dataTable = new DataTable();
                    if (sheet != null)
                    {
                        int rowCount = sheet.LastRowNum; //总行数
                        if (rowCount > 0)
                        {
                            IRow firstRow = sheet.GetRow(0); //第一行
                            int cellCount = firstRow.LastCellNum; //列数

                            #region 构建datatable的列

                            if (isFirstRowColumnName) //第一行是否是列名
                            {
                                dataTable.Columns.Add(new DataColumn("表格索引"));//从0开始计数

                                for (int i = firstRow.FirstCellNum; i < cellCount; ++i)
                                {
                                    var cell = firstRow.GetCell(i);
                                    if (cell != null)
                                    {
                                        if (cell.StringCellValue != null)
                                        {
                                            var column = new DataColumn(cell.StringCellValue);
                                            dataTable.Columns.Add(column);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                dataTable.Columns.Add(new DataColumn("表格索引"));//从0开始计数
                                for (int i = firstRow.FirstCellNum; i < cellCount; ++i)
                                {
                                    var column = new DataColumn("column" + (i + 1));
                                    dataTable.Columns.Add(column);
                                }
                            }

                            #endregion 构建datatable的列

                            // bool IsEmptyRow = true; //默认表示空行

                            if (isFirstRowColumnName) //第一行是否是列名
                                startRow = 1; //如果第一行是列名，则从第二行开始读取
                            //填充行
                            for (int rowIndex = startRow; rowIndex <= rowCount; ++rowIndex)
                            {
                                //IsEmptyRow = true;
                                var row = sheet.GetRow(rowIndex);
                                if (row == null) continue;//获取的行为null 跳过
                                if (row.FirstCellNum < 0) continue;//
                                                                   //datatable 新增一行
                                var dataRow = dataTable.NewRow();

                                dataRow[0] = rowIndex;
                                for (int columnIndex = row.FirstCellNum; columnIndex < cellCount; ++columnIndex)
                                {
                                    var columnIndex2 = columnIndex + 1;//这里加1，是因为，默认第一列是表格索引列，后来定位是excle中哪一行的
                                    var cell = row.GetCell(columnIndex);
                                    if (cell == null)
                                    {
                                        dataRow[columnIndex2] = "";
                                    }
                                    else
                                    {
                                        //获取cell值
                                        var excelCellValueReturn = GetExcelCellValue(sheet, cell.CellType, row, cell);
                                        if (excelCellValueReturn.IsSuccess)
                                        {
                                            dataRow[columnIndex2] = excelCellValueReturn.ObjValue;
                                        }
                                        else if (string.IsNullOrEmpty(excelCellValueReturn.Error))
                                        {
                                            //获取值出现错误的情况
                                            //if (properties.Any(o => o.Name == "Error"))
                                            //{
                                            //    var p = properties.First(o => o.Name == "Error");
                                            //    model.SetValue(p, rowIndex);
                                            //}
                                        }
                                    }

                                }
                                dataTable.Rows.Add(dataRow);
                            }
                        }
                    }
                }
            }
            //return dataTable;
        }
        catch (Exception ex)
        {
            LogEx.Error(ex, "Excel\\Excel转DataTable");
        }

        return dataTable;
    }


    ///<summary>
    /// 将excel导入到 List<T>
    /// </summary>
    /// <param name="filePath">excel路径</param>
    /// <returns>返回List<T></returns>
    public static List<T> ReadExcelToList<T>(this string filePath) where T : new()
    {
        //DataTable dataTable = null;
        var datas = new List<T>();
        int startRow = 0;
        try
        {
            // using (fs = File.OpenRead(filePath))
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            {
                IWorkbook workbook = null;
                // 2007版本
                if (filePath.IndexOf(".xlsx") > 0)
                    workbook = new XSSFWorkbook(fs);
                // 2003版本
                else if (filePath.IndexOf(".xls") > 0)
                    workbook = new HSSFWorkbook(fs);

                if (workbook != null)
                {
                    var sheet = workbook.GetSheetAt(0); //读取第一个sheet，当然也可以循环读取每个sheet
                    //dataTable = new DataTable();
                    if (sheet != null)
                    {
                        int rowCount = sheet.LastRowNum; //总行数
                        //数据行数为0，直接返回
                        if (rowCount <= 0) return datas;

                        IRow firstRow = sheet.GetRow(0); //第一行
                        int cellCount = firstRow.LastCellNum; //列数

                        #region 读取表头
                        Dictionary<int, string> excelHeads = new Dictionary<int, string>();
                        for (int i = 0; i < cellCount; ++i)
                        {
                            var cell = firstRow.GetCell(i);
                            if (cell != null)
                            {
                                excelHeads.Add(0, cell.StringCellValue);
                            }

                        }
                        //处理自定义了字段映射到excel列名的情况【Key：对象字段名称，value:映射的excel列名称】
                        Dictionary<string, string> customFieldNames;
                        {
                            var model = new T();
                            customFieldNames = model.GetExcelColumnFieldAlias(excelHeads.Select(o => o.Value));
                        }

                        #endregion 读取表头


                        // bool IsEmptyRow = true; //默认表示空行

                        //if (isFirstRowColumnName) //第一行是否是列名
                        //    startRow = 1; //如果第一行是列名，则从第二行开始读取
                        //填充行
                        for (int rowIndex = startRow; rowIndex <= rowCount; ++rowIndex)
                        {
                            //IsEmptyRow = true;
                            var row = sheet.GetRow(rowIndex);
                            if (row == null) continue;//获取的行为null 跳过
                            if (row.FirstCellNum < 0) continue;//

                            //foreach (var headItem in excelHeads)
                            //{
                            var model = new T();
                            var properties = new T().GetType().GetProperties();
                            try
                            {
                                //设置索引字段的行数
                                if (properties.Any(o => o.Name == "RowIndex"))
                                {
                                    var p = properties.First(o => o.Name == "RowIndex");
                                    model.SetValue(p, rowIndex);
                                }
                            }
                            catch (Exception ex)
                            {
                                //导入错误消息
                                if (properties.Any(o => o.Name == "Error"))
                                {
                                    var p = properties.First(o => o.Name == "Error");
                                    model.SetValue(p, rowIndex);
                                }
                                datas.Add(model);
                                continue;
                            }

                            for (int columnIndex = 0; columnIndex < cellCount; ++columnIndex)
                            {
                                var cell = row.GetCell(columnIndex);
                                var excelColumnName = "";//excel列名
                                if (excelHeads.ContainsKey(columnIndex))
                                {
                                    excelColumnName = excelHeads[columnIndex];
                                }

                                //对象的字段名
                                var field = customFieldNames.FirstOrDefault(o => o.Value == excelColumnName);

                                //获取字段的属性
                                var propertyInfo = properties.FirstOrDefault(o => o.Name == field.Key);
                                if (propertyInfo == null)
                                {
                                    //没有找到此字段
                                    continue;
                                }

                                if (cell == null)
                                {
                                    //单元格是空的，字段默认就行，不设置
                                }
                                else
                                {
                                    //获取cell值
                                    var excelCellValueReturn = GetExcelCellValue(sheet, cell.CellType, row, cell);
                                    if (excelCellValueReturn.IsSuccess)
                                    {
                                        model.SetValue(propertyInfo, excelCellValueReturn.ObjValue);
                                    }
                                    //如果没有获取成功，且错误消息不为空
                                    else if (!string.IsNullOrEmpty(excelCellValueReturn.Error))
                                    {
                                        if (properties.Any(o => o.Name == "Error"))
                                        {
                                            var p = properties.First(o => o.Name == "Error");
                                            model.SetValue(p, rowIndex);
                                        }
                                    }

                                }

                            }

                            datas.Add(model);
                        }

                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogEx.Error(ex, "Excel\\ReadExcelToList");
        }



        return datas;
    }
    static void SetValue<T>(this T obj, PropertyInfo propertyInfo, object? value)
    {
        //var pval = data.Rows[i][j]?.ToString();
        if (value != null)
        {
            try
            {
                // We need to check whether the property is NULLABLE
                //检测该字段是否为可空
                if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    propertyInfo.SetValue(obj, Convert.ChangeType(value, propertyInfo.PropertyType.GetGenericArguments()[0]), null);
                }
                else
                {
                    propertyInfo.SetValue(obj, Convert.ChangeType(value, propertyInfo.PropertyType), null);
                }
            }
            catch (Exception x)
            {

            }
        }
    }


    ///<summary>
    /// 将excel导入到datatable
    /// </summary>
    /// <param name="filePath">excel路径</param>
    /// <param name="isFirstRowColumnName">第一行是否是列名,默认true</param>
    /// <returns>返回 List<IDictionary<string, object></returns>
    public static List<IDictionary<string, object>> RedExcelToList(this string filePath, bool isFirstRowColumnName = true)
    {
        var dt = filePath.ReadExcelToDataTable(isFirstRowColumnName);
        return dt.ToListDic();
    }

    ///<summary>
    /// 获取excel列名
    /// </summary>
    /// <param name="filePath">excel路径</param>
    /// <returns>返回datatable</returns>
    public static List<string> ReadExcelColumns(this string filePath)
    {
        var columns = new List<string>();

        IWorkbook workbook = null;
        try
        {
            // using (fs = File.OpenRead(filePath))
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            {
                // 2007版本
                if (filePath.IndexOf(".xlsx") > 0)
                    workbook = new XSSFWorkbook(fs);
                // 2003版本
                else if (filePath.IndexOf(".xls") > 0)
                    workbook = new HSSFWorkbook(fs);

                if (workbook != null)
                {
                    var sheet = workbook.GetSheetAt(0); //读取第一个sheet，当然也可以循环读取每个sheet
                    if (sheet != null)
                    {
                        int rowCount = sheet.LastRowNum; //总行数
                        if (rowCount > 0)
                        {
                            IRow firstRow = sheet.GetRow(0); //第一行
                            int cellCount = firstRow.LastCellNum; //列数

                            //构建datatable的列

                            //从0开始计数
                            for (int i = firstRow.FirstCellNum; i < cellCount; ++i)
                            {
                                var cell = firstRow.GetCell(i);
                                if (!string.IsNullOrWhiteSpace(cell?.StringCellValue))
                                {
                                    columns.Add(cell?.StringCellValue.Trim());
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogEx.Error(ex, "Excel\\Excel转DataTable");
        }
        return columns;
    }

    /// <summary>
    /// 获取Excel单元格数据
    /// </summary>
    /// <param name="sheet"></param>
    /// <param name="cellType">单元格数据类型</param>
    /// <param name="row">行</param>
    /// <param name="cell">单元格</param>
    /// <returns>读取数据是否成功</returns>
    public static ExcelCellValueReturn GetExcelCellValue(this ISheet sheet, CellType cellType, IRow row, ICell cell)
    {
        var result = new ExcelCellValueReturn();
        //CellType(Unknown = -1, Numeric = 0, String = 1, Formula = 2, Blank = 3, Boolean = 4, Error = 5,)

        if (cell == null)
        {
            return result;
        }

        switch (cellType)
        {
            case CellType.Numeric:
                //NPOI中数字和日期都是NUMERIC类型的，这里对其进行判断是否是日期类型
                if (DateUtil.IsCellDateFormatted(cell)) //日期类型
                {
                    //dataRow[columnIndex] = cell.DateCellValue;
                    result.ObjValue = cell.DateCellValue;
                }
                else //其他数字类型
                {
                    //dataRow[columnIndex] = cell.NumericCellValue;
                    result.ObjValue = cell.NumericCellValue;
                }
                break;
            case CellType.String:
                result.ObjValue = cell.StringCellValue;
                break;
            case CellType.Formula: //公式类型
                {
                    NPOI.SS.Formula.BaseFormulaEvaluator evaluator;
                    if (sheet is XSSFSheet)// 2007版本
                    {
                        evaluator = new XSSFFormulaEvaluator(sheet.Workbook);
                    }
                    else
                    {// 2003版本
                        evaluator = new HSSFFormulaEvaluator(sheet.Workbook);
                    }
                    var formulaValue = evaluator.Evaluate(cell);
                    //递归调用
                    result = GetExcelCellValue(sheet, formulaValue.CellType, row, cell);
                    break;
                }
            case CellType.Blank: //空的
                                 //dataRow[columnIndex] = "";
                result.ObjValue = null;
                break;
            case CellType.Boolean:
                // Boolean type
                //dataRow[columnIndex] = cell.BooleanCellValue;
                result.ObjValue = cell.BooleanCellValue;
                break;
            case CellType.Error:
                //dataRow[columnIndex] = row.GetCell(columnIndex).ErrorCellValue.ToString();
                result.Error = cell.ErrorCellValue.ToString();
                result.IsSuccess = false; //出错了，获取失败
                break;
        }
        return result;
    }


    /// <summary>
    /// XLS  转 XLSX
    /// XLS  转 XLSX 参考：https://www.cnblogs.com/HelloQLQ/p/16166140.html
    /// </summary>
    /// <param name="xlsPath">xls 文件路径</param>
    /// <returns></returns>
    public static string XlsToXlsx(this string xlsPath)
    {
        using var oldWorkbook = new HSSFWorkbook(new FileStream(xlsPath, FileMode.Open));

        var newExcelPath = xlsPath.ToLower().Replace(".xls", ".xlsx");
        using var fileStream = new FileStream(newExcelPath, FileMode.Create);
        {
            var newWorkBook = new XSSFWorkbook();
            //遍历所有的sheets
            for (int oldWorkBookIndex = 0; oldWorkBookIndex < oldWorkbook.NumberOfSheets; oldWorkBookIndex++)
            {
                var oldWorkSheet = oldWorkbook.GetSheetAt(oldWorkBookIndex);

                var newWorkSheet = newWorkBook.CreateSheet("Sheet1");

                foreach (HSSFRow oldRow in oldWorkSheet)
                {
                    var newRow = newWorkSheet.CreateRow(oldRow.RowNum);

                    for (int ii = oldRow.FirstCellNum; ii < oldRow.LastCellNum; ii++)
                    {
                        var newCell = newRow.CreateCell(ii);
                        newCell = oldRow.Cells[ii];
                    }
                }

                //合并单元格
                int sheetMergerCount = oldWorkSheet.NumMergedRegions;
                for (int me = 0; me < sheetMergerCount; me++)
                    newWorkSheet.AddMergedRegion(oldWorkSheet.GetMergedRegion(me));
            }

            newWorkBook.Write(fileStream);
            newWorkBook?.Close();
        }
        oldWorkbook?.Close();

        return newExcelPath;
    }


    /// <summary>
    /// DataTable 转换为 List<T>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static List<T> ConvertDataTableToList<T>(this DataTable dt) where T : new()
    {
        List<T> data = new List<T>();
        foreach (DataRow row in dt.Rows)
        {
            try
            {
                T item = new T();
                foreach (PropertyInfo property in typeof(T).GetProperties())
                {
                    try
                    {
                        //自定义列名
                        var excelColumnAttr = property.GetCustomAttribute<ExcelColumnAttribute>();
                        if (excelColumnAttr != null)
                        {
                            if (row[excelColumnAttr.ColumnName] != DBNull.Value)
                            {
                                property.SetValue(item, row[excelColumnAttr.ColumnName], null);
                            }
                        }
                        else
                        {
                            if (dt.Columns.Contains(property.Name))
                            {
                                if (row[property.Name] != DBNull.Value)
                                {
                                    property.SetValue(item, row[property.Name], null);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogEx.Error(ex, "Abc\\ConvertDataTableToList");
                    }
                }
                data.Add(item);
            }
            catch (Exception ex)
            {
                LogEx.Error(ex, "Abc\\ConvertDataTableToList");
            }
        }
        return data;
    }

    /// <summary>
    /// 获取excel表格列名和返回实体对象字段名称的映射关系
    /// 返回字典Key：对象字段名称，value:映射的excel列名称
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model">实体对象</param>
    /// <param name="columnNames">列名</param>
    /// <returns><![CDATA[ Dictionary<string, string>]]><!--Key:对象字段名称，value：映射的excel列名称--></returns>
    public static Dictionary<string, string> GetExcelColumnFieldAlias<T>(this T model, IEnumerable<string> columnNames) where T : new()
    {
        var customFieldNames = new Dictionary<string, string>();


        try
        {
            T item = new T();
            foreach (PropertyInfo property in typeof(T).GetProperties())
            {
                try
                {
                    //自定义列名
                    var excelColumnAttr = property.GetCustomAttribute<ExcelColumnAttribute>();
                    if (excelColumnAttr != null)
                    {
                        if (columnNames.Contains(excelColumnAttr.ColumnName))
                        {
                            customFieldNames.Add(excelColumnAttr.ColumnName, excelColumnAttr.ColumnName);
                        }
                        else if (columnNames.Contains(property.Name))
                        {
                            customFieldNames.Add(excelColumnAttr.ColumnName, property.Name);
                        }
                    }
                    else
                    {
                        customFieldNames.Add(property.Name, property.Name);
                    }
                }
                catch (Exception ex)
                {
                    LogEx.Error(ex, "Abc\\GetExcelColumnFieldAlias");
                }
            }
        }
        catch (Exception ex)
        {
            LogEx.Error(ex, "Abc\\GetExcelColumnFieldAlias");
        }

        return customFieldNames;
    }
}
