
using MathNet.Numerics.LinearAlgebra.Factorization;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using Pather.CSharp;

namespace TUI.Utils.Extensions
{
    /// <summary>
    /// 扩展方法类
    /// </summary>
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
        /// 去掉类中所有字符串类型的首尾空格
        /// </summary>
        public static void TrimClassStringProperties<T>(T model)
        {
            if (model == null) return;

            Type t = model.GetType();
            PropertyInfo[] PropertyList = t.GetProperties();

            foreach (PropertyInfo item in PropertyList)
            {
                if (item == null) continue;

                string name = item.Name;

                if (item.PropertyType.Equals(typeof(String)) && item.GetValue(model, null) != null)
                {
                    var value = item.GetValue(model, null)?.ToString();

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        item.SetValue(model, value.Trim());
                    }
                }
            }
        }

        
        /// <summary>
        /// 获取属性的自定义特性
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="source"></param>
        /// <param name="valueSelector"></param>
        /// <returns></returns>
        public static Dictionary<PropertyInfo, TValue> GetPropertiesAttributeValue<TAttribute, TValue>(this object source, Func<TAttribute, TValue> valueSelector) where TAttribute : Attribute
        {
            var properties = source.GetType().GetProperties();
            var query = from property in properties
                        let attribute = property.GetCustomAttribute<TAttribute>()
                        let value = (attribute == null ? default(TValue) : valueSelector(attribute))
                        where attribute != null
                        select new { Key = property, Value = value };

            return query.ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// 将对象转换为指定类型
        /// </summary>
        /// <param name="value"></param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
        public static object ChangeType(this object value, Type conversionType)
        {
            if (conversionType == null)
            {
                throw new ArgumentNullException(nameof(conversionType));
            }

            if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return null;
                }

                NullableConverter nullableConverter = new NullableConverter(conversionType);

                conversionType = nullableConverter.UnderlyingType;
            }

            return Convert.ChangeType(value, conversionType);
        }

        /// <summary>
        /// 将对象转换为指定类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T ChangeType<T>(this object value)
        {
            try
            {
                return (T)ChangeType(value, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// 克隆对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static T Clone<T>(this T source) where T : class, new()
        {
            var result = new T();
            foreach (var p in typeof(T).GetProperties())
            {
                if (p.PropertyType.IsEnum || p.PropertyType.BaseType != null && p.PropertyType.Namespace.StartsWith("Sunshine") == false && p.PropertyType.Namespace.StartsWith("Sunshine") == false)
                {
                    if (p.CanWrite && p.CanRead)
                    {
                        p.SetValue(result, p.GetValue(source, null));
                    }

                }
            }
            return result;
        }

        
        /// <summary>
        /// 设置可空类型的默认值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        public static void SetNullableDefaultValue<T>(this object obj) where T : struct
        {
            if (obj == null)
            {
                return;
            }

            foreach (var p in obj.GetType().GetProperties().Where(x => x.PropertyType == typeof(T?)))
            {
                if (p.GetValue(obj) == null)
                {
                    p.SetValue(obj, default(T));
                }
            }
        }

        /// <summary>
        /// 设置字符串类型的默认值
        /// </summary>
        /// <param name="obj"></param>
        public static void SetEmtpyValueForString(this object obj)
        {
            if (obj == null)
            {
                return;
            }

            foreach (var p in obj.GetType().GetProperties().Where(x => x.PropertyType == typeof(string)))
            {
                if (p.GetValue(obj) == null)
                {
                    p.SetValue(obj, "");
                }
            }
        }

        /// <summary>
        /// 更新DateTime类型的默认值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="traget"></param>
        /// <returns></returns>
        public static T UpdateDateTimeTo<T>(this T source, T traget)
        {
            foreach (var p in typeof(T).GetProperties().Where(x => x.PropertyType == typeof(DateTime)))
            {
                DateTime value = (DateTime)p.GetValue(source);
                if (value == default(DateTime))
                {
                    p.SetValue(traget, value);
                }
            }
            return traget;
        }

        /// <summary>
        /// 根据路径获取对象的值
        /// </summary>
        /// <param name="o"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static object GetValueByPath(this object o, string path)
        {
            if (o == null)
            {
                return null;
            }

            IResolver resolver = new Resolver();
            object result = resolver.Resolve(o, path);

            return result;
        }

        /// <summary>
        /// 根据路径获取对象的属性值
        /// </summary>
        /// <param name="o"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static object GetPropertyValue(this object o, string path)
        {
            if (o == null)
            {
                return null;
            }

            var propertyNames = path.Split('.');
            var value = o.GetType().GetProperty(propertyNames[0]).GetValue(o, null);

            if (propertyNames.Length == 1 || value == null)
            {
                return value;
            }
            else
            {
                return GetPropertyValue(value, path.Replace(propertyNames[0] + ".", ""));
            }
        }

        /// <summary>
        /// 根据路径获取对象的字段值
        /// </summary>
        /// <param name="o"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static object GetFieldValue(this object o, string path)
        {
            if (o == null)
            {
                return null;
            }

            var propertyNames = path.Split('.');
            var value = o.GetType().GetField(propertyNames[0]).GetValue(o);

            if (propertyNames.Length == 1 || value == null)
            {
                return value;
            }
            else
            {
                return GetFieldValue(value, path.Replace(propertyNames[0] + ".", ""));
            }
        }

        /// <summary>
        /// 获取对象的属性值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propName"></param>
        /// <returns></returns>
        public static object GetPropValue(this object obj, string propName)
        {
            object result = null;
            var p = obj.GetType().GetProperty(propName);
            if (p != null)
            {
                result = p.GetValue(obj, null);
            }
            return result;
        }

        /// <summary>
        /// 获取对象的属性值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="propName"></param>
        /// <returns></returns>
        public static T GetPropValue<T>(this object obj, string propName)
        {
            T result = default(T);
            var p = obj.GetType().GetProperty(propName);
            if (p != null)
            {
                result = (T)p.GetValue(obj, null);
            }
            return result;

        }

        /// <summary>
        /// 判断对象是否为泛型列表
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static bool IsGenericList(this object Value)
        {
            var t = Value.GetType();
            return t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(List<>) || t.GetGenericTypeDefinition() == typeof(IList<>));
        }

        public static Dictionary<string, object> ToDictionary(this Object obj)
        {
            if (obj == null) return new Dictionary<string, object>();
            var jObj = JObject.FromObject(obj);
            var newJObj = new Dictionary<string, object>();
            foreach (var property in jObj.Properties())
            {
                newJObj.Add(property.Name, property.Value);
            }
            return newJObj;



        }
    }

}


