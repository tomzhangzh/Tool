// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Furion.Localization;

namespace Abc.Utils;

public static class WebHelper
{
    /// <summary>
    /// 传入参数验证(表示验证请求结果的容器。)
    /// </summary>
    /// <param name="errorMessage">错误消息</param>
    /// <param name="memberNames">成员名称，例如： nameof(due_payment)</param>
    /// <param name="IsLocalization">是否全球化，默认true</param>
    /// <returns></returns>
    public static ValidationResult Validation(string errorMessage, IEnumerable<string> memberNames = null, bool IsLocalization = true)
    {
        if (IsLocalization)
        {
            return new ValidationResult(L.Text[errorMessage], memberNames);
        }
        else
        {
            return new ValidationResult(errorMessage, memberNames);
        }
    }
}