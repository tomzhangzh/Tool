// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

public static class ObjectExtension
{
    /// <summary>
    /// 检查 Object 是否为 NULL
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsEmpty(this object value)
    {
        return value == null || string.IsNullOrEmpty(value.ParseToString());
    }

    /// <summary>
    /// 检查 Object 是否为 NULL 或者 0
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsNullOrZero(this object value)
    {
        return value == null || value.ParseToString().Trim() == "0";
    }

    /// <summary>
    /// 检查是否为 AJAX 请求
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static bool IsAjaxRequest(this HttpRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.Headers != null)
            return request.Headers["X-Requested-With"] == "XMLHttpRequest";
        return false;
    }

    /// <summary>
    /// 将object转换为string，若转换失败，则返回""。不抛出异常。
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string ParseToString(this object obj)
    {
        try
        {
            return obj == null ? string.Empty : obj.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 判断类型是否实现某个泛型
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="generic">泛型类型</param>
    /// <returns>bool</returns>
    public static bool HasImplementedRawGeneric(this Type type, Type generic)
    {
        // 检查接口类型
        var isTheRawGenericType = type.GetInterfaces().Any(IsTheRawGenericType);
        if (isTheRawGenericType) return true;

        // 检查类型
        while (type != null && type != typeof(object))
        {
            isTheRawGenericType = IsTheRawGenericType(type);
            if (isTheRawGenericType) return true;
            type = type.BaseType;
        }

        return false;

        // 判断逻辑
        bool IsTheRawGenericType(Type type) => generic == (type.IsGenericType ? type.GetGenericTypeDefinition() : type);
    }
     
    /// <summary>
    /// 去掉类中所有字符串类型的首尾空格
    /// </summary>
    public static void TrimClassStringProperties<T>(T model)
    {
        if(model==null) return;
        Type t = model.GetType();
        PropertyInfo[] PropertyList = t.GetProperties();
        foreach (PropertyInfo item in PropertyList)
        {
            if(item==null) continue;
            string name = item.Name;
            if(item.PropertyType.Equals(typeof(String))&& item.GetValue(model, null) != null)
            {
                var value = item.GetValue(model, null)?.ToString();
                if(!string.IsNullOrWhiteSpace(value))
                {
                   item.SetValue(model, value.Trim());
                }
            }
          
        }
    }
}