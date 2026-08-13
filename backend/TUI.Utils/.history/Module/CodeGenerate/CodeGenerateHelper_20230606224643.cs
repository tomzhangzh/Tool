// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Furion.ViewEngine;
using Furion.ViewEngine.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace TUI.Utils;

public class CodeGenerateHelper
{
    // 生成模板代码
    public static void GenerateCode(CG_CodeGenerateConfig config)
    {
        var scopeFactory = App.GetService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        var _logger = AppEx.GetSeriLogger("代码生成");
        try
        {
            //var _db = App.GetService<ISqlSugarClient>(services);
            ISqlSugarClient _db = SqlSugarHelper.Db;
            if (!string.IsNullOrWhiteSpace(config.DbConn))
            {
                SqlSugarHelper.Db.AddOrUpdateConnection(new ConnectionConfig()
                {
                    DbType = config.DbType.ToEnum<SqlSugar.DbType>(),
                    ConfigId = $"TUI_GenerateCode",//设置库的唯一标识
                    IsAutoCloseConnection = true,
                    ConnectionString = config.DbConn
                });

                _db = SqlSugarHelper.Db.ToTenant($"TUI_GenerateCode");
            }

            var tables = _db.DbMaintenance.GetTableInfoList(false);//true 走缓存 false不走缓存

            var tableinfos = new List<CG_CodeTableInfo>();

            //所有的表
            var codetypes = GetCodeTypes();

            foreach (var table in tables)
            {
                //Console.WriteLine(table.Description);//输出表信息

                //获取列信息
                //var columns=db.DbMaintenance.GetColumnInfosByTableName("表名",false);

                var tableinfo = new CG_CodeTableInfo();
                tableinfo.Config = config;
                tableinfo.TableName = table.Name;
                tableinfo.Description = table.Description;
                tableinfo.ClassName = GetCsharpName(table.Name);

                //实体名称，可能包含前缀，根据替换定
                tableinfo.EntityName = GetDbTableReplace(tableinfo.ClassName, config.DbTableReplace);
                //没有前缀的实体名称
                tableinfo.NoPrefixEntity = GetDbTableNoPrefixEntity(tableinfo.ClassName, config.DbTableNoPrefix);

                //应用层实体名称【服务层实体替换，例如：Sys_=Ht,APC_=Ht或者Sys_=Ht,APC_=Ht ;  等号前是被替换名称，等号后是替换的新内容】
                tableinfo.MvcApplicationEntityName = config.MvcApplicationDtoPrefix + GetDbTableReplace(tableinfo.ClassName, config.ApplicationEntityReplace);
                //控制体名称【Sys_=,APC_=，等号前是被替换名称，等号后是替换的新内容】
                tableinfo.MvcControllerName = config.MvcControllerEntityPrefix + GetDbTableReplace(tableinfo.ClassName, config.MvcControllerEntityReplace);

                //应用层实体名称【服务层实体替换，例如：Sys_=Ht,APC_=Ht或者Sys_=Ht,APC_=Ht ;  等号前是被替换名称，等号后是替换的新内容】
                tableinfo.ApiApplicationEntityName = config.ApiDtoEntityPrefix + GetDbTableReplace(tableinfo.ClassName, config.ApplicationEntityReplace);

                foreach (var columnInfo in _db.DbMaintenance.GetColumnInfosByTableName(table.Name, false))
                {
                    var typeInfo = GetEntityType(codetypes, columnInfo, _db.CurrentConnectionConfig.DbType);
                    CG_CodeColumnsInfo column = new CG_CodeColumnsInfo()
                    {
                        ClassProperName = GetCsharpName(columnInfo.DbColumnName),
                        DbColumnName = columnInfo.DbColumnName,
                        Description = columnInfo.ColumnDescription,
                        IsIdentity = columnInfo.IsIdentity,
                        IsPrimaryKey = columnInfo.IsPrimarykey,
                        Required = columnInfo.IsNullable == false,
                        //CodeTableId = entity.Id,
                        CodeType = typeInfo.CodeType.Name,
                        Length = typeInfo.DbTypeInfo.Length,
                        DecimalDigits = typeInfo.DbTypeInfo.DecimalDigits,
                        DefaultValue = columnInfo.DefaultValue,
                        IsNullable = columnInfo.IsNullable,
                        // IsIgnore=columnInfo.i
                    };
                    var codeType = codetypes.First(it => it.Name == column.CodeType);
                    column.IsSpecialType = IsSpecialType(column);
                    column.Type = IsSpecialType(column) ? GetType(column) : codeType.CSharepType;
                    tableinfo.ColumnsInfos.Add(column);
                }

                tableinfos.Add(tableinfo);
            }

            var codeTemplates = GetCodeTemplates();

            //先删除文件夹
            var GenerateCodeDirectory = App.WebHostEnvironment.ContentRootPath.TrimEnd('\\') + "\\GenerateCode\\";
            FileHelper.TryDeleteDirectory(GenerateCodeDirectory);

            foreach (CG_CodeTableInfo tableinfo in tableinfos)
            {
                foreach (var codeTemplate in codeTemplates)
                {
                    //var resultContent = GetTemplateValue(codeTemplate, tableinfo);
                    var resultContent = RunCompile(codeTemplate, tableinfo);

                    var codeTemplateRootPath = App.WebHostEnvironment.ContentRootPath.TrimEnd('\\') + "\\GenerateCode\\";

                    var newfilename = GetReleaseContent(tableinfo, codeTemplate, codeTemplate.OutFileName);
                    var filePath = $"{codeTemplateRootPath.TrimEnd('\\')}\\{GetReleaseContent(tableinfo, codeTemplate, codeTemplate.SaveRelativeDirectory.TrimEnd('\\'))}\\{newfilename}";
                    //先删除文件
                    FileHelper.TryDeleteFile(filePath);
                    //创建文件
                    FileHelper.CreateFile(filePath, resultContent, System.Text.Encoding.UTF8);
                }
            }
            GC.Collect();
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToStringEx());
        }
    }

    /// <summary>
    /// 获取代码模板
    /// </summary>
    /// <returns></returns>
    private static List<CG_CodeTemplate> GetCodeTemplates()
    {
        var CodeTemplates = new List<CG_CodeTemplate>();

        #region MVC

        CodeTemplates.Add(new CG_CodeTemplate(CodeTemplateType.MVC, "Application_Input.txt", "MVC\\Application\\{{NoPrefixEntity}}\\Dto", outFileName: "{{MvcApplicationDtoPrefix}}{{NoPrefixEntity}}Input.cs"));
        CodeTemplates.Add(new CG_CodeTemplate(CodeTemplateType.MVC, "Application_Out.txt", "MVC\\Application\\{{NoPrefixEntity}}\\Dto", outFileName: "{{MvcApplicationDtoPrefix}}{{NoPrefixEntity}}Out.cs"));
        CodeTemplates.Add(new CG_CodeTemplate(CodeTemplateType.MVC, "Application_Query.txt", "MVC\\Application\\{{NoPrefixEntity}}\\Dto", outFileName: "{{MvcApplicationDtoPrefix}}{{NoPrefixEntity}}Query.cs"));
        CodeTemplates.Add(new CG_CodeTemplate(CodeTemplateType.MVC, "Application_IService.txt", "MVC\\Application\\{{NoPrefixEntity}}", outFileName: "I{{MvcApplicationDtoPrefix}}{{NoPrefixEntity}}Service.cs"));
        CodeTemplates.Add(new CG_CodeTemplate(CodeTemplateType.MVC, "Application_Service.txt", "MVC\\Application\\{{NoPrefixEntity}}", outFileName: "{{MvcApplicationDtoPrefix}}{{NoPrefixEntity}}Service.cs"));
        CodeTemplates.Add(new CG_CodeTemplate(CodeTemplateType.MVC, "Application_MapperInput.txt", "MVC\\Application\\{{NoPrefixEntity}}", outFileName: "{{MvcApplicationDtoPrefix}}{{NoPrefixEntity}}MapperInput.cs"));
        CodeTemplates.Add(new CG_CodeTemplate(CodeTemplateType.MVC, "Application_MapperOut.txt", "MVC\\Application\\{{NoPrefixEntity}}", outFileName: "{{MvcApplicationDtoPrefix}}{{NoPrefixEntity}}MapperOut.cs"));

        CodeTemplates.Add(new CG_CodeTemplate(CodeTemplateType.MVC, "View_Controller.txt", "MVC\\Controllers", outFileName: "{{MvcControllerName}}Controller.cs"));
        CodeTemplates.Add(new CG_CodeTemplate(CodeTemplateType.MVC, "View_PearViews_Index.txt", "MVC\\PearViews\\{{MvcControllerName}}", ".cshtml", outFileName: "Index.cshtml"));
        CodeTemplates.Add(new CG_CodeTemplate(CodeTemplateType.MVC, "View_PearViews_Info.txt", "MVC\\PearViews\\{{MvcControllerName}}", ".cshtml", outFileName: "Info.cshtml"));
        CodeTemplates.Add(new CG_CodeTemplate(CodeTemplateType.MVC, "View_PearViews_InfoTable.txt", "MVC\\PearViews\\{{MvcControllerName}}", ".cshtml", outFileName: "InfoTable.cshtml"));

        #endregion MVC

        #region Api

        CodeTemplates.Add(new CG_CodeTemplate(CodeTemplateType.WebAPI, "Application_Input.txt", "API\\{{NoPrefixEntity}}\\Dto", outFileName: "{{ApiDtoEntityPrefix}}{{NoPrefixEntity}}Input.cs"));
        CodeTemplates.Add(new CG_CodeTemplate(CodeTemplateType.WebAPI, "Application_Out.txt", "API\\{{NoPrefixEntity}}\\Dto", outFileName: "{{ApiDtoEntityPrefix}}{{NoPrefixEntity}}Out.cs"));
        CodeTemplates.Add(new CG_CodeTemplate(CodeTemplateType.WebAPI, "Application_Query.txt", "API\\{{NoPrefixEntity}}\\Dto", outFileName: "{{ApiDtoEntityPrefix}}{{NoPrefixEntity}}Query.cs"));
        CodeTemplates.Add(new CG_CodeTemplate(CodeTemplateType.WebAPI, "Application_Service.txt", "API\\{{NoPrefixEntity}}", outFileName: "{{ApiControllerEntityPrefix}}{{NoPrefixEntity}}Service.cs"));
        CodeTemplates.Add(new CG_CodeTemplate(CodeTemplateType.WebAPI, "Application_MapperInput.txt", "API\\{{NoPrefixEntity}}", outFileName: "{{ApiControllerEntityPrefix}}{{NoPrefixEntity}}MapperInput.cs"));
        CodeTemplates.Add(new CG_CodeTemplate(CodeTemplateType.WebAPI, "Application_MapperOut.txt", "API\\{{NoPrefixEntity}}", outFileName: "{{ApiControllerEntityPrefix}}{{NoPrefixEntity}}MapperOut.cs"));

        #endregion Api

        return CodeTemplates;
    }

    private static string GetReleaseContent(CG_CodeTableInfo codeTableInfo, CG_CodeTemplate codeTemplate, string content)
    {
        //模板
        content = content.Replace(".txt", codeTemplate.OutFileExtensionName);

        //表

        //content = content.Replace("{{ClassName}}", codeTableInfo.ClassName);// 类名
        //content = content.Replace("{{TableName}}", codeTableInfo.TableName);// 表名
        //content = content.Replace("{{Description}}", codeTableInfo.Description);// 描述

        //content = content.Replace("{{EntityName}}", codeTableInfo.EntityName);// 实体名称，可能包含前缀，根据替换定
        //content = content.Replace("{{ApplicationEntityName}}", codeTableInfo.ApplicationEntityName);// 应用层实体名称【服务层实体替换，例如：Sys_=Ht,TUI_=Ht或者Sys_=Ht,TUI_=Ht ;  等号前是被替换名称，等号后是替换的新内容】
        //content = content.Replace("{{ControllerName}}", codeTableInfo.ControllerName);// 控制体名称【Sys_=,TUI_=，等号前是被替换名称，等号后是替换的新内容】
        //content = content.Replace("{{NoPrefixEntity}}", codeTableInfo.NoPrefixEntity);// 没有前缀的实体名称

        var tableDics = codeTableInfo.GetDictionary();
        foreach (var item in tableDics)
        {
            content = content.Replace($"{{{{{item.Key}}}}}", item.Value);
        }

        var configDics = codeTableInfo.Config.GetDictionary();
        foreach (var configField in configDics)
        {
            content = content.Replace($"{{{{{configField.Key}}}}}", configField.Value);
        }
        ////配置
        //content = content.Replace("{{ProjectName}}", codeTableInfo.Config.ProjectName);
        //content = content.Replace("{{DbTablePrefixReplace}}", codeTableInfo.Config.DbTablePrefixReplace);
        //content = content.Replace("{{DbTableReplace}}", codeTableInfo.Config.DbTableReplace);
        //content = content.Replace("{{DbTableNoPrefix}}", codeTableInfo.Config.DbTableNoPrefix);
        ////应用层
        //content = content.Replace("{{ApplicationArea}}", codeTableInfo.Config.ApplicationArea);
        //content = content.Replace("{{ApplicationEntityReplace}}", codeTableInfo.Config.ApplicationEntityReplace);
        //content = content.Replace("{{ApplicationDtoPrefix}}", codeTableInfo.Config.ApplicationDtoPrefix);
        //
        ////controller
        //content = content.Replace("{{ControllerNameSpaceName}}", codeTableInfo.Config.MvcControllerNameSpaceName);
        //content = content.Replace("{{ControllerBaseName}}", codeTableInfo.Config.MvcControllerBaseName);
        //content = content.Replace("{{ControllerArea}}", codeTableInfo.Config.MvcControllerArea);
        //content = content.Replace("{{ControllerEntityReplace}}", codeTableInfo.Config.MvcControllerEntityReplace);
        //content = content.Replace("{{ControllerEntityPrefix}}", codeTableInfo.Config.MvcControllerEntityPrefix);
        //
        ////API
        //content = content.Replace("{{ApiArea}}", codeTableInfo.Config.ApiArea);
        //content = content.Replace("{{ApiControllerEntityPrefix}}", codeTableInfo.Config.ApiControllerEntityPrefix);
        //content = content.Replace("{{ApiDtoEntityPrefix}}", codeTableInfo.Config.ApiDtoEntityPrefix);

        return content;
    }

    /// <summary>
    /// 运行编译
    /// </summary>
    /// <param name="codeTemplate"></param>
    /// <param name="tableinfo"></param>
    /// <returns></returns>
    private static string RunCompile(CG_CodeTemplate codeTemplate, CG_CodeTableInfo tableinfo)
    {
        var result = "";
        var _logger = AppEx.GetSeriLogger("代码生成");
        var templatecontent = "";
        try
        {
            //模板的完整路径
            //var codeTemplateRootPath = App.WebHostEnvironment.ContentRootPath.TrimEnd('\\') + $"\\CodeTemplate\\{codeTemplate.Type.ToString()}\\{codeTemplate.TemplateName}";
            //模板的完整路径
            var codeTemplateRootPath = "";
            //if (App.WebHostEnvironment.IsDevelopment())
            {
                codeTemplateRootPath = AppContext.BaseDirectory.TrimEnd('\\') + $"\\Module\\CodeGenerate\\CodeTemplates\\{codeTemplate.Type.ToString()}\\{codeTemplate.TemplateName}";
            }
            if (!FileHelper.IsExistFile(codeTemplateRootPath))
            {
                codeTemplateRootPath = App.WebHostEnvironment.ContentRootPath.TrimEnd('\\') + $"\\Module\\CodeGenerate\\CodeTemplates\\{codeTemplate.Type.ToString()}\\{codeTemplate.TemplateName}";
            }
            if (!FileHelper.IsExistFile(codeTemplateRootPath))
            {
                codeTemplateRootPath = App.WebHostEnvironment.ContentRootPath.TrimEnd('\\') + $"\\CodeTemplates\\{codeTemplate.Type.ToString()}\\{codeTemplate.TemplateName}";
            }

            if (FileHelper.IsExistFile(codeTemplateRootPath))
            {
                templatecontent = FileHelper.FileToString(codeTemplateRootPath, System.Text.Encoding.UTF8);
            }

            Scoped.Create((factory, scope) =>
        {
            var services = scope.ServiceProvider;

            IViewEngine _viewEngine = App.GetService<IViewEngine>(services);

            result = templatecontent.RunCompile(tableinfo, builder =>
            //result = templatecontent.RunCompileFromCached<CG_CodeTableInfo>(tableinfo, builderAction: builder =>
            //result = _viewEngine.RunCompileFromCached<CG_CodeTableInfo>(templatecontent, tableinfo, builderAction: builder =>
            {
                //bulder.AddAssemblyReferenceByName("Microsoft.AspNetCore.Mvc.Rendering");
                //builder.AddAssemblyReferenceByName("System.Linq.Dynamic.Core");
                //builder.AddAssemblyReferenceByName("System.Linq.Expressions");
                //builder.AddAssemblyReferenceByName("System.Collections");
                //builder.AddAssemblyReferenceByName("System.Linq");
                builder.AddAssemblyReferenceByName("System.Linq.Dynamic.Core");

                builder.AddUsing("System.Linq.Dynamic.Core");
                //builder.AddUsing("System.Linq.Expressions");
                //builder.AddUsing("System.Collections");
            });

            // result = _viewEngine.RunCompileFromCached(templatecontent, tableinfo);
        });
        }
        catch (Exception ex)
        {
            _logger.Error($"运行编译  模板：{codeTemplate.TemplateName}\r\n{ex.ToStringEx()}");
            //throw;
        }
        return result;
    }

    #region RazorEngine 暂时先不用了，用Furion的ViewEngine

    //private static IRazorEngineService razorEngineService { get; set; }

    ///// <summary>
    ///// Razor 引擎服务
    ///// </summary>
    ///// <returns></returns>
    //private static IRazorEngineService GetRazorEngineService()
    //{
    //    if (razorEngineService == null)
    //    {
    //        //创建配置
    //        var config = new TemplateServiceConfiguration();
    //        config.Language = Language.CSharp; // VB.NET as template language.
    //        config.EncodedStringFactory = new RawStringFactory(); // Raw string encoding. 可解决输出的html字符是编码后的
    //                                                              //config.EncodedStringFactory = new HtmlEncodedStringFactory(); // Html encoding.

    //        //根据配置创建服务
    //        razorEngineService = RazorEngineService.Create(config);
    //    }
    //    return razorEngineService;
    //}

    ///// <summary>
    ///// 添加默认模板
    ///// </summary>
    ///// <param name="codeTemplate"></param>
    //private static void AddDefaultTemplate(CG_CodeTemplate codeTemplate)
    //{
    //    var codeTemplateRootPath = App.WebHostEnvironment.ContentRootPath.TrimEnd('\\') + $"\\CodeTemplate\\{codeTemplate.Type.ToString()}\\View_PearViews_example.txt";
    //    if (FileHelper.IsExistFile(codeTemplateRootPath))
    //    {
    //        //添加模板缓存
    //        var name = $"View_PearViews_example";
    //        //验证模板缓存是否存在
    //        if (RazorHelper.CustomerEngine().IsTemplateCached(name, null) == false)
    //        {
    //            var templatecontent = FileHelper.FileToString(codeTemplateRootPath, System.Text.Encoding.UTF8);
    //            RazorHelper.CustomerEngine().AddTemplate(name, templatecontent);
    //        }
    //    }
    //    //@Include("View_PearViews_example")
    //}
    ///// <summary>
    /////
    ///// </summary>
    ///// <param name="codeTemplate">模板</param>
    ///// <param name="tableinfo">实体数据信息</param>
    ///// <returns></returns>
    //private static string GetTemplateValue(CG_CodeTemplate codeTemplate, object tableinfo)
    //{
    //    try
    //    {
    //        AddDefaultTemplate(codeTemplate);

    //        //模板的完整路径
    //        var codeTemplateRootPath = "";
    //        //if (App.WebHostEnvironment.IsDevelopment() == false)
    //        //{
    //        //    codeTemplateRootPath = AppContext.BaseDirectory.TrimEnd('\\') + $"\\CodeTemplate\\{codeTemplate.Type.ToString()}\\{codeTemplate.TemplateName}";
    //        //}
    //        //else
    //        //{
    //        codeTemplateRootPath = App.WebHostEnvironment.ContentRootPath.TrimEnd('\\') + $"\\CodeTemplate\\{codeTemplate.Type.ToString()}\\{codeTemplate.TemplateName}";
    //        //}

    //        if (FileHelper.IsExistFile(codeTemplateRootPath))
    //        {
    //            var templatecontent = FileHelper.FileToString(codeTemplateRootPath, System.Text.Encoding.UTF8);
    //            //var result = templatecontent.RunCompile(model);

    //            //RazorEngine 文档地址：https://antaris.github.io/RazorEngine/index.html

    //            ////创建配置
    //            //var config = new TemplateServiceConfiguration();
    //            //config.Language = Language.CSharp; // VB.NET as template language.
    //            //config.EncodedStringFactory = new RawStringFactory(); // Raw string encoding. 可解决输出的html字符是编码后的
    //            //                                                      //config.EncodedStringFactory = new HtmlEncodedStringFactory(); // Html encoding.

    //            ////根据配置创建服务
    //            //var service = RazorEngineService.Create(config);

    //            //添加模板缓存
    //            var name = $"{codeTemplate.TemplateName}_{((CG_CodeTableInfo)tableinfo).ClassName}";
    //            //验证模板缓存是否存在
    //            if (RazorHelper.CustomerEngine().IsTemplateCached(name, null) == false)
    //            {
    //                RazorHelper.CustomerEngine().AddTemplate(name, templatecontent);
    //            }
    //            //运行编译
    //            var result = RazorHelper.CustomerEngine().RunCompile(name: name, tableinfo.GetType(), tableinfo);

    //            return result;
    //        }
    //        else
    //        {
    //            return "//没有找到模板";
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        SerilogEx.GetLogger("CodeGenerate").Error(ex.ToStringEx());
    //        throw Oops.Bah(ex.Message);
    //    }
    //}

    #endregion RazorEngine 暂时先不用了，用Furion的ViewEngine

    /// <summary>
    ///  获取 Entity 名称
    /// </summary>
    /// <param name="dbTableName">数据库表名</param>
    /// <param name="str">Sys_,替换的前缀</param>
    /// <returns></returns>
    private static string GetDbTableNoPrefixEntity(string dbTableName, string str)
    {
        string s = dbTableName;//取到表名

        var arr = str.Split(',');
        foreach (var item in arr)
        {
            var kv = item.Split('=');
            if (kv.Length == 2)
            {
                var pre = kv[0].Trim();
                var replace = kv[1].Trim();
                if (string.IsNullOrWhiteSpace(pre)) continue; //被替换的部分为空，跳过
                s = s.Replace(pre, replace);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(item)) continue; //被替换的部分为空，跳过
                s = s.Replace(item, "");
            }
        }
        return ToPascal(s);
    }

    /// <summary>
    ///  获取 Entity 名称
    /// </summary>
    /// <param name="dbTableName">数据库表名</param>
    /// <param name="str">Sys_=Sys,前缀=替换</param>
    /// <returns></returns>
    private static string GetDbTableReplace(string dbTableName, string str)
    {
        string s = dbTableName;//取到表名

        var arr = str.Split(',');
        foreach (var item in arr)
        {
            var kv = item.Split('=');
            if (kv.Length != 2) continue;
            var pre = kv[0].Trim();
            var replace = kv[1].Trim();
            if (string.IsNullOrWhiteSpace(pre)) continue; //被替换的部分为空，跳过

            s = s.Replace(pre, replace);
        }
        return ToPascal(s);
    }

    //Pascal命名法（将首字母大写）
    private static string ToPascal(string str)
    {
        return str.Substring(0, 1).ToUpper() + str.Substring(1);
    }

    //骆驼命名法（将首字母小写）
    private static string ToCamel(string s)
    {
        return s.Substring(0, 1).ToLower() + s.Substring(1);
    }

    //将整个字符串小写
    private static string ToLower(string s)
    {
        return s.ToLower();
    }

    private static bool IsSpecialType(CG_CodeColumnsInfo column)
    {
        return Regex.IsMatch(column.ClassProperName, @"\[.+\]");
    }

    private static string GetType(CG_CodeColumnsInfo column)
    {
        string type = "string";
        if (IsSpecialType(column))
        {
            type = Regex.Match(column.ClassProperName, @"\[(.+)\]").Groups[1].Value;
        }

        return type;
    }

    #region Common

    /// <summary>
    /// 获取CodeType
    /// 【此数据从Sqlsugar的webfirst的数据库中复制出来的】
    /// </summary>
    /// <returns></returns>
    private static List<CG_CodeType> GetCodeTypes()
    {
        var json = """
             [
            	{
            		"Id" : 1,
            		"Name" : "int",
            		"CSharepType" : "int",
            		"DbTypeStr" : "[{\"Name\":\"int\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"int4\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"number\",\"Length\":9,\"DecimalDigits\":0},{\"Name\":\"integer\",\"Length\":null,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 2,
            		"Name" : "string10",
            		"CSharepType" : "string",
            		"DbTypeStr" : "[{\"Name\":\"varchar\",\"Length\":10,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 3,
            		"Name" : "ignore",
            		"CSharepType" : "建表忽略该类型字段，生成实体中@Model.IsIgnore 值为 true ",
            		"DbTypeStr" : "[]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 4,
            		"Name" : "string36",
            		"CSharepType" : "string",
            		"DbTypeStr" : "[{\"Name\":\"varchar\",\"Length\":36,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 5,
            		"Name" : "string100",
            		"CSharepType" : "string",
            		"DbTypeStr" : "[{\"Name\":\"varchar\",\"Length\":100,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 6,
            		"Name" : "string200",
            		"CSharepType" : "string",
            		"DbTypeStr" : "[{\"Name\":\"varchar\",\"Length\":200,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 7,
            		"Name" : "string500",
            		"CSharepType" : "string",
            		"DbTypeStr" : "[{\"Name\":\"varchar\",\"Length\":500,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 8,
            		"Name" : "string2000",
            		"CSharepType" : "string",
            		"DbTypeStr" : "[{\"Name\":\"varchar\",\"Length\":2000,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 9,
            		"Name" : "nString10",
            		"CSharepType" : "string",
            		"DbTypeStr" : "[{\"Name\":\"nvarchar\",\"Length\":10,\"DecimalDigits\":null},{\"Name\":\"varchar\",\"Length\":10,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 10,
            		"Name" : "nString36",
            		"CSharepType" : "string",
            		"DbTypeStr" : "[{\"Name\":\"nvarchar\",\"Length\":36,\"DecimalDigits\":null},{\"Name\":\"varchar\",\"Length\":36,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 11,
            		"Name" : "nString100",
            		"CSharepType" : "string",
            		"DbTypeStr" : "[{\"Name\":\"nvarchar\",\"Length\":100,\"DecimalDigits\":null},{\"Name\":\"varchar\",\"Length\":100,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 12,
            		"Name" : "nString200",
            		"CSharepType" : "string",
            		"DbTypeStr" : "[{\"Name\":\"nvarchar\",\"Length\":200,\"DecimalDigits\":null},{\"Name\":\"varchar\",\"Length\":200,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 13,
            		"Name" : "nString500",
            		"CSharepType" : "string",
            		"DbTypeStr" : "[{\"Name\":\"nvarchar\",\"Length\":500,\"DecimalDigits\":null},{\"Name\":\"varchar\",\"Length\":500,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 14,
            		"Name" : "nString2000",
            		"CSharepType" : "string",
            		"DbTypeStr" : "[{\"Name\":\"nvarchar\",\"Length\":2000,\"DecimalDigits\":null},{\"Name\":\"varchar\",\"Length\":2000,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 15,
            		"Name" : "maxString",
            		"CSharepType" : "string",
            		"DbTypeStr" : "[{\"Name\":\"text\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"clob\",\"Length\":null,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 16,
            		"Name" : "bool",
            		"CSharepType" : "bool",
            		"DbTypeStr" : "[{\"Name\":\"bit\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"number\",\"Length\":1,\"DecimalDigits\":null},{\"Name\":\"boolean\",\"Length\":null,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 17,
            		"Name" : "DateTime",
            		"CSharepType" : "DateTime",
            		"DbTypeStr" : "[{\"Name\":\"datetime\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"date\",\"Length\":null,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 18,
            		"Name" : "timestamp",
            		"CSharepType" : "byte[]",
            		"DbTypeStr" : "[{\"Name\":\"timestamp\",\"Length\":null,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 19,
            		"Name" : "decimal_18_8",
            		"CSharepType" : "decimal",
            		"DbTypeStr" : "[{\"Name\":\"decimal\",\"Length\":18,\"DecimalDigits\":8},{\"Name\":\"number\",\"Length\":18,\"DecimalDigits\":8},{\"Name\":\"numeric\",\"Length\":18,\"DecimalDigits\":8}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 20,
            		"Name" : "decimal_18_4",
            		"CSharepType" : "decimal",
            		"DbTypeStr" : "[{\"Name\":\"decimal\",\"Length\":18,\"DecimalDigits\":4},{\"Name\":\"number\",\"Length\":18,\"DecimalDigits\":4},{\"Name\":\"money\",\"Length\":0,\"DecimalDigits\":0}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 21,
            		"Name" : "decimal_18_2",
            		"CSharepType" : "decimal",
            		"DbTypeStr" : "[{\"Name\":\"decimal\",\"Length\":18,\"DecimalDigits\":2},{\"Name\":\"number\",\"Length\":18,\"DecimalDigits\":2}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 22,
            		"Name" : "guid",
            		"CSharepType" : "Guid",
            		"DbTypeStr" : "[{\"Name\":\"uniqueidentifier\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"guid\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"char\",\"Length\":36,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 23,
            		"Name" : "byte",
            		"CSharepType" : "byte",
            		"DbTypeStr" : "[{\"Name\":\"tinyint\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"varbit\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"number\",\"Length\":3,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 24,
            		"Name" : "short",
            		"CSharepType" : "short",
            		"DbTypeStr" : "[{\"Name\":\"short\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"int2\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"int16\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"smallint\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"number\",\"Length\":5,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 25,
            		"Name" : "long",
            		"CSharepType" : "long",
            		"DbTypeStr" : "[{\"Name\":\"long\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"int8\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"int64\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"bigint\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"number\",\"Length\":19,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 26,
            		"Name" : "byteArray",
            		"CSharepType" : "byte[]",
            		"DbTypeStr" : "[{\"Name\":\"blob\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"longblob\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"binary\",\"Length\":null,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 27,
            		"Name" : "datetimeoffset",
            		"CSharepType" : "DateTimeOffset",
            		"DbTypeStr" : "[{\"Name\":\"datetimeoffset\",\"Length\":null,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 28,
            		"Name" : "json_default",
            		"CSharepType" : "object",
            		"DbTypeStr" : "[{\"Name\":\"json\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"varchar\",\"Length\":3999,\"DecimalDigits\":null}]",
            		"Sort" : 1000
            	},
            	{
            		"Id" : 29,
            		"Name" : "string_char10",
            		"CSharepType" : "string",
            		"DbTypeStr" : "[{\"Name\":\"char\",\"Length\":10,\"DecimalDigits\":null}]",
            		"Sort" : 10000
            	},
            	{
            		"Id" : 30,
            		"Name" : "float",
            		"CSharepType" : "decimal",
            		"DbTypeStr" : "[{\"Name\":\"float\",\"Length\":null,\"DecimalDigits\":null}]",
            		"Sort" : 10000
            	},
            	{
            		"Id" : 31,
            		"Name" : "Extension_Time_20220407",
            		"CSharepType" : "DateTime",
            		"DbTypeStr" : "[{\"Name\":\"date\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"time\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"timestamp with time zone\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"timestamptz\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"timestamp without time zone\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"time with time zone\",\"Length\":null,\"DecimalDigits\":null}]",
            		"Sort" : 10000
            	},
            	{
            		"Id" : 32,
            		"Name" : "Extension_Bool_20220407",
            		"CSharepType" : "bool",
            		"DbTypeStr" : "[{\"Name\":\"bool\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"Boolean\",\"Length\":null,\"DecimalDigits\":null}]",
            		"Sort" : 10000
            	},
            	{
            		"Id" : 33,
            		"Name" : "Extension_Decimal_20220407",
            		"CSharepType" : "decimal",
            		"DbTypeStr" : "[{\"Name\":\"float4\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"float8\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"interval\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"lseg\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"macaddr\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"path\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"point\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"polygon\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"double precision\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"real\",\"Length\":null,\"DecimalDigits\":null}]",
            		"Sort" : 10000
            	},
            	{
            		"Id" : 34,
            		"Name" : "Extension_Byte_20220407",
            		"CSharepType" : "byte",
            		"DbTypeStr" : "[{\"Name\":\"varbit\",\"Length\":null,\"DecimalDigits\":null}]",
            		"Sort" : 10000
            	},
            	{
            		"Id" : 35,
            		"Name" : "Extension_Guid_20220407",
            		"CSharepType" : "guid",
            		"DbTypeStr" : "[{\"Name\":\"uuid\",\"Length\":null,\"DecimalDigits\":null}]",
            		"Sort" : 10000
            	}
            ]

            """;
        var list = json.ToObj2<List<CG_CodeType>>();

        foreach (var item in list)
        {
            item.DbType = item.DbTypeStr.ToObj2<List<DbTypeInfo>>();
        }

        return list;
    }

    /// <summary>
    /// 获取实体类型信息
    /// </summary>
    /// <param name="types"></param>
    /// <param name="columnInfo"></param>
    /// <param name="dbtype"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static SortTypeInfo GetEntityType(List<CG_CodeType> types, DbColumnInfo columnInfo, SqlSugar.DbType dbtype)
    {
        var typeInfo = types.FirstOrDefault(y => y.DbType.Any(it => it.Name.Equals(columnInfo.DataType, StringComparison.OrdinalIgnoreCase)));
        if (typeInfo == null && (columnInfo.DataType + "").ToLower() == "double")
        {
            var type = types.First(it => it.Name == "decimal_18_4");
            return new SortTypeInfo() { CodeType = type, DbTypeInfo = type.DbType[0] };
        }
        else if (typeInfo == null)
        {
            var type = types.First(it => it.Name == "string100");
            return new SortTypeInfo() { CodeType = type, DbTypeInfo = type.DbType[0] };
        }
        else if (columnInfo.DataType == "json")
        {
            var type = types.First(it => it.Name.ToLower() == "string2000");
            return new SortTypeInfo() { CodeType = type, DbTypeInfo = type.DbType[0] };
        }
        else if (columnInfo.DataType == "timestamp" && dbtype != SqlSugar.DbType.SqlServer)
        {
            var type = types.First(it => it.Name.ToLower() == "datetime");
            return new SortTypeInfo() { CodeType = type, DbTypeInfo = type.DbType[0] };
        }
        else
        {
            List<SortTypeInfo> SortTypeInfoList = new List<SortTypeInfo>();
            foreach (var type in types)
            {
                foreach (var cstype in type.DbType)
                {
                    SortTypeInfo item = new SortTypeInfo();
                    item.DbTypeInfo = cstype;
                    item.CodeType = type;
                    item.Sort = GetSort(cstype, type, columnInfo, dbtype);
                    SortTypeInfoList.Add(item);
                }
            }
            var result = SortTypeInfoList.Where(it => it.CodeType.Name != "json_default").OrderByDescending(it => it.Sort).FirstOrDefault();
            if (result == null)
            {
                throw new Exception($"没有匹配到类型{columnInfo.DataType} 来自 {columnInfo.TableName} 表 {columnInfo.DbColumnName} ，请到类型管理添加");
            }
            if (result.CodeType.Name == "guid" && columnInfo.DataType == "char" && columnInfo.Length != 36)
            {
                result = SortTypeInfoList.FirstOrDefault(it => it.CodeType.Name == "string100");
            }
            if (dbtype == SqlSugar.DbType.PostgreSQL)
            {
                if (columnInfo.DataType == "bit")
                {
                    var type = types.First(it => it.Name.ToLower() == "bytearray");
                    return new SortTypeInfo() { CodeType = type, DbTypeInfo = type.DbType[0] };
                }
            }
            return result;
        }
    }

    /// <summary>
    /// 匹配出最符合的类型
    /// </summary>
    /// <param name="cstype"></param>
    /// <param name="type"></param>
    /// <param name="columnInfo"></param>
    /// <param name="dbtype"></param>
    /// <returns></returns>
    private static decimal GetSort(DbTypeInfo cstype, CG_CodeType type, DbColumnInfo columnInfo, SqlSugar.DbType dbtype)
    {
        decimal result = 0;
        if (columnInfo.DataType.Equals(cstype.Name, StringComparison.OrdinalIgnoreCase))
        {
            result = result + 10000;
        }
        else
        {
            result = result - 30000;
        }
        if (columnInfo.Length == Convert.ToInt32(cstype.Length))
        {
            result = result + 5000;
        }
        else if (columnInfo.Length > Convert.ToInt32(cstype.Length))
        {
            result = result + (columnInfo.Length - Convert.ToInt32(cstype.Length)) * -3;
        }
        else
        {
            result = result - 500;
        }
        if (columnInfo.DecimalDigits == Convert.ToInt32(cstype.DecimalDigits))
        {
            result = result + 5000;
        }
        else if (columnInfo.DecimalDigits > Convert.ToInt32(cstype.DecimalDigits))
        {
            result = result + (columnInfo.DecimalDigits - Convert.ToInt32(cstype.DecimalDigits)) * -3;
        }
        else
        {
            result = result - 500;
        }
        if (type.Name.Contains("nString") && columnInfo.DataType == "varchar")
        {
            result = result - 500;
        }
        return result;
    }

    /// <summary>
    /// 排序计算MODEL
    /// </summary>
    public class SortTypeInfo
    {
        public DbTypeInfo DbTypeInfo { get; set; }
        public decimal Sort { get; set; }
        public CG_CodeType CodeType { get; set; }
    }

    /// <summary>
    /// 获取类名
    /// </summary>
    /// <param name="dbColumnName"></param>
    /// <returns></returns>
    private static string GetCsharpName(string dbColumnName)
    {
        if (dbColumnName.Contains("_"))
        {
            dbColumnName = dbColumnName.TrimEnd('_');
            dbColumnName = dbColumnName.TrimStart('_');
            var array = dbColumnName.Split('_').Select(it => GetFirstUpper(it, true)).ToArray();
            return string.Join("", array);
        }
        else
        {
            return GetFirstUpper(dbColumnName);
        }
    }

    /// <summary>
    /// 获取第一个字母大写
    /// </summary>
    /// <param name="dbColumnName"></param>
    /// <param name="islower"></param>
    /// <returns></returns>
    private static string GetFirstUpper(string dbColumnName, bool islower = false)
    {
        if (dbColumnName == null)
            return null;
        if (islower)
        {
            return dbColumnName.Substring(0, 1).ToUpper() + dbColumnName.Substring(1).ToLower();
        }
        else
        {
            if (dbColumnName.ToUpper() == dbColumnName)
            {
                return dbColumnName.Substring(0, 1).ToUpper() + dbColumnName.Substring(1).ToLower();
            }
            else
            {
                return dbColumnName.Substring(0, 1).ToUpper() + dbColumnName.Substring(1);
            }
        }
    }

    #endregion Common
}