// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

public static class EntityExtensions
{
    #region 数据库扩展字段，存储 Dictionary<string,T>  的操作

    /// <summary>
    /// 获取指定Key的数据
    /// </summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? GetValue<T>(this string data, string key, T? defaultValue = default(T))
    {
        //if (string.IsNullOrEmpty(data)) throw new ArgumentException($"“{nameof(data)}”不能为 null 或空。", nameof(data));
        if (string.IsNullOrEmpty(key)) throw new ArgumentException($"“{nameof(key)}”不能为 null 或空。", nameof(key));
        try
        {
            var dic = data.ToObj2<Dictionary<string, object>>();
            if (dic == null) return defaultValue;

            var state = dic.TryGetValue(key, out var value);
            if (state)
            {
                return value.To<T>();
            }
            else
            {
                return defaultValue;
            }
        }
        catch (Exception)
        {
            return default(T);
        }
    }

    /// <summary>
    /// 获取指定Key的数据
    /// </summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string? GetValue(this string data, string key)
    {
        return GetValue<string>(data, key);
    }

    /// <summary>
    /// 获取指定Key的数据
    /// </summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? GetObject<T>(this string data, string key)
    {
        try
        {
            if (data == null) return default(T);
            if (string.IsNullOrWhiteSpace(data)) return default(T);
            var dic = data.ToObj2<Dictionary<string, string>>();
            if (dic == null) return default(T);

            var state = dic.TryGetValue(key, out var value);
            if (state && value != null)
            {
                return value.ToObj<T>();
            }
            else
            {
                return default(T);
            }
        }
        catch (Exception)
        {
            return default(T);
        }
    }

    /// <summary>
    /// 获取指定Key的数据
    /// </summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string? GetObject(this string data, string key)
    {
        return GetObject<string>(data, key);
    }

    /// <summary>
    /// 设置数据到 集合中去
    /// </summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string? SetData(this string data, string key, string value)
    {
        var dic = (data ?? "{}").ToObj2<Dictionary<string, string>>();
        if (dic == null) dic = new Dictionary<string, string>();
        if (dic.ContainsKey(key))
        {
            dic[key] = value;
        }
        else
        {
            dic.Add(key, value);
        }
        return dic.ToJson();
    }

    #endregion 数据库扩展字段，存储 Dictionary<string,T>  的操作

    #region 对象属性扩展

    /// <summary>
    /// 对class属性设置值
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="propName">属性名称(字段名)</param>
    /// <param name="value">设置的值</param>
    public static void FieldSetValue(this object obj, string propName, object value)
    {
        var t = obj.GetType();
        var info = t.GetProperty(propName);
        if (info == null)
            return;
        if (!info.CanWrite)
            return;
        if (info.PropertyType == typeof(string))
        {
            info.SetValue(obj, value, null);
        }
        else if (info.PropertyType == typeof(DateTimeOffset))
        {
            info.SetValue(obj, value.To<DateTimeOffset>(), null);
        }
        else if (info.PropertyType == typeof(DateTime))
        {
            info.SetValue(obj, value.To<DateTime>(), null);
        }
        else if (info.PropertyType == typeof(int))
        {
            info.SetValue(obj, value.To<int>(), null);
        }
        else if (info.PropertyType == typeof(double))
        {
            info.SetValue(obj, value.To<double>(), null);
        }
        else if (info.PropertyType == typeof(decimal))
        {
            info.SetValue(obj, value.To<decimal>(), null);
        }
        else if (info.PropertyType == typeof(bool))
        {
            info.SetValue(obj, value.To<bool>(), null);
        }
        else
        {
            try
            {
                info.SetValue(obj, value, null);
            }
            catch (Exception)
            {
            }
        }
    }

    ///// <summary>
    ///// 获取对象的属性名，属性值、描述
    ///// </summary>
    ///// <typeparam name="T">对象类型</typeparam>
    ///// <param name="obj">对象</param>
    ///// <returns></returns>
    //public static Dictionary<string, Dictionary<string, string>> GetPropertyElements(this object obj)
    //{
    //    var dic = new Dictionary<string, Dictionary<string, string>>();
    //    foreach (var property in obj.GetType().GetProperties())
    //    {
    //        var fieldvalue = "";
    //        if (property.PropertyType.BaseType == typeof(System.Enum))
    //        {
    //            fieldvalue = ((int)(property.GetValue(obj, null) ?? 0)).ToString();
    //        }
    //        else
    //        {
    //            fieldvalue = property.GetValue(obj, null)?.ToString();
    //        }
    //        var description = "";
    //        var customattributes = property.GetCustomAttributes(false);
    //        if (customattributes != null && customattributes.Any())
    //        {
    //            var customattribute = customattributes.First() as DescriptionAttribute;
    //            description = customattribute.Description;
    //        }
    //        // var description = property.GetCustomAttributes(false).FirstOrDefault();
    //        dic.Add(property.Name, new Dictionary<string, string>()
    //        {
    //           { "Value",fieldvalue ?? ""},
    //           { "Description",description}
    //        });

    //        // { Value = fieldvalue ?? "", Description = description });
    //    }

    //    return dic;
    //}

    /// <summary>
    /// 获取指定class的字段值
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="obj">class对象</param>
    /// <param name="fieldName">字段名</param>
    /// <returns></returns>
    public static TValue? GetFieldValue<TValue>(this object obj, string fieldName)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));
        if (fieldName is null) throw new ArgumentNullException(nameof(fieldName));
        try
        {
            Type Ts = obj.GetType();
            if (Ts == null) return default(TValue);
            object? o = Ts.GetProperty(fieldName)?.GetValue(obj, null);
            //string Value = Convert.ToString(o);
            //if (string.IsNullOrEmpty(Value)) return null;
            //return Value;
            return o.To<TValue>() ?? default(TValue);
        }
        catch
        {
        }
        return default(TValue);
    }

    /// <summary>
    /// 获取对象的属性名，属性值、描述
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="obj">对象</param>
    /// <returns></returns>
    public static Dictionary<string, EntityInfo> GetDictionaryEntityInfo(this object obj)
    {
        Dictionary<string, EntityInfo> dic = new Dictionary<string, EntityInfo>();
        foreach (var property in obj.GetType().GetProperties())
        {
            var fieldvalue = "";
            if (property.PropertyType.BaseType == typeof(System.Enum))
            {
                fieldvalue = ((int)(property.GetValue(obj, null) ?? 0)).ToString();
            }
            else
            {
                fieldvalue = property.GetValue(obj, null)?.ToString();
            }
            var description = "";
            var customattributes = property.GetCustomAttributes(false);
            if (customattributes != null && customattributes.Any()&& customattributes.Any(o=>o is DescriptionAttribute))
            {
                var attr = customattributes.First() as DescriptionAttribute;
                if (attr != null)
                {
                    description = attr.Description;
                }
            }
            // var description = property.GetCustomAttributes(false).FirstOrDefault();

            var methodinfo = new EntityInfo();
            methodinfo.Filed = property.Name;
            methodinfo.Value = fieldvalue ?? "";
            methodinfo.Description = description;
            dic.Add(property.Name, methodinfo);
            //dic.Add(property.Name, new Dictionary<string, string>()
            //{
            //   { "Value",fieldvalue ?? ""},
            //   { "Description",description}
            //});

            // { Value = fieldvalue ?? "", Description = description });
        }

        return dic;
    }

    /// <summary>
    /// 获取对象的属性名，属性值
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="obj">对象</param>
    /// <returns></returns>
    public static Dictionary<string, TValue> GetDictionary<TValue>(this object obj)
    {
        if (obj == null) return new Dictionary<string, TValue>();
        var dic = new Dictionary<string, TValue>();
        foreach (var property in obj.GetType().GetProperties())
        {
            if (property.Name == null) continue;
            // var fieldvalue = "";

            //if (property.PropertyType.BaseType == typeof(System.Enum))
            //{
            //    fieldvalue = ((int)(property.GetValue(obj, null) ?? 0)).ToString();
            //}
            //else
            //{
            //    fieldvalue = property.GetValue(obj, null)?.ToString();
            //}

            //Type targetType = typeof(TValue);

            var fieldvalue = property.GetValue(obj, null);
            if (fieldvalue != null && fieldvalue.GetType().BaseType == typeof(System.Enum))
            {
                dic.Add(property.Name, ((int)fieldvalue).To<TValue>());
            }
            else
            {
                dic.Add(property.Name, fieldvalue.To<TValue>());
            }
        }
        return dic;
    }

    /// <summary>
    /// 获取对象的属性名，属性值
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="obj">对象</param>
    /// <returns></returns>
    public static SortedDictionary<string, string> GetSortedDictionary(this object obj)
    {
        var dic = obj.GetDictionary<string>();
        var sortDic = new SortedDictionary<string, string>();
        foreach (var item in dic)
        {
            sortDic.Add(item.Key, item.Value);
        }
        return sortDic;
    }

    /// <summary>
    /// 获取对象的属性名，属性值
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="obj">对象</param>
    /// <returns></returns>
    public static Dictionary<string, string> GetDictionary(this object obj)
    {
        return obj.GetDictionary<string>();
    }

    #endregion 对象属性扩展

    /// <summary>
    /// 获取class指定字段的描述（ Description）
    /// </summary>
    /// <param name="obj">class</param>
    /// <param name="fieldName">field:使用 nameof(字段名)</param>
    /// <returns>string</returns>
    public static string GetClassFieldDescription(this object obj, string fieldName)
    {
        AttributeCollection attributes = TypeDescriptor.GetProperties(obj)[fieldName].Attributes;
        DescriptionAttribute attribute = (DescriptionAttribute)attributes[typeof(DescriptionAttribute)];

        return attribute.Description;
    }

    /// <summary>
    /// 通过枚举值 获取枚举的 描述（Description）
    /// </summary>
    /// <param name="enumValue"></param>
    /// <returns>string</returns>
    public static string GetDescriptionByEnum(this Enum enumValue)
    {
        string value = enumValue.ToString();
        System.Reflection.FieldInfo field = enumValue.GetType().GetField(value);
        object[] objs = field.GetCustomAttributes(typeof(DescriptionAttribute), false);    //获取描述属性
        if (objs.Length == 0)    //当描述属性没有时，直接返回名称
            return value;
        DescriptionAttribute descriptionAttribute = (DescriptionAttribute)objs[0];
        return descriptionAttribute.Description;
    }

    // 调用示例
    //GetDescriptionByEnum(enumStudent.age) → 年龄

    /// <summary>
    /// 通过描述（Description）获取枚举值
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <param name="description">枚举值描述</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static T GetEnumByDescription<T>(this string description) where T : Enum
    {
        System.Reflection.FieldInfo[] fields = typeof(T).GetFields();
        foreach (System.Reflection.FieldInfo field in fields)
        {
            object[] objs = field.GetCustomAttributes(typeof(DescriptionAttribute), false);    //获取描述属性
            if (objs.Length > 0 && (objs[0] as DescriptionAttribute).Description == description)
            {
                return (T)field.GetValue(null);
            }
        }

        throw new ArgumentException(string.Format("{0} 未能找到对应的枚举.", description), "Description");
    }

    // 调用示例
    //GetEnumByDescription<enumStudent>("性别").ToString() → sex

    #region summary 读取

    /// <summary>
    /// 获取Class 的注释
    /// </summary>
    /// <typeparam name="TClass"></typeparam>
    /// <param name="className"></param>
    /// <returns></returns>
    public static string GetClassComment<TClass>(this TClass className)
    {
        var xmlCommentHelper = new XmlCommentHelper();
        xmlCommentHelper.LoadAll();
        Type type = typeof(TClass);
        var typeComment = xmlCommentHelper.GetTypeComment(type);
        return typeComment;
    }

    /// <summary>
    /// 获取指定类中，指定字段的注释
    /// </summary>
    /// <typeparam name="TClass"></typeparam>
    /// <param name="className"></param>
    /// <param name="fieldName">字段名</param>
    /// <returns></returns>
    public static string GetFieldComment<TClass>(this TClass className, string fieldName)
    {
        Type type = typeof(TClass);
        //获取当前class 全部字段
        var fields = type.GetFields(bindingAttr: System.Reflection.BindingFlags.NonPublic);
        //取出指定字段
        var field = fields.FirstOrDefault(o => o.Name == fieldName);
        if (field == null) return "";
        var xmlCommentHelper = new XmlCommentHelper();
        xmlCommentHelper.LoadAll();
        var fieldComment = xmlCommentHelper.GetFieldOrPropertyComment(field);
        // Console.WriteLine($"{field.Name}字段的注释：{fieldComment}");
        return fieldComment;
    }

    /// <summary>
    /// 获取指定类中，指定方法的注释
    /// </summary>
    /// <typeparam name="TClass"></typeparam>
    /// <param name="className"></param>
    /// <param name="classMethodName">方法名</param>
    /// <returns></returns>
    public static string GetClassMethodComment<TClass>(this TClass className, string classMethodName)
    {
        Type type = typeof(TClass);

        var xmlCommentHelper = new XmlCommentHelper();
        xmlCommentHelper.LoadAll();

        var method = type.GetMethod(classMethodName);
        var methodComment = xmlCommentHelper.GetMethodComment(method);
        //Console.WriteLine($"{nameof(MyClass.MyMethod)}方法的注释：{methodComment}");

        return methodComment;
    }

    /// <summary>
    /// 获取Class 中方法的参数
    /// </summary>
    /// <typeparam name="TClass"></typeparam>
    /// <param name="className"></param>
    /// <param name="classMethodName">方法名</param>
    /// <returns></returns>
    public static Dictionary<string, string> GetClassMethodPars<TClass>(this TClass className, string classMethodName)
    {
        Type type = typeof(TClass);

        var xmlCommentHelper = new XmlCommentHelper();
        xmlCommentHelper.LoadAll();

        var method = type.GetMethod(classMethodName);

        var dict = xmlCommentHelper.GetParameterComments(method);
        //foreach (var pair in dict)
        //{
        //    //Console.WriteLine($"{nameof(MyClass.MyMethod)}方法的参数{pair.Key}注释：{pair.Value}");
        //}
        return dict;
    }

    ///// <summary>
    ///// MyClass注释
    ///// </summary>
    //public class MyClass
    //{
    //    /// <summary>
    //    /// FieldName1字段注释
    //    /// </summary>
    //    int FieldName1;
    //    /// <summary>
    //    /// FieldName2字段注释
    //    /// </summary>
    //    string FieldName2;

    //    /// <summary>
    //    /// PropertyName1属性注释
    //    /// </summary>
    //    public int PropertyName1 { get; set; }
    //    /// <summary>
    //    /// PropertyName2属性注释
    //    /// </summary>
    //    public string PropertyName2 { get; set; }

    //    /// <summary>
    //    /// MyMethod方法注释
    //    /// </summary>
    //    /// <param name="intParameter">整型参数<see cref="int"/></param>
    //    /// <param name="stringParameter">字符串类型参数<see cref="string"/></param>
    //    /// <returns>返回值类型是字符串</returns>
    //    public string MyMethod(int intParameter, string stringParameter)
    //    {
    //        return string.Empty;
    //    }
    //}
    //static void Main(string[] args)
    //{
    //    var xmlCommentHelper = new XmlCommentHelper();
    //    xmlCommentHelper.LoadAll();

    //    Type type = typeof(MyClass);
    //    var typeComment = xmlCommentHelper.GetTypeComment(type);
    //    Console.WriteLine($"{type.Name}的注释：{typeComment}");

    //    var fields = type.GetFields(bindingAttr: System.Reflection.BindingFlags.NonPublic);
    //    foreach (var field in fields)
    //    {
    //        var fieldComment = xmlCommentHelper.GetFieldOrPropertyComment(field);
    //        Console.WriteLine($"{field.Name}字段的注释：{fieldComment}");
    //    }

    //    var properties = type.GetProperties();
    //    foreach (var property in properties)
    //    {
    //        var propertyComment = xmlCommentHelper.GetFieldOrPropertyComment(property);
    //        Console.WriteLine($"{property.Name}属性的注释：{propertyComment}");
    //    }

    //    var method = type.GetMethod(nameof(MyClass.MyMethod));
    //    var methodComment = xmlCommentHelper.GetMethodComment(method);
    //    Console.WriteLine($"{nameof(MyClass.MyMethod)}方法的注释：{methodComment}");

    //    var dict = xmlCommentHelper.GetParameterComments(method);
    //    foreach (var pair in dict)
    //    {
    //        Console.WriteLine($"{nameof(MyClass.MyMethod)}方法的参数{pair.Key}注释：{pair.Value}");
    //    }
    //}

    #endregion summary 读取
}

/// <summary>
/// 实体信息
/// </summary>
public class EntityInfo
{
    /// <summary>
    /// 字段名称
    /// </summary>
    public string Filed { get; set; }

    /// <summary>
    /// 值
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    public string Description { get; set; }
}