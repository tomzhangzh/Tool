using DynamicExpresso;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using TUI.Services.Extension;
using System.Text.RegularExpressions;

namespace TUI.Services.Utility
{
	public static class DynamicExpressoStringExtension
    {
		/// <summary>
		/// 解析表达式
		/// </summary>
		/// <param name="str">字符串</param>
		/// <param name="obj">参数</param>
		/// <param name="paramName">参数名</param>
		/// <returns></returns>
		public static T Eval<T>(this string str, object obj = null, string paramName = "obj")
		{
			if (str.IsNullOrEmpty())
            {
				return default(T);
            }
			Interpreter expr = new Interpreter(InterpreterOptions.Default)
				.EnableReflection()
				.Reference(typeof(ExpandoObject))
				.Reference(typeof(DynamicExpressoHelper))
				//.SetFunction("equalsIgnore", (Func<object, object, bool>)EqualsIgnorecase)
				//.SetFunction("startsIgnore", (Func<object, object, bool>)StartsWithIgnorecase)
				//.SetFunction("endsIgnore", (Func<object, object, bool>)EndsWithIgnorecase)
				//.SetFunction("compareIgnore", (Func<object, object, int>)CompareIgnorecase)
				//.SetFunction("indexOfIgnore", (Func<object, object, int>)IndexOfIgnorecase)
				//.SetFunction("lastIndexOfIgnore", (Func<object, object, int>)LastIndexOfIgnorecase);
				;
			if (obj != null)
			{
				paramName = paramName ?? "obj";
				expr.SetVariable(paramName, obj);
			}
			return expr.Eval<T>(str);
		}
		public static string DynamicString(this string dynamicString, object obj = null, string paramName = "obj")
		{
			Interpreter interpreter = new Interpreter(InterpreterOptions.Default)
				.EnableReflection()
				.Reference(typeof(ExpandoObject))
				.Reference(typeof(DynamicExpressoHelper));
			MatchEvaluator evaluator = match =>
			{
				string CodeName = match.Groups["Name"].Value;
				var formated = interpreter.Eval<object>(CodeName,
				new DynamicExpresso.Parameter("obj", obj));
				return $"{formated}";
			};
			return Regex.Replace(dynamicString, @"{{(?<Name>[^}}]+)}}", evaluator, RegexOptions.Compiled);
		}
	}


	public static class DynamicExpressoHelper
	{
		static readonly Interpreter _interpreter = new Interpreter();

		static readonly Type eType = typeof(Enumerable);

		#region 列表方法
		static readonly MethodInfo anyMethod = null;
		static readonly MethodInfo whereAnyMethod = null;
		static readonly MethodInfo whereMethod = null;
		static readonly MethodInfo countMethod = null;
		static readonly MethodInfo whereCountMethod = null;
		static readonly MethodInfo firstMethod = null;
		static readonly MethodInfo whereFirstMethod = null;
		static readonly MethodInfo firstOrDefaultMethod = null;
		static readonly MethodInfo whereFirstOrDefaultMethod = null;
		static readonly MethodInfo lastMethod = null;
		static readonly MethodInfo whereLastMethod = null;
		static readonly MethodInfo lastOrDefaultMethod = null;
		static readonly MethodInfo whereLastOrDefaultMethod = null;
		#endregion

		static readonly Type whereType = typeof(Func<,>);

		static readonly MethodInfo parseDelegateMethod = null;

		static readonly Type enumType = typeof(IEnumerable<>);

		static readonly Type nullableType = typeof(Nullable<>);

		static DynamicExpressoHelper()
		{
			#region 支持的方法
			MethodInfo[] methods = eType.GetMethods();
			foreach (MethodInfo method in methods)
			{
				if (method.Name == "Any")
				{
					if (Enumerable.Count(method.GetParameters()) == 1)
					{
						anyMethod = method;
					}
					else
					{
						whereAnyMethod = method;
					}
				}
				if (method.Name == "Count")
				{
					if (Enumerable.Count(method.GetParameters()) == 1)
					{
						countMethod = method;
					}
					else
					{
						whereCountMethod = method;
					}
				}
				if (method.Name == "Where")
				{
					if (method.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2)
					{
						whereMethod = method;
					}
				}
			}
			#endregion
			parseDelegateMethod = _interpreter.GetType().GetMethod("ParseAsDelegate", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod); ;
		}

		#region 字符串操作
		/// <summary>
		/// 忽略大小写判断相等
		/// </summary>
		/// <param name="str1"></param>
		/// <param name="str2"></param>
		/// <returns></returns>
		public static bool EqualsIgnorecase(this ExpandoObject obj, object str1, object str2)
		{
			if (str1 == null && str2 == null)
				return true;

			if (str1 == null || str2 == null)
			{
				return false;
			}

			return string.Equals(str1.ToString(), str2.ToString(), StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// str1 以 str2开头
		/// </summary>
		/// <param name="str1"></param>
		/// <param name="str2"></param>
		/// <returns></returns>
		public static bool StartsWithIgnorecase(this ExpandoObject obj, object str1, object str2)
		{
			if (str1 == null && str2 == null)
				return true;

			if (str1 == null || str2 == null)
			{
				return false;
			}

			return str1.ToString().StartsWith(str2.ToString(), StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// str1 以 str2 结尾
		/// </summary>
		/// <param name="str1"></param>
		/// <param name="str2"></param>
		/// <returns></returns>
		public static bool EndsWithIgnorecase(this ExpandoObject obj, object str1, object str2)
		{
			if (str1 == null && str2 == null)
				return true;

			if (str1 == null || str2 == null)
			{
				return false;
			}

			return str1.ToString().EndsWith(str2.ToString(), StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// 忽略大小写 比较
		/// </summary>
		/// <param name="str1"></param>
		/// <param name="str2"></param>
		/// <returns></returns>
		public static int CompareIgnorecase(this ExpandoObject obj, object str1, object str2)
		{
			if (str1 == null && str2 == null)
				return 0;

			if (str1 == null || str2 == null)
			{
				return 0;
			}

			return string.Compare(str1.ToString(), str2.ToString(), StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// 索引位置
		/// </summary>
		/// <param name="str1"></param>
		/// <param name="str2"></param>
		/// <returns></returns>
		public static int IndexOfIgnorecase(this ExpandoObject obj, object str1, object str2)
		{
			if (str1 == null && str2 == null)
				return -1;

			if (str1 == null || str2 == null)
			{
				return -1;
			}

			return str1.ToString().IndexOf(str2.ToString(), StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// 匹配最后一个索引位置
		/// </summary>
		/// <param name="str1"></param>
		/// <param name="str2"></param>
		/// <returns></returns>
		public static int LastIndexOfIgnorecase(this ExpandoObject obj, object str1, object str2)
		{
			if (str1 == null && str2 == null)
				return -1;

			if (str1 == null || str2 == null)
			{
				return -1;
			}

			return str1.ToString().LastIndexOf(str2.ToString(), StringComparison.OrdinalIgnoreCase);
		}
		#endregion

		#region 列表

		/// <summary>
		/// 统计
		/// </summary>
		/// <param name="list">列表</param>
		/// <param name="expression">表达式</param>
		/// <returns></returns>
		public static object Any(this object list, string expression = null)
		{
			Type type = list.GetType();
			Tuple<Type, Type> genericType = GetEnumerableGenericType(type);
			if (genericType == null)
			{
				return list;
			}

			if (string.IsNullOrEmpty(expression))
			{
				return anyMethod.MakeGenericMethod(genericType.Item1).Invoke(null, new object[] { list });
			}
			else
			{
				Type funcType = whereType.MakeGenericType(genericType.Item1, typeof(bool));
				MethodInfo parseMethod = parseDelegateMethod.MakeGenericMethod(funcType);
				object predicate = parseMethod.Invoke(_interpreter, new object[] { expression, new string[] { "x" } });
				return whereAnyMethod.MakeGenericMethod(genericType.Item1).Invoke(null, new object[] { list, predicate });
			}
		}

		/// <summary>
		/// 过滤
		/// </summary>
		/// <param name="list">列表</param>
		/// <param name="expression">表达式</param>
		/// <returns></returns>
		public static object Where(this object list, string expression)
		{
			Type type = list.GetType();
			Tuple<Type, Type> genericType = GetEnumerableGenericType(type);
			if (genericType == null || string.IsNullOrEmpty(expression))
			{
				return list;
			}

			Type funcType = whereType.MakeGenericType(genericType.Item1, typeof(bool));
			MethodInfo parseMethod = parseDelegateMethod.MakeGenericMethod(funcType);
			object predicate = parseMethod.Invoke(_interpreter, new object[] { expression, new string[] { "x" } });
			return whereMethod.MakeGenericMethod(genericType.Item1).Invoke(null, new object[] { list, predicate });
		}

		/// <summary>
		/// 统计
		/// </summary>
		/// <param name="list">列表</param>
		/// <param name="expression">表达式</param>
		/// <returns></returns>
		public static object Count(this object list, string expression = null)
		{
			Type type = list.GetType();
			Tuple<Type, Type> genericType = GetEnumerableGenericType(type);
			if (genericType == null)
			{
				return list;
			}

			if (string.IsNullOrEmpty(expression))
			{
				return countMethod.MakeGenericMethod(genericType.Item1).Invoke(null, new object[] { list });
			}
			else
			{
				Type funcType = whereType.MakeGenericType(genericType.Item1, typeof(bool));
				MethodInfo parseMethod = parseDelegateMethod.MakeGenericMethod(funcType);
				object predicate = parseMethod.Invoke(_interpreter, new object[] { expression, new string[] { "x" } });
				return whereCountMethod.MakeGenericMethod(genericType.Item1).Invoke(null, new object[] { list, predicate });
			}
		}

		/// <summary>
		/// 统计
		/// </summary>
		/// <param name="list">列表</param>
		/// <param name="expression">表达式</param>
		/// <returns></returns>
		public static object First(this object list, string expression = null)
		{
			Type type = list.GetType();
			Tuple<Type, Type> genericType = GetEnumerableGenericType(type);
			if (genericType == null)
			{
				return list;
			}

			if (string.IsNullOrEmpty(expression))
			{
				return firstMethod.MakeGenericMethod(genericType.Item1).Invoke(null, new object[] { list });
			}
			else
			{
				Type funcType = whereType.MakeGenericType(genericType.Item1, typeof(bool));
				MethodInfo parseMethod = parseDelegateMethod.MakeGenericMethod(funcType);
				object predicate = parseMethod.Invoke(_interpreter, new object[] { expression, new string[] { "x" } });
				return whereFirstMethod.MakeGenericMethod(genericType.Item1).Invoke(null, new object[] { list, predicate });
			}
		}

		/// <summary>
		/// 统计
		/// </summary>
		/// <param name="list">列表</param>
		/// <param name="expression">表达式</param>
		/// <returns></returns>
		public static object FirstOrDefault(this object list, string expression = null)
		{
			Type type = list.GetType();
			Tuple<Type, Type> genericType = GetEnumerableGenericType(type);
			if (genericType == null)
			{
				return list;
			}

			if (string.IsNullOrEmpty(expression))
			{
				return firstOrDefaultMethod.MakeGenericMethod(genericType.Item1).Invoke(null, new object[] { list });
			}
			else
			{
				Type funcType = whereType.MakeGenericType(genericType.Item1, typeof(bool));
				MethodInfo parseMethod = parseDelegateMethod.MakeGenericMethod(funcType);
				object predicate = parseMethod.Invoke(_interpreter, new object[] { expression, new string[] { "x" } });
				return whereFirstOrDefaultMethod.MakeGenericMethod(genericType.Item1).Invoke(null, new object[] { list, predicate });
			}
		}

		/// <summary>
		/// 最后一个
		/// </summary>
		/// <param name="list">列表</param>
		/// <param name="expression">表达式</param>
		/// <returns></returns>
		public static object Last(this object list, string expression = null)
		{
			Type type = list.GetType();
			Tuple<Type, Type> genericType = GetEnumerableGenericType(type);
			if (genericType == null)
			{
				return list;
			}

			if (string.IsNullOrEmpty(expression))
			{
				return lastMethod.MakeGenericMethod(genericType.Item1).Invoke(null, new object[] { list });
			}
			else
			{
				Type funcType = whereType.MakeGenericType(genericType.Item1, typeof(bool));
				MethodInfo parseMethod = parseDelegateMethod.MakeGenericMethod(funcType);
				object predicate = parseMethod.Invoke(_interpreter, new object[] { expression, new string[] { "x" } });
				return whereLastMethod.MakeGenericMethod(genericType.Item1).Invoke(null, new object[] { list, predicate });
			}
		}

		/// <summary>
		/// 最后一个
		/// </summary>
		/// <param name="list">列表</param>
		/// <param name="expression">表达式</param>
		/// <returns></returns>
		public static object LastOrDefault(this object list, string expression = null)
		{
			Type type = list.GetType();
			Tuple<Type, Type> genericType = GetEnumerableGenericType(type);
			if (genericType == null)
			{
				return list;
			}

			if (string.IsNullOrEmpty(expression))
			{
				return lastOrDefaultMethod.MakeGenericMethod(genericType.Item1).Invoke(null, new object[] { list });
			}
			else
			{
				Type funcType = whereType.MakeGenericType(genericType.Item1, typeof(bool));
				MethodInfo parseMethod = parseDelegateMethod.MakeGenericMethod(funcType);
				object predicate = parseMethod.Invoke(_interpreter, new object[] { expression, new string[] { "x" } });
				return whereLastOrDefaultMethod.MakeGenericMethod(genericType.Item1).Invoke(null, new object[] { list, predicate });
			}
		}

		/// <summary>
		/// 获取列表泛型类型
		/// </summary>
		/// <param name="type">列表类型</param>
		/// <returns></returns>
		private static Tuple<Type, Type> GetEnumerableGenericType(Type type)
		{
			if (type.IsGenericType)
			{
				if (type.GetGenericTypeDefinition() == nullableType)
				{
					return null;
				}

				Type genericType = type.GetGenericArguments()[0];
				Type listType = enumType.MakeGenericType(genericType);
				if (listType.IsAssignableFrom(type))
				{
					return Tuple.Create(genericType, listType);
				}
			}
			return null;
		}
		#endregion

		#region 字典
		/// <summary>
		/// 求取字典值
		/// </summary>
		/// <param name="key">键</param>
		/// <returns></returns>
		public static object Value(this ExpandoObject obj, string key)
		{
			IDictionary<string, object> dic = obj as IDictionary<string, object>;
			return dic.ContainsKey(key) ? dic[key] : null;
		}

		/// <summary>
		/// 是否包含键
		/// </summary>
		/// <param name="key">键</param>
		/// <returns></returns>
		public static bool Exists(this ExpandoObject obj, string key)
		{
			IDictionary<string, object> dic = obj as IDictionary<string, object>;
			return dic.ContainsKey(key);
		}
		#endregion
	}
//	int result = "3+2".Eval<int>();
//	Console.WriteLine(result);
 
//            dynamic obj = new ExpandoObject();
//	obj.Name = ".net";
//            obj.Id = 10;
 
//            IDictionary<string, object> dic = obj as IDictionary<string, object>;
//	dic.Add("Address", "北京市东城区");
//            /*
//            var interpreter = new Interpreter()
//                .SetVariable("obj", (object)obj);
//            Console.WriteLine(obj.Id == interpreter.Eval<int>("obj.Id"));
//            */
 
//            //True
//            bool flag = "((int)obj.Id) > 2 && equalsIgnore(obj.Name,\".Net\") && startsIgnore(obj.Name,\".\")"
//				.Eval<bool>("obj", (object)obj);
//	Console.WriteLine(flag);
 
//            //True
//            flag = "obj.Address.Contains(\"北京\")".Eval<bool>("obj", (object) obj);
//            Console.WriteLine(flag);
 
//            List<Detail> details = new List<Detail>();
//	Detail detail = new Detail();
//	detail.id = 12;
//            detail.name = "您好";
//            details.Add(detail);
//            dynamic obj = new ExpandoObject();
//	obj.details = details;
//            int count = "obj.details.Where(\"o.id>10\").Count()".Eval<int>((object)obj);
//	Console.WriteLine(count);
//            Console.ReadLine();



}
