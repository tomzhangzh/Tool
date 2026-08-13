// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.Rendering;

public static class CustomerHtmlHelper
{
    /// <summary>
    /// layui-form-item
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="displayName">显示名称</param>
    /// <param name="fieldName">字段名</param>
    /// <param name="value">值</param>
    /// <param name="layVerfyName">laui 的验证参数名称</param>
    /// <param name="placeholder">提示信息</param>
    /// <returns></returns>
    public static IHtmlContent LayuiInput(this IHtmlHelper htmlHelper, string displayName, string fieldName, string value = "", string layVerfyName = "", string placeholder = "")
    {
        string content = "<div class=\"layui-form-item\">";
        content += $" <label class=\"layui-form-label\">{displayName}</label>";
        content += "<div class=\"layui-input-block\">";
        content += $"<input asp-for=\"{fieldName}\" lay-verify=\"{(string.IsNullOrWhiteSpace(layVerfyName) ? fieldName : layVerfyName)}\" autocomplete=\"off\" placeholder=\"{(string.IsNullOrWhiteSpace(placeholder) ? "请输入" + displayName : placeholder)}\" value=\"{value}\" class=\"layui-input\">";
        content += "</div>";
        //content+="<div class='layui-form-mid layui-word-aux'>格式 例如：用户列表</div>";
        content += "</div>";
        return new HtmlString(content);
    }

    /// <summary>
    /// layui-form-textarea
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="displayName">显示名称</param>
    /// <param name="fieldName">字段名</param>
    /// <param name="value">值</param>
    /// <param name="layVerfyName">laui 的验证参数名称</param>
    /// <param name="placeholder">提示信息</param>
    /// <returns></returns>
    public static IHtmlContent LayuiTextarea(this IHtmlHelper htmlHelper, string displayName, string fieldName, string value = "", string layVerfyName = "", string placeholder = "")
    {
        string content = "<div class=\"layui-form-item\">";
        content += $" <label class=\"layui-form-label\">{displayName}</label>";
        content += "<div class=\"layui-input-block\">";
        content += $"<textarea id=\"{fieldName}\" name=\"{fieldName}\" lay-verify=\"{(string.IsNullOrWhiteSpace(layVerfyName) ? fieldName : layVerfyName)}\" autocomplete=\"off\" placeholder=\"{(string.IsNullOrWhiteSpace(placeholder) ? "请输入" + displayName : placeholder)}\"  class=\"layui-textarea\">{value}</textarea>";
        content += "</div>";
        //content+="<div class='layui-form-mid layui-word-aux'>格式 例如：用户列表</div>";
        content += "</div>";
        return new HtmlString(content);
    }
}