using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TUI.Services.Extension
{
    public static class StringExtension
    {
        public static bool ContainsSurroundedWith(this string MainString, string value, string surroundWith)
        {
            return (surroundWith + MainString + surroundWith).Contains(surroundWith + value + surroundWith);
        }

        public static string SubstringIfExists(this string str, int startInd, int length)
        {
            if (str == null) return null;
            if (str.Length < startInd + length)
            {
                if (str.Length <= startInd)
                {
                    return "";
                }
                else
                {
                    return str.Substring(startInd); //up to the end
                }
            }
            return str.Substring(startInd, length);
        }
        public static string GetSafeSheetName(this string sheetName)
        {
            if (string.IsNullOrEmpty(sheetName))
            {
                return " ";
            }
            var sb = new StringBuilder();
            foreach (var c in sheetName)
            {
                switch (c)
                {
                    case '*':
                    case '/':
                    case ':':
                    case '?':
                    case '[':
                    case '\\':
                    case '"':
                    case ']':
                        sb.Append(' '); break;
                    default: sb.Append(c); break;
                }
                if (sb.Length >= 31) break;
            }
            if (sb[0] == '\'')
            {
                sb[0] = ' ';
            }
            if (sb[sb.Length - 1] == '\'')
            {
                sb[sb.Length - 1] = ' ';
            }
            return sb.ToString().Trim();
        }
        public static string GetNewSheetName(this string name, List<string> existSheetNames)
        {
            if (name.IsNullOrEmpty())
            {
                name = "Sheet 1";
            }
            else
            {
                name = name.Trim();
            }
            name = name.GetSafeSheetName();
            if (name.Length > 30)
            {
                name = name.Substring(0, 30);
            }
            var find = existSheetNames.Where(x => x == name).FirstOrDefault();
            if (find == null)
            {
                return name;
            }
            else
            {
                name = $"{name.Truncate(27)}{existSheetNames.Where(x => x.Truncate(27) == name.Truncate(27)).Count().ToString("000")}";

            }
            return "";
        }
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        public static bool IsUpper(this string value)
        {
            // Consider string to be uppercase if it has no lowercase letters.
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsLower(value[i]))
                {
                    return false;
                }
            }
            return true;
        }
        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        public static bool IsWhiteSpace(this string value)
        {
            // Consider string to be uppercase if it has no lowercase letters.
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsWhiteSpace(value[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public static DateTime ParseStringToDate(this string dt)
        {
            DateTime resultDt = DateTime.Today.Date;
            if (!string.IsNullOrEmpty(dt))
            {
                if (!DateTime.TryParse(dt, out resultDt))
                {
                    resultDt = DateTime.Today.Date;
                }
            }

            return resultDt;
        }

        public static string GetPropertyName<T>(this T obj, Expression<Func<T, object>> expression)
        {
            var result = string.Empty;
            var ModelName = typeof(T).FullName;
            var property = string.Empty;
            if (expression.Body is UnaryExpression)
            {
                property = ((MemberExpression)((UnaryExpression)expression.Body).Operand).Member.Name;
            }
            else if (expression.Body is MemberExpression)
            {
                property = ((MemberExpression)expression.Body).Member.Name;
            }
            else if (expression.Body is ParameterExpression)
            {
                property = ((ParameterExpression)expression.Body).Type.Name;
            }
            return property;
        }

        public static string GetValidFileName(this string value, string ReplacementChar = "")
        {
            return string.Join(ReplacementChar, value.Split(Path.GetInvalidFileNameChars()));
        }

        public static string DeleteLastCharacter(this string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value.Remove(value.Length - 1);
            }
            else
            {
                return value;
            }

        }
        public static string ConvertHtmlToText(this string value)
        {
            if (string.IsNullOrEmpty(value) == true)
            {
                return value;
            }
            else
            {
                var result = Html2Text(value);
                result = result.Replace("&amp;nbsp;", " ").Replace("&amp;amp;", "&");
                return result;
            }
        }

        public static string ReplaceMultipleSpacesWithOne(this string value)
        {
            RegexOptions options = RegexOptions.None;
            Regex regex = new Regex("[ ]{2,}", options);
            return regex.Replace(value, " ");
        }
        public static T ToEnum<T>(this string value, T defaultValue) where T : struct
        {
            try
            {
                T enumValue;
                if (value.IsNullOrEmpty())
                {
                    return defaultValue;
                }
                if (!Enum.TryParse(value, true, out enumValue))
                {
                    return defaultValue;
                }
                return enumValue;
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
        public static T _ToEnum<T>(this string value, T defaultValue) where T : struct
        {
            return ToEnum(value, defaultValue);
        }
        public static string ToString2(this object obj, string format)
        {
            Type type = obj.GetType();
            PropertyInfo[] properties = type.GetProperties(
                BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance);

            MatchEvaluator evaluator = match =>
            {
                string propertyName = match.Groups["Name"].Value;
                PropertyInfo property = properties.FirstOrDefault(p => p.Name == propertyName);
                if (property != null)
                {
                    object propertyValue = property.GetValue(obj, null);
                    if (propertyValue != null) return propertyValue.ToString();
                    else return "";
                }
                else return match.Value;
            };
            return Regex.Replace(format, @"{(?<Name>[^}]+)}", evaluator, RegexOptions.Compiled);
        }

        public static string RegExpFy(this string value)
        {
            return @"\b" + value.Replace("|", @"\b|\b") + @"\b";
        }

        public static string RegExpFyV2(this string value)
        {
            if (!value.IsNullOrWhiteSpace())
            {
                return value.RegExpFy();
            }
            else
            {
                return value;
            }
        }

        #region 
        private static string getNodeText(XText textNode)
        {
            var xxx = textNode.Ancestors();
            var pre = "";
            var next = "";
            for (int i = 0; i < xxx.Count(x => x.Name == "ul" || x.Name == "ol"); i++)
            {
                pre = $"    {pre}";
            }
            var firstUL = xxx.Where(x => x.Name == "ul" || x.Name == "ol").FirstOrDefault();
            if (firstUL != null)
            {
                if (firstUL.Name == "ol")
                {
                    var li = xxx.Where(x => x.Name == "li").FirstOrDefault();
                    if (li != null)
                    {
                        var index = li.Parent.Nodes().ToList().IndexOf(li) + 1;
                        pre = $"{pre}{index}. ";
                    }


                }
                else
                {
                    pre = $"{pre}● ";
                }
            }
            foreach (var parent in xxx)
            {
                if (parent.Name == "p" || parent.Name == "br")
                {
                    next = $"{next}\r";
                }
                else if (parent.Name == "li")
                {

                    if (parent.Parent.Name == "ol")
                    {

                        next = $"{next}\r";
                    }
                    else
                    {
                        next = $"{next}\r";
                    }
                    if (parent.Parent.Nodes().First() == parent)
                    {
                        if (pre.IndexOf('\r') < 0)
                        {
                            pre = $"\r{pre}";
                        }

                    }
                }


            }

            return $"{pre}{textNode.Value}{next}";
        }

        private static string Html2Text(string source)
        {
            if (source == null)
            {
                return source;
            }
            if (source.IndexOf(System.Web.HttpUtility.HtmlEncode("<")) >= 0 && source.IndexOf(System.Web.HttpUtility.HtmlEncode(">")) >= 0)
            {
                try
                {
                    var replace = System.Web.HttpUtility.HtmlDecode(source).Replace("&nbsp;", " ").Replace("&", ".&amp;");
                    var xdoc = XDocument.Parse($"<div>{replace}</div>");
                    var result = Html2Text(xdoc);
                    return result.TrimEnd();//.Replace("\r\n", "&#10;");
                }
                catch
                {
                    return source.TrimEnd();
                }

            }
            else
            {
                return source.TrimEnd();
            }

        }
        private static string Html2Text(XDocument source, bool withHtmlEncode = false)
        {
            string result = "";
            var nodes = source.DescendantNodes();
            foreach (var item in nodes.Where(x => x.NodeType == System.Xml.XmlNodeType.Text))
            {

                result += getNodeText((XText)item);
            }
            if (withHtmlEncode)
            {
                return System.Web.HttpUtility.HtmlEncode(result);
            }
            else
            {
                return result;
            }
        }
        public static string ToString4(this object obj, string format)
        {
            MatchEvaluator evaluator = match =>
            {
                string[] propertyNames = match.Groups["Name"].Value.Split('.');
                string propertyFormat = match.Groups["Format"].Value;

                object propertyValue = obj;
                try
                {
                    foreach (string propertyName in propertyNames)
                        propertyValue = propertyValue.GetPropertyValue(propertyName);
                }
                catch
                {
                    return match.Value;
                }

                if (string.IsNullOrEmpty(format) == false)
                    return string.Format("{0:" + propertyFormat + "}", propertyValue);
                else
                    return propertyValue.ToString();
            };
            string pattern = @"\[(?<Name>[^\[\]:]+)(\s*[:]\s*(?<Format>[^\[\]:]+))?\]";
            return Regex.Replace(format, pattern, evaluator, RegexOptions.Compiled);
        }

        #endregion
    }
}

