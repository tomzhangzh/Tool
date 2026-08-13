

using Pather.CSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace TUI.Services
{

    public static class ObjectExtensions
    {
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
        public static IEnumerable<T> Flatten<T, R>(this T source, Func<T, R> recursion) where R : IEnumerable<T>
        {
            var children = recursion(source);
            foreach (var item in children)
            {
                foreach (var i in Flatten(item, recursion))
                {
                    yield return i;
                }
                yield return item;
            }
        }
        public static Type GetEntityType(this object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            Type entityType = null;
            if (obj is Type)
            {
                entityType = obj as Type;
            }
            else
            {
                entityType = obj.GetType();
            }

            if (entityType.BaseType != null && (entityType.Namespace == "System.Data.Entity.DynamicProxies" || entityType.Namespace == "Castle.Proxies"))
            {
                entityType = entityType.BaseType;
            }
            return entityType;
        }
        public static object ChangeType(this object value, Type conversionType)
        {
            // Note: This if block was taken from Convert.ChangeType as is, and is needed here since we're
            // checking properties on conversionType below.
            if (conversionType == null)
            {
                throw new ArgumentNullException("conversionType");
            } // end if

            // If it's not a nullable type, just pass through the parameters to Convert.ChangeType

            if (conversionType.IsGenericType &&
              conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                // It's a nullable type, so instead of calling Convert.ChangeType directly which would throw a
                // InvalidCastException (per http://weblogs.asp.net/pjohnson/archive/2006/02/07/437631.aspx),
                // determine what the underlying type is
                // If it's null, it won't convert to the underlying type, but that's fine since nulls don't really
                // have a type--so just return null
                // Note: We only do this check if we're converting to a nullable type, since doing it outside
                // would diverge from Convert.ChangeType's behavior, which throws an InvalidCastException if
                // value is null and conversionType is a value type.
                if (value == null)
                {
                    return null;
                } // end if

                // It's a nullable type, and not null, so that means it can be converted to its underlying type,
                // so overwrite the passed-in conversion type with this underlying type
                NullableConverter nullableConverter = new NullableConverter(conversionType);

                conversionType = nullableConverter.UnderlyingType;
            } // end if

            // Now that we've guaranteed conversionType is something Convert.ChangeType can handle (i.e. not a
            // nullable type), pass the call on to Convert.ChangeType

            return Convert.ChangeType(value, conversionType);

        }
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
        public static T Clone<T>(this T source) where T : class, new()
        {
            var result = new T();
            foreach (var p in typeof(T).GetProperties())
            {
                if (p.PropertyType.IsEnum || p.PropertyType.BaseType != null && p.PropertyType.Namespace.StartsWith("TUI") == false && p.PropertyType.Namespace.StartsWith("TUI") == false)
                {
                    if (p.CanWrite && p.CanRead)
                    {
                        p.SetValue(result, p.GetValue(source, null));
                    }

                }
            }
            return result;
        }
       

        public static void SetNullableDefaultValue<T>(this object obj) where T : struct
        {
            if (obj == null)
            {
                return;
            }
            foreach (var p in obj.GetType().GetProperties().Where(x => x.PropertyType == typeof(Nullable<T>)))
            {
                var value = p.GetValue(obj);
                if (value == null)
                {
                    p.SetValue(obj, default(T));
                }
            }
        }
        public static void SetStringDefaultValue<T>(this object obj) where T : struct
        {
            if (obj == null)
            {
                return;
            }
            foreach (var p in obj.GetType().GetProperties().Where(x => x.PropertyType == typeof(Nullable<T>)))
            {
                var value = p.GetValue(obj);
                if (value == null)
                {
                    p.SetValue(obj, default(T));
                }
            }
        }
        public static void SetNullableDefaultValue(this object obj, bool includeString = true)
        {
            SetNullableDefaultValue<int>(obj);
            SetNullableDefaultValue<decimal>(obj);
            SetNullableDefaultValue<double>(obj);
            if (includeString) { SetEmtpyValueForString(obj); }
        }
        public static void SetEmtpyValueForString(this object obj)
        {
            if (obj == null) return;
            foreach (var p in obj.GetType().GetProperties().Where(x => x.PropertyType == typeof(string)))
            {
                var value = p.GetValue(obj);
                if (value == null)
                {
                    p.SetValue(obj, "");
                }
            }
        }
        public static void SetSqlMinDate(this object obj, DateTime? defaultDate = null)
        {
            if (defaultDate == null)
            {
                defaultDate = new DateTime(1900, 1, 1);
            }
            if (obj == null)
            {
                return;
            }
            foreach (var p in obj.GetType().GetProperties().Where(x => x.PropertyType == typeof(DateTime)))
            {
                DateTime value = (DateTime)p.GetValue(obj);
                if (value == default(DateTime))
                {
                    p.SetValue(obj, defaultDate.Value);
                }
            }
        }
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
        public static object GetValueByPath(this object o, string path)
        {
            if (o == null)
            {
                return null;
            }
            IResolver resolver = new Resolver();
         

            object result = resolver.Resolve(o, path); //the result is "value"
            return result;
        }
        public static object GetPropertyValue(this object o, string path)
        {
            if (o == null)
            {
                return null;
            }
            var propertyNames = path.Split('.');
            var value = o.GetType().GetProperty(propertyNames[0]).GetValue(o, null);

            if (propertyNames.Length == 1 || value == null)
                return value;
            else
            {
                return GetPropertyValue(value, path.Replace(propertyNames[0] + ".", ""));
            }
        }
        public static object GetFieldValue(this object o, string path)
        {
            if (o == null)
            {
                return null;
            }
            var propertyNames = path.Split('.');
            var value = o.GetType().GetField(propertyNames[0]).GetValue(o);

            if (propertyNames.Length == 1 || value == null)
                return value;
            else
            {
                return GetFieldValue(value, path.Replace(propertyNames[0] + ".", ""));
            }
        }
        public static object GetPropValue(this object obj, string propName)
        {
            return obj.GetType().GetProperty(propName).GetValue(obj, null);
        }
        //public static string ExecuteCode(this object obj,string code)
        //{
        //    if (obj==null)
        //    {
        //        return null;
        //    }
        //    var interpreter = new DynamicExpresso.Interpreter();
        //    MatchEvaluator evaluator = match =>
        //    {
        //        string CodeName = match.Groups["Name"].Value;
        //        var formated = interpreter.Eval<object>(CodeName,
        //             new DynamicExpresso.Parameter("Model", obj));
        //        return $"{formated}";
        //    };
        //    return Regex.Replace(code, @"{(?<Name>[^}]+)}", evaluator, RegexOptions.Compiled);
        //}
        public static T GetPropValue<T>(this object obj, string propName)
        {
            return (T)obj.GetType().GetProperty(propName).GetValue(obj, null);
        }
        public static bool IsGenericList(this object Value)
        {
            var t = Value.GetType();
            return t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(List<>) || t.GetGenericTypeDefinition() == typeof(IList<>));
        }

    }
  
    public static class TypedDefaultExtensions
    {

        public static object ToDefault(this Type targetType)
        {

            if (targetType == null)
                throw new NullReferenceException();

            var mi = typeof(TypedDefaultExtensions)
                .GetMethod("_ToDefaultHelper", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            var generic = mi.MakeGenericMethod(targetType);

            var returnValue = generic.Invoke(null, new object[0]);
            return returnValue;
        }

        static T _ToDefaultHelper<T>()
        {
            return default(T);
        }

    }
    public static class IEnumerableExtensions
    {
        public static IEnumerable<List<T>> GroupByMaximumNumber<T>(this IEnumerable<T> items, int MaximumNumber)
        {
            if (MaximumNumber <= 0)
            {
                throw new ArgumentException("Chunk size must be positive.", "chunkSize");
            }

            return
                items.Select((item, index) => new { item, index })
                     .GroupBy(pair => pair.index / MaximumNumber, pair => pair.item)
                     .Select(grp => grp.ToList());
        }
        public static IEnumerable<IEnumerable<T>> Chunks<T>(this IEnumerable<T> enumerable,
                                                    int chunkSize)
        {
            if (chunkSize < 1) throw new ArgumentException("chunkSize must be positive");

            using (var e = enumerable.GetEnumerator())
                while (e.MoveNext())
                {
                    var remaining = chunkSize;    // elements remaining in the current chunk
                    var innerMoveNext = new Func<bool>(() => --remaining > 0 && e.MoveNext());

                    yield return e.GetChunk(innerMoveNext);
                    while (innerMoveNext()) {/* discard elements skipped by inner iterator */}
                }
        }

        private static IEnumerable<T> GetChunk<T>(this IEnumerator<T> e,
                                                  Func<bool> innerMoveNext)
        {
            do yield return e.Current;
            while (innerMoveNext());
        }
    }
}
