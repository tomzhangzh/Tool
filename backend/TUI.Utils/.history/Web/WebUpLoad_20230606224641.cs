// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using System.Net.Http.Headers;

namespace TUI.Utils;

/// <summary>
/// web 文件上传
/// </summary>
public partial class WebUpLoad
{
    #region 文件上传2

    /// <summary>
    /// 单文件上传
    /// </summary>
    /// <param name="input">输入信息</param>
    /// <param name="file"></param>
    /// <returns></returns>
    public static async Task<WebUploadResult> FileUpload(WebUploadInput input, IFormFile file)
    {
        return await FileUploadItem(input, file);
    }

    /// <summary>
    /// 单文件上传
    /// </summary>
    /// <param name="input">输入信息</param>
    /// <returns></returns>
    public static async Task<WebUploadResult> FileUpload(WebUploadInput input)
    {
        var context = App.GetService<IHttpContextAccessor>().HttpContext;
        if (context.Request.Form.Files == null || context.Request.Form.Files.Count <= 0)
        {
            throw Oops.Bah("请先选择文件");
        }
        var file = context.Request.Form.Files[0];
        return await FileUploadItem(input, file);
    }

    /// <summary>
    /// 多个文件上传
    /// </summary>
    /// <param name="input">输入信息</param>
    /// <returns></returns>
    public static async Task<List<WebUploadResult>> FileUploads(WebUploadInput input)
    {
        var context = App.GetService<IHttpContextAccessor>().HttpContext;
        if (context.Request.Form.Files == null || context.Request.Form.Files.Count <= 0)
        {
            throw Oops.Bah("请先选择文件");
        }
        var resultdata = new List<WebUploadResult>();
        foreach (var file in context.Request.Form.Files)
        {
            try
            {
                var rd = await FileUploadItem(input, file);
                resultdata.Add(rd);
            }
            catch (Exception ex)
            {
                if (file != null)
                {
                    resultdata.Add(new WebUploadResult() { uploadFilename = file.FileName, msg = ex.Message });
                }
            }
        }
        return resultdata;
    }

    /// <summary>
    /// 多个文件上传
    /// </summary>
    /// <param name="input">输入信息</param>
    /// <param name="files">文件集合</param>
    /// <returns></returns>
    public static async Task<List<WebUploadResult>> FileUploads(WebUploadInput input, List<IFormFile> files)
    {
        var resultdata = new List<WebUploadResult>();
        foreach (var file in files)
        {
            try
            {
                var rd = await FileUploadItem(input, file);
                resultdata.Add(rd);
            }
            catch (Exception ex)
            {
                if (file != null)
                {
                    resultdata.Add(new WebUploadResult() { uploadFilename = file.FileName, msg = ex.Message });
                }
            }
        }
        return resultdata;
    }

    /// <summary>
    /// 单文件上传
    /// </summary>
    /// <param name="input">输入信息</param>
    /// <returns></returns>
    private static async Task<WebUploadResult> FileUploadItem(WebUploadInput input, IFormFile file)
    {
        var webUploadOut = new WebUploadResult();

        //URL访问的逻辑路径 域名/逻辑路径/文件相对路径   默认:/u/f
        if (String.IsNullOrWhiteSpace(input.LogicalPath))
        {
            input.LogicalPath = App.Configuration["AppInfo:WebUploadLogicalPath"];
            //如果从配置里取出来还是空的，那就是设置一个默认的值
            if (string.IsNullOrWhiteSpace(input.LogicalPath))
            {
                input.LogicalPath = "/u/f";
            }
        }

        var context = App.GetService<IHttpContextAccessor>().HttpContext;

        var newfilepath = "";

        if (file == null && !string.IsNullOrEmpty(file.FileName))
        {
            throw Oops.Bah("请先选择文件");
        }

        #region 判断大小

        //imgFile.Length (单位字节)
        var maxlength = input.MaxFileSize * 1024 * 1024;//转换为字节
                                                        //long mb = imgFile.Length / 1024 / 1024; // MB
        if (file.Length > maxlength)
        {
            //resultdata.Errors = ;
            //return resultdata;
            throw Oops.Bah($"上传文件超过{input.MaxFileSize}MB");
        }
        webUploadOut.fileLength = file.Length;//字节

        #endregion 判断大小

        var filename = ContentDispositionHeaderValue
                        .Parse(file.ContentDisposition)
                        .FileName.Trim("").Trim("\"");

        var extname = filename[filename.LastIndexOf(".")..]; //扩展名，如.jpg
        webUploadOut.uploadFilename = filename;
        webUploadOut.extension = extname.ToLower();

        #region 文件类别判断

        //if (!extname.ToLower().Contains("jpg") && !extname.ToLower().Contains("png") && !extname.ToLower().Contains("gif"))
        //{
        //    return Json(new { code = 1, msg = "只允许上传jpg,png,gif格式的图片.", });
        //}
        switch (input.FileType)
        {
            case WebUploadType.所有:
                {
                    //if (!string.IsNullOrWhiteSpace(input.LimitExtensions) && !input.LimitExtensions.ToLower().Trim().Contains(extname.Trim().ToLower()))
                    //{
                    //    throw Oops.Bah($"文件上传格式错误，只允许上传{input.AllowExtensions}格式的文件！");
                    //}
                }
                break;

            case WebUploadType.自定义:
                {
                    if (!string.IsNullOrWhiteSpace(input.AllowExtensions) && !input.AllowExtensions.ToLower().Trim().Contains(extname.Trim().ToLower()))
                    {
                        throw Oops.Bah($"文件上传格式错误，只允许上传{input.AllowExtensions}格式的文件！");
                    }
                }
                break;

            case WebUploadType.图片:
                {
                    if (!string.IsNullOrWhiteSpace(input.AllowExtensions) && !input.AllowExtensions.ToLower().Trim().Contains(extname.Trim().ToLower()))
                    {
                        throw Oops.Bah($"文件上传格式错误，只允许上传{input.AllowExtensions}格式的图片！");
                    }
                    //验证是否图片
                    try
                    {
                        // System.Drawing.Image img = System.Drawing.Image.FromFile(file.FileName);
                        using (var memoryStream = new MemoryStream())
                        {
                            await file.CopyToAsync(memoryStream);
                            using (var img = System.Drawing.Image.FromStream(memoryStream))
                            {
                                // TODO: ResizeImage(img, 100, 100);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        throw Oops.Bah($"文件上传格式错误，只允许上传{input.AllowExtensions}格式的图片！");
                    }
                }
                break;

            case WebUploadType.Office文件:
                {
                    if (!string.IsNullOrWhiteSpace(input.AllowExtensions) && !input.AllowExtensions.ToLower().Trim().Contains(extname.Trim().ToLower()))
                    {
                        throw Oops.Bah($"文件上传格式错误，只允许上传{input.AllowExtensions}格式的文件！");
                    }
                }
                break;

            case WebUploadType.压缩包:
                {
                    if (!string.IsNullOrWhiteSpace(input.AllowExtensions) && !input.AllowExtensions.ToLower().Trim().Contains(extname.Trim().ToLower()))
                    {
                        throw Oops.Bah($"文件上传格式错误，只允许上传{input.AllowExtensions}格式的文件！");
                    }
                }
                break;

            default:
                break;
        }

        #endregion 文件类别判断

        var filename1 = "";//文件名
        //如果传入的文件名为空，则按 {DateTime.Now:yyyyMMddHHmmss}{RandomHelper.GetInt(1000, 9909)} 生成
        if (string.IsNullOrWhiteSpace(input.FileNamePrefix))
        {
            filename1 = $"{DateTime.Now:yyyyMMddHHmmss}{RandomHelper.GetInt(1000, 9909)}{extname}".ToLower();
        }
        else
        {
            //按：{input.FileName}_{DateTime.Now:yyyyMMddHHmmss}  生成
            filename1 = $"{input.FileNamePrefix}_{DateTime.Now:yyyyMMddHHmmss}{extname}".ToLower();
        }
        webUploadOut.newFilename = filename1;
        // 日期目录：yyyyMMdd
        string dir = DateTime.Now.ToString("yyyyMMdd");
        if (string.IsNullOrWhiteSpace(input.FloderName))
        {
            input.FloderName = "DefaultFiles";//设置默认文件夹
        }

        var path1 = "";//默认相对路径，不包含目录和文件名
        if (input.IsDateTimeFloder)
        {
            // upload/{typeName}/20210101
            path1 = Path.Combine(input.BaseFloder, input.FloderName.TrimStart('\\'), dir.TrimStart('\\'));
        }
        else
        {
            // upload/{typeName}
            path1 = Path.Combine(input.BaseFloder, input.FloderName.TrimStart('\\'));
        }

        //默认物理文件夹
        //var defaultPhysicalPath = App.Configuration["AppInfo:WebUploadFloder"];
        if (string.IsNullOrWhiteSpace(input.DefaultPhysicalPath) || !Directory.Exists(input.DefaultPhysicalPath))
        {
            //如果不存在，设置默认的物理文件，网站更目录下
            input.DefaultPhysicalPath = App.WebHostEnvironment.ContentRootPath;
        }

        //完整物理路径，注意要用分隔符
        var wuli_path = input.DefaultPhysicalPath + $"{Path.DirectorySeparatorChar}{path1}";
        if (!System.IO.Directory.Exists(wuli_path))
        {
            System.IO.Directory.CreateDirectory(wuli_path);
        }
        newfilepath = wuli_path + Path.DirectorySeparatorChar + filename1;

        using (FileStream fs = System.IO.File.Create(newfilepath))
        {
            await file.CopyToAsync(fs);
            fs.Flush();
        }
        webUploadOut.newFilePath = newfilepath;

        //是否使用逻辑路径  /u/f
        if (input.IsUseLogicalPath)
        {
            //默认文件夹：type=DefaultFiles
            //默认相对路径：/u/f/DefaultFiles/20220408/xxxx文件夹名.png
            //默认物理路径为：网站根目录/upload/DefaultFiles/20220408/xxxx文件夹名.png
            webUploadOut.src = HandlePathOrUrl($"{input.LogicalPath}\\{path1.TrimStart(input.BaseFloder)}\\{filename1.Replace("/", "\\")}");
            //默认：http://域名/u/f/DefaultFiles/20220408/xxxx文件夹名.png
            webUploadOut.httpsrc = context.GetDomain().TrimEnd('/') + "/" + HandlePathOrUrl($"{input.LogicalPath.TrimStart('/')}/{path1.TrimStart(input.BaseFloder)}/{filename1}");
        }
        else
        {
            //默认文件夹：type=DefaultFiles
            //默认相对路径：/u/f/DefaultFiles/20220408/xxxx文件夹名.png
            //默认物理路径为：网站根目录/upload/DefaultFiles/20220408/xxxx文件夹名.png
            webUploadOut.src = HandlePathOrUrl($"{path1}\\{filename1.Replace("/", "\\")}");
            //默认：http://域名/u/f/DefaultFiles/20220408/xxxx文件夹名.png
            webUploadOut.httpsrc = context.GetDomain().TrimEnd('/') + "/" + HandlePathOrUrl($"{path1.TrimStart('/')}/{filename1}");
        }

        webUploadOut.issuccess = true;//表示上传成功了

        return webUploadOut;
    }

    /// <summary>
    /// 处理路径或url地址
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    private static string HandlePathOrUrl(string str)
    {
        for (int i = 0; i < 3; i++)
        {
            str = str.Replace("\\\\", "/").Replace("\\", "/").Replace("//", "/");
        }
        return str;
    }

    #endregion 文件上传2
}

/// <summary>
/// web上传输入
/// </summary>
public class WebUploadInput
{
    public WebUploadInput()
    { }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="allowExtensions">允许扩展名</param>
    /// <param name="limitExtensions">限制扩展名（默认禁止 html/xml 扩展名上传）</param>
    /// <param name="fileType">文件上传类型</param>
    /// <param name="maxFileSize">最大文件大小(单位：MB),默认10MB</param>
    /// <param name="baseFloder">默认根目录文件夹：upload</param>
    /// <param name="FloderName">文件夹,type</param>
    /// <param name="fileNamePrefix">文件名前缀</param>
    /// <param name="IsDateTimeFloder">是否按日期创建文件夹</param>
    /// <param name="IsDateTimeFloder">是否使用逻辑路径  /u/f</param>
    /// <param name="logicalPath"> 默认文件输出逻辑路径</param>
    public WebUploadInput(string allowExtensions = "", string limitExtensions = ".html,.xml", WebUploadType fileType = WebUploadType.所有, int maxFileSize = 10, string baseFloder = "upload", string FloderName = "DefaultFiles", string fileNamePrefix = "", bool IsDateTimeFloder = true, bool isUseLogicalPath = true, string logicalPath = "/u/f")
    {
        this.AllowExtensions = allowExtensions;
        this.LimitExtensions = limitExtensions;
        this.FileType = fileType;
        this.MaxFileSize = maxFileSize;
        this.FloderName = FloderName;
        this.FileNamePrefix = fileNamePrefix;
        this.IsDateTimeFloder = IsDateTimeFloder;
        this.IsUseLogicalPath = isUseLogicalPath;
        this.LogicalPath = logicalPath;
    }

    /// <summary>
    /// 允许扩展名(如果为空，表示默认所有此类型扩展)
    /// </summary>
    public string AllowExtensions { get; set; }

    /// <summary>
    /// 限制扩展名(文件类型为全部，生效)
    /// 默认禁止 html/xml 扩展名上传
    /// </summary>
    public string LimitExtensions { get; set; } = ".html,.xml";

    /// <summary>
    /// 默认物理路径(网站根目录)
    /// </summary>
    public string DefaultPhysicalPath { get; set; } = AppEx.GetUploadDefaultFloder(); //App.WebHostEnvironment.ContentRootPath;

    /// <summary>
    /// 默认根目录文件夹：upload
    /// </summary>
    public string BaseFloder { get; set; } = "Upload";

    /// <summary>
    /// 文件上传类型
    /// </summary>
    public WebUploadType FileType { get; set; } = WebUploadType.所有;

    /// <summary>
    /// 最大文件大小(单位：MB)
    /// </summary>
    public int MaxFileSize { get; set; } = 10;

    /// <summary>
    /// 文件夹
    /// </summary>
    public string FloderName { get; set; } = "DefaultFiles";

    /// <summary>
    /// 默认文件输出逻辑路径
    /// URL访问的逻辑路径 域名/逻辑路径/文件相对路径
    /// 这样可以隐藏真正的服务器文件路径
    /// </summary>
    public string LogicalPath { get; set; } = "/u/f";

    /// <summary>
    /// 是否使用逻辑路径  /u/f
    /// </summary>
    public bool IsUseLogicalPath { get; set; } = true;

    /// <summary>
    /// 是否按日期创建文件夹
    /// 例如：FloderName\20220101\文件名
    /// </summary>
    public bool IsDateTimeFloder { get; set; } = true;

    /// <summary>
    /// 文件名前缀
    /// 默认按：{DateTime.Now:yyyyMMddHHmmss}{RandomHelper.GetInt(1000, 9909)
    /// </summary>
    public string FileNamePrefix { get; set; }
}

/// <summary>
/// web上传文件类型
/// </summary>
public enum WebUploadType
{
    所有 = 0,
    自定义 = 1,
    图片 = 2,
    Office文件 = 3,
    压缩包 = 4,
}

/// <summary>
/// Web 上传返回
/// </summary>
public class WebUploadResult
{
    /// <summary>
    /// 文件名
    /// </summary>
    public string uploadFilename { get; set; }

    /// <summary>
    /// 扩展名
    /// </summary>
    public string extension { get; set; }

    /// <summary>
    /// 新文件名
    /// </summary>
    public string newFilename { get; set; }

    /// <summary>
    /// 新文件路径
    /// </summary>
    public string newFilePath { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long fileLength { get; set; }

    /// <summary>
    /// 相对路径
    /// </summary>
    public string src { get; set; }

    /// <summary>
    /// http完整路径
    /// </summary>
    public string httpsrc { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool issuccess { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string msg { get; set; }

    /// <summary>
    /// 获取默认返回结果
    /// </summary>
    /// <returns></returns>
    public dynamic GetDefaultResult()
    {
        return new { src = this.src, httpsrc = this.httpsrc, issuccess = this.issuccess, msg = this.msg };
    }
}