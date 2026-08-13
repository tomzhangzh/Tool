
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using System.IO;
using OfficeOpenXml;
using TUI.Services.Properties;
using OfficeOpenXml.Style;
using TUI.Services.Extension;

namespace TUI.Services.Manager
{
    public partial class ExcelTemplateSetting
    {
        public Dictionary<string, int> RowsTemplate { get; set; } = new Dictionary<string, int>();
        public int StartRow { get; set; }
        public int StartColumn { get; set; }


    }
    public partial class ExcelCellBindingInfo
    {
        public int ColumnIndex { get; set; }
        public string Value { get; set; }
        //public string Formula { get; set; }
        //public int? MergeColumnCount { get; set; }
    }
    public partial class RowTemplate
    {
        public string TemplateName { get; set; }
        public int RowNumber { get; set; }
        public List<ExcelCellBindingInfo> CellSettings { get; set; } = new List<ExcelCellBindingInfo>();
    }
    public interface IExcelUtilityService : IScopeDependency
    {
        ExcelRangeBase Clone(int templateRow, int newRow);
        void SetWorkSheetByWorkBook(ExcelWorkbook workbook);
        void SetWorkSheet(ExcelWorksheet worksheetTemplate, ExcelWorksheet worksheet);
        public ExcelWorkbook workbook { get; set; }
        public ExcelWorksheet worksheet { get; set; }
        string SetWorkBookByTemplate(string TemplatePath);
        void SaveUseOffice(string file);
    }
    public class ExcelUtilityService : IExcelUtilityService
    {
        public ExcelUtilityService()
        {
        }
        public ExcelWorksheet worksheet { get; set; }
        private ExcelWorksheet worksheetTemplate = null;
        public ExcelWorkbook workbook { get; set; }
        public void SetWorkSheetByWorkBook(ExcelWorkbook workbook)
        {
            this.workbook = workbook;
            SetWorkSheet(workbook.Worksheets[0], workbook.Worksheets[1]);
        }
        public string SetWorkBookByTemplate(string TemplatePath)
        {
            var returnFile = Path.Combine(App.WebHostEnvironment.ContentRootPath,App.SystemSetting.ExcelGenerate_Setting.ExcelProcessedFolder, $"{Guid.NewGuid().ToString("N")}.xlsx");
            var templateFile = Path.Combine(App.WebHostEnvironment.ContentRootPath, App.SystemSetting.ExcelGenerate_Setting.ExcelTemplatePath, TemplatePath);
            System.IO.File.Copy(templateFile, returnFile);
            using (var ExPack = new ExcelPackage(returnFile))
            {
                var wb = ExPack.Workbook;
                SetWorkSheetByWorkBook(workbook);
                return returnFile;
            }
        }
        public void SetWorkSheet(ExcelWorksheet worksheetTemplate, ExcelWorksheet worksheet)
        {
            this.worksheet = worksheet;
            this.worksheetTemplate = worksheetTemplate;
        }
        public ExcelRangeBase Clone(int templateRow, int newRow)
        {
            if (worksheet.Rows.Count() + 1 < newRow)
            {
                worksheet.InsertRow(worksheet.Dimension.Rows, 1);
                
            }
            worksheetTemplate.Rows[templateRow].Range.Copy(worksheet.Rows[newRow + 1].Range);
            return worksheet.Rows[newRow + 1].Range;
        }
        public void SaveUseOffice(string file)
        {
            
        }
    }
    public interface IExcelGenerate : IScopeDependency
    {
        IExcelUtilityService excelUtilityService { get; set; }
        string GetExcelFile<T>(string TemplatePath, List<(string, object)> RowData, T info, bool removeTemplateSheet = true, Action<ExcelWorksheet> beforeSave = null, string filePre = null);
        string GetExcelFile<T>(string TemplatePath, List<Tuple<string, int, int, object>> CellData, T info, bool removeTemplateSheet = true, Action<ExcelWorksheet> beforeSave = null, Action<ExcelWorksheet, ExcelGenerate> beforeSaveWithThis = null, string filePre = null);
        //string ToPDF(string fileName, string filePre = null);
        int ToIndex(string columnName);
        string ToName(int index);
        void SetPageMinWidth(int minPageWidth, ExcelWorksheet worksheet);
        //void CopyRange(string fromFile, string ToFile, string sourceRange, int toRow, int? addEmptyRow = 0);
        //void CopyRange(string fromFile, string ToFile, int fromRow, int toRow, int? addEmptyRow = 0);
        void SaveExcel(string fileName, Action<ExcelWorkbook> beforeSave = null);
        //ExcelWorkbook OpenFile(string fileName);
    }
    public class ExcelGenerate : IExcelGenerate
    {
     
        public IExcelUtilityService excelUtilityService { get; set; }
         public ExcelGenerate(
            IExcelUtilityService excelUtilityService)
        {

            this.excelUtilityService = excelUtilityService;

        }
        private ExcelWorksheet worksheet = null;
        private ExcelWorksheet worksheetTemplate = null;
        private ExcelTemplateSetting templateSetting = new ExcelTemplateSetting();
        private List<RowTemplate> RowTemplates = new List<RowTemplate>();
        private Dictionary<string, Tuple<ExcelStyle, double>> cellsStyle = new Dictionary<string, Tuple<ExcelStyle, double>>();
        //private List<ExcelRangeBase> Cells = null;
        private int nextRow = 1;
        //private System.Collections.Hashtable Cells = new System.Collections.Hashtable();

        //private void setCells(ExcelWorksheet worksheet)
        //{
        //    this.Cells = new System.Collections.Hashtable();
        //    worksheet.Cells.ToList().ForEach(x =>

        //    {
        //        Cells[$"{x.Rows}-{x.Columns}"] = x;
        //    });
        //}
        //public ExcelRangeBase getCell(int Row, int Column)
        //{
        //    ExcelRangeBase result = null;
        //    return this.worksheet.Cells[Row, Column];
        //    result = this.Cells[$"{Row + 1}-{Column + 1}"] as ExcelRangeBase;
        //    return result;
        //}
        public int ToIndex(string columnName)
        {
            if (!Regex.IsMatch(columnName.ToUpper(), @"[A-Z]+")) { throw new Exception("invalid parameter"); }
            int index = 0;
            char[] chars = columnName.ToUpper().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                index += ((int)chars[i] - (int)'A' + 1) * (int)Math.Pow(26, chars.Length - i - 1);
            }
            return index - 1;
        }

        public string ToName(int index)
        {
            if (index < 0) { throw new Exception("invalid parameter"); }

            List<string> chars = new List<string>();
            do
            {
                if (chars.Count > 0) index--;
                chars.Insert(0, ((char)(index % 26 + (int)'A')).ToString());
                index = (int)((index - index % 26) / 26);
            } while (index > 0);

            return string.Join(string.Empty, chars.ToArray());
        }
        public void SetPageMinWidth(int minPageWidth, ExcelWorksheet worksheet)
        {

            var pageWidth = worksheet.Columns.Select(x => x.Width).Sum();
            if (pageWidth < minPageWidth)
            {
                worksheet.Columns.ToList().ForEach(x =>
                {
                    x.Width = (minPageWidth / pageWidth) * x.Width;
                });

            }

        }
        //private List<ExcelRangeBase> cells = new List<ExcelRangeBase>();
        //private ExcelRangeBase getCell(List<ExcelRangeBase> cells, int nextRow, int colIndex)
        //{
        //    return cells.Single(x => x.Row == nextRow + 1 && x.Column == colIndex + 1);
        //}
        private void initVer()
        {
            worksheet = null;
            worksheetTemplate = null;
            templateSetting = new ExcelTemplateSetting();
            RowTemplates = new List<RowTemplate>();
            nextRow = 1;
        }
        private void loadCellTemplate()
        {
            foreach (var item in templateSetting.RowsTemplate)
            {
                var cell = worksheetTemplate.Cells[item.Value, 1];
                cellsStyle[item.Key] = new Tuple<ExcelStyle, double>(cell.Style, worksheetTemplate.Row(cell.Rows).Height);
            }
        }
        //public ExcelWorkbook OpenFile(string fileName)
        //{
        //    using (var ExPack = new ExcelPackage(fileName))
        //    {
        //        var wb = ExPack.Workbook;
        //        return wb;
        //    }
        //}
        public string CopyFileFromTemplate(string TemplatePath,string filePre = null)
        {
            var returnFile = Path.Combine(App.WebHostEnvironment.ContentRootPath, App.SystemSetting.ExcelGenerate_Setting.ExcelProcessedFolder, $"{filePre ?? ""}{Guid.NewGuid().ToString("N")}.xlsx");
            System.IO.File.Copy(Path.Combine(App.WebHostEnvironment.ContentRootPath, App.SystemSetting.ExcelGenerate_Setting.ExcelTemplatePath, TemplatePath), returnFile);
            return returnFile;
        }
        //public void CopyRange(string fromFile, string ToFile, string copyRange, int toRow, int? addEmptyRow = 0)
        //{

    
        //    var fromExcelWorkbook= OpenFile(fromFile);
        //    var toExcelWorkbook= OpenFile(ToFile);
        //    var copyExcelRangeBase =fromExcelWorkbook.Worksheets.FirstOrDefault(f => f.View.TabSelected).Cells[copyRange];
        //    toExcelWorkbook.ActiveSheet.InsertRow(toRow, copyExcelRangeBase.RowCount + (addEmptyRow ?? 0));
        //    toExcelWorkbook.Save();
        //    copyExcelRangeBase.Copy(toExcelWorkbook.ActiveSheet.Rows[toRow - 1]);
        //    toExcelWorkbook.Save();

        //}
        //public void CopyRange(string fromFile, string ToFile, int fromRow, int toRow, int? addEmptyRow = 0)
        //{
        //    Spire.Xls.ExcelWorkbook fromExcelWorkbook = new ExcelWorkbook();
        //    fromExcelWorkbook.LoadFromFile(fromFile);
        //    this.CopyRange(fromFile, ToFile, $"A{fromRow}:{ToName(fromExcelWorkbook.ActiveSheet.Columns.Count())}{fromExcelWorkbook.ActiveSheet.Rows.Count()}", toRow, addEmptyRow);


        //}
        public void SaveExcel(string fileName, Action<ExcelWorkbook> beforeSave = null)
        {
            using (var ExPack = new ExcelPackage(fileName))
            {
                var workbook = ExPack.Workbook;
                beforeSave?.Invoke(workbook);
                ExPack.Save();
            }

        }

        public string GetExcelFile<T>(string TemplatePath, List<Tuple<string, int, int, object>> CellData, T info, bool removeTemplateSheet = true
            , Action<ExcelWorksheet> beforeSave = null
            , Action<ExcelWorksheet, ExcelGenerate> beforeSaveWithThis = null
            , string filePre = null)
        {
            initVer();
            var returnFile = CopyFileFromTemplate(TemplatePath, filePre);
            using (var ExPack = new ExcelPackage(returnFile))
            {
                ExcelWorkbook workbook = ExPack.Workbook;
                worksheetTemplate = workbook.Worksheets[0];
                worksheet = workbook.Worksheets[1];
                templateSetting = worksheetTemplate.Cells[1, 1].Value.ToString().Deserialize<ExcelTemplateSetting>();
                loadCellTemplate();
                if (CellData.Count > 0)
                {
                    //var maxRow = CellData.Select(x => x.Item2).Max() + 1;
                    //var maxColumn = CellData.Select(x => x.Item3).Max() + 1;
                    //if (maxRow > worksheet.Rows.Count())
                    //{
                    //    worksheet(maxRow);
                    //}
                    //if (maxColumn > worksheet.Columns.Count())
                    //{
                    //    worksheet.SetLastColumn(maxColumn);
                    //}
                }
                processCellForInfo(info, worksheet);
                foreach (var data in CellData)
                {
                    processCell(data);
                }
                if (removeTemplateSheet)
                {
                    worksheetTemplate.Hidden = eWorkSheetHidden.VeryHidden;
                }
                else
                {
                    worksheetTemplate.Hidden = eWorkSheetHidden.Hidden;
                }
                if (beforeSave != null)
                {
                    beforeSave?.Invoke(worksheet);
                }
                if (beforeSaveWithThis != null)
                {
                    beforeSaveWithThis?.Invoke(worksheet, this);
                }

                return returnFile;
            }
        }
        private void processCell(Tuple<string, int, int, object> data)
        {
            var find = worksheet.Cells[data.Item2, data.Item3];
            var findCellTemplate = cellsStyle.Where(x => x.Key == data.Item1).FirstOrDefault();
            //Todo
            //find.Style = findCellTemplate.Value.Item1;
            find.Value = data.Item4;
        }

        public string GetExcelFile<T>(string TemplatePath, List<(string, object)> RowData, T info, bool removeTemplateSheet = true, Action<ExcelWorksheet> beforeSave = null, string filePre = null)
        {
            initVer();
            var returnFile = $"{Path.Combine(App.WebHostEnvironment.ContentRootPath, App.SystemSetting.ExcelGenerate_Setting.ExcelProcessedFolder)}{filePre ?? ""}{Guid.NewGuid().ToString("N")}.xlsx";
            System.IO.File.Copy(Path.Combine(App.WebHostEnvironment.ContentRootPath, App.SystemSetting.ExcelGenerate_Setting.ExcelTemplatePath, TemplatePath), returnFile);
            using (var ExPack = new ExcelPackage(returnFile))
            {
                ExcelWorkbook workbook = ExPack.Workbook;
                worksheetTemplate = workbook.Worksheets[0];
                worksheet = workbook.Worksheets[1];
                templateSetting = worksheetTemplate.Cells[1, 1].Value.ToString().Deserialize<ExcelTemplateSetting>();
                loadRowTemplate();
                nextRow = templateSetting.StartRow;
                processCellForInfo(info, worksheet);
                foreach (var data in RowData)
                {
                    processRow(data);
                    nextRow++;
                }
                this.setMergeCells();
                if (removeTemplateSheet)
                {
                    worksheetTemplate.Hidden = eWorkSheetHidden.VeryHidden;
                }
                else
                {
                    worksheetTemplate.Hidden = eWorkSheetHidden.Hidden;
                }
                beforeSave?.Invoke(worksheet);
                workbook.Calculate();
                ExPack.Save();
                return returnFile;
            }
        }

        private void processCellForInfo<T>(T info, ExcelWorksheet worksheet)
        {
            var list = worksheet.Cells.Where(x=>$"{x.Value}".IndexOf("{")>=0).ToList();
            foreach (var item in list)
            {
                item.Value = ReplaceTemplate(info, $"{item.Value}");
            }
        }
        public string ReplaceTemplate<T>(T info, string code)
        {
            var interpreter = new DynamicExpresso.Interpreter().Reference(typeof(ExcelGenerate)).Reference(typeof(System.DateTime)).Reference(typeof(TUI.Services.Properties.Settings));
            MatchEvaluator evaluator = match =>
            {
                string CodeName = match.Groups["Name"].Value;
                var formated = interpreter.Eval<object>(CodeName,
                     new DynamicExpresso.Parameter("Setting", TUI.Services.Properties.Settings.Default),
                     new DynamicExpresso.Parameter("ExcelGenerate", this),
                     new DynamicExpresso.Parameter("Info", info));
                return $"{formated}";
            };
            return Regex.Replace(code, @"{(?<Name>[^}]+)}", evaluator, RegexOptions.Compiled);
        }
        private void processRow((string, object) rowData)
        {
            var row = RowTemplates.Where(x => x.TemplateName == rowData.Item1).Single();
            var templateRow = worksheetTemplate.Rows[row.RowNumber];

            templateRow.Range.Copy(worksheet.Rows[nextRow].Range);
            processCell(row, rowData);
        }

        private void processCell(RowTemplate row, (string, object) rowData)
        {
            foreach (var item in row.CellSettings.Where(x => x.Value.IsNullOrEmpty() == false))
            {
                //if (item.MergeColumnCount != null)
                //{
                //    this.MergeCells.Add($"{this.ToName(item.ColumnIndex + 1)}{nextRow + 1}:{this.ToName(item.ColumnIndex + 1 + item.MergeColumnCount.Value)}{nextRow + 1}");
                //}
                var value = getCellValue(rowData.Item2, item);
                //if (this.getCell(nextRow, item.ColumnIndex) == null)
                //{
                //    this.setCells(this.worksheet);
                //}
                this.worksheet.Cells[nextRow, item.ColumnIndex].Value = value;
            }
            //foreach (var item in row.CellSettings.Where(x => x.Value.IsNullOrEmpty() == true && x.Formula.IsNullOrEmpty() == false))
            //{
            //    var value = "";
            //    worksheet.Rows[nextRow].Cells[item.ColumnIndex].Value2 = value;
            //}
        }

        private object getCellValue(object rowData, ExcelCellBindingInfo item)
        {
            if (item.Value.StartsWith("{") || item.Value.StartsWith("'{"))
            {
                return this.ReplaceTemplate(rowData, item.Value);
            }
            //else if (item.Value.StartsWith("'")){
            //    if (rowData.GetType().GetProperty(item.Value.Replace("'","")) != null)
            //    {
            //        return $"'{rowData.GetType().GetProperty(item.Value).GetValue(rowData, null)}";
            //    }
            //    return item.Value;
            //}
            else
            {
                if (rowData.GetType().GetProperty(item.Value) != null)
                {
                    return rowData.GetType().GetProperty(item.Value).GetValue(rowData, null);
                }

                return item.Value;
            }
        }

        private void loadRowTemplate()
        {
            foreach (var item in templateSetting.RowsTemplate)
            {
                var RowTemplate = new RowTemplate()
                {
                    TemplateName = item.Key,
                    RowNumber = item.Value,
                    CellSettings = new List<ExcelCellBindingInfo>(),
                };

                int count = worksheetTemplate.Dimension.End.Column;
                for (int i = 1; i <= count; i++)
                {
                    var value = $"{worksheetTemplate.Cells[item.Value+1,i].Value}";
                    //var formula = worksheetTemplate.Rows[item.Value].Cells[i].Value;
                    if (value.IsNullOrEmpty() == false)
                    {
                        RowTemplate.CellSettings.Add(new ExcelCellBindingInfo()
                        {
                            ColumnIndex = i,
                            Value = value,
                            //Formula = formula,
                            //ToDo:
                            //MergeColumnCount = worksheetTemplate.MergedCells == null ? null : worksheetTemplate.MergedCells.Where(x => x.row == item.Value && x.Column == i).FirstOrDefault()?.ColumnCount,
                            

                        });
                    }
                }
                this.RowTemplates.Add(RowTemplate);
            }
        }
        private List<string> MergeCells = new List<string>();
        private void setMergeCells()
        {
            if (MergeCells == null)
            {
                return;
            }
            //ToDo:
            //foreach (var item in MergeCells)
            //{
            //    worksheet.Range[item].Merge();

            //}
        }

    }
}
