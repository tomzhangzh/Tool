// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Core.Utils.Web.Model;

public class WebHttpInfo
{
    /// <summary>
    /// 远程IP V4地址
    /// </summary>
    public string RemoteIPv4 { get; set; }

    /// <summary>
    /// 远程IP V6地址
    /// </summary>
    public string RemoteIPv6 { get; set; }

    /// <summary>
    /// 本地IP V4地址
    /// </summary>
    public string LocalIPv4 { get; set; }

    /// <summary>
    /// 本地IP V6地址
    /// </summary>
    public string LocalIPv6 { get; set; }

    /// <summary>
    /// 请求地址
    /// </summary>
    public string RequestUrl { get; set; }

    /// <summary>
    /// 来源 Url 地址
    /// </summary>
    public string RefererUrl { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    public string HttpMethod { get; set; }

    /// <summary>
    /// URL 参数
    /// </summary>
    public string UrlParams { get; set; }

    /// <summary>
    /// 请求参数
    /// </summary>
    public string Params { get; set; }

    /// <summary>
    /// 请求头
    /// </summary>
    public string Header { get; set; }

    /// <summary>
    /// 内容类型
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// 区域
    /// </summary>
    public string AreaName { get; set; }

    /// <summary>
    /// 控制器
    /// </summary>
    public string ControllerName { get; set; }

    /// <summary>
    /// 功能
    /// </summary>
    public string ActionName { get; set; }

    /// <summary>
    /// 系统
    /// </summary>
    public string Os { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    public string Browser { get; set; }

    /// <summary>
    /// 设备
    /// </summary>
    public string Device { get; set; }

    /// <summary>
    /// 客户端浏览器信息
    /// </summary>
    public string UserAgent { get; set; }

    /// <summary>
    /// 客户端请求区域语言
    /// </summary>
    public string AcceptLanguage { get; set; }

    /// <summary>
    /// 请求来源（swagger还是其他）
    /// </summary>
    public string RequestFrom { get; set; }

    /// <summary>
    /// 获取请求 cookies 信息
    /// </summary>
    public string RequestHeaderCookies { get; set; }

    /// <summary>
    /// 获取响应头信息 授权Token
    /// </summary>
    public string AccessToken { get; set; }

    /// <summary>
    /// 授权信息
    /// </summary>
    public string Authorization { get; set; }

    /// <summary>
    /// 获取响应 cookies 信息
    /// </summary>
    public string ResponseHeaderCookies { get; set; }

    #region 系统信息

    public string osDescription { get; set; }
    public string osArchitecture { get; set; }
    public string frameworkDescription { get; set; }
    public string basicFramework { get; set; }
    public string basicFrameworkVersion { get; set; }

    #endregion 系统信息

    /// <summary>
    /// 获取启动信息
    /// </summary>
    public string EntryAssemblyName { get; set; }

    /// <summary>
    /// 获取进程信息
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 获取部署程序
    /// </summary>
    public string DeployServer { get; set; }

    /// <summary>
    /// 服务器环境
    /// </summary>
    public string Environment { get; set; }
}