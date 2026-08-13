
using System;

namespace TUI.Utils.Extensions
{
    /// <summary>
    /// 异常扩展类
    /// </summary>
    public static class ExceptionExtension
    {
        /// <summary>
        /// 将异常转换为字符串
        /// </summary>
        /// <param name="exception">异常</param>
        /// <returns>异常字符串</returns>
        public static string ToStringEx(this Exception exception)
        {
            if (exception == null) return "";
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("====================EXCEPTION====================");
            stringBuilder.AppendLine("【Message】:" + exception.Message);
            stringBuilder.AppendLine("【Source】:" + exception.Source);
            stringBuilder.AppendLine("【TargetSite】:" + ((exception.TargetSite != null) ? exception.TargetSite.Name : "None"));
            stringBuilder.AppendLine("【StackTrace】:" + exception.StackTrace);
            //stringBuilder.AppendLine("【exception】:" + exception);
            stringBuilder.AppendLine("=================================================");
            if (exception.InnerException != null)
            {
                stringBuilder.AppendLine("====================INNER EXCEPTION====================");
                stringBuilder.AppendLine("【Message】:" + exception.InnerException.Message);
                stringBuilder.AppendLine("【Source】:" + exception.InnerException.Source);
                stringBuilder.AppendLine("【TargetSite】:" + ((exception.InnerException.TargetSite != null) ? exception.InnerException.TargetSite.Name : "None"));
                stringBuilder.AppendLine("【StackTrace】:" + exception.InnerException.StackTrace);
    
                stringBuilder.AppendLine("=================================================");
            }
            return stringBuilder.ToString();
        }
    }
}


