using TUI.Services.DBModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace TUI.Services.Model
{
    public class ValidateInfo
    {
        public bool? required { get; set; }
        public bool? email { get; set; }
        public bool? url { get; set; }//                       必须输入正确格式的网址
        public bool? date { get; set; }//                       必须输入正确格式的日期 日期校验ie6出错，慎用
        public bool? dateISO { get; set; }//                必须输入正确格式的日期(ISO)，例如：2009-06-23，1998/01/22 只验证格式，不验证有效性
        public bool? number { get; set; }//                 必须输入合法的数字(负数，小数)
        public bool? digits { get; set; }//                    必须输入整数
        //public bool creditcard:                   必须输入合法的信用卡号
        //public bool  equalTo:"#field"          输入值必须和#field相同
        //public bool accept:                       输入拥有合法后缀名的字符串（上传文件的后缀）
        public int? maxlength { get; set; }//              输入长度最多是5的字符串(汉字算一个字符)
        public int? minlength { get; set; }//           输入长度最小是10的字符串(汉字算一个字符)
        //public bool rangelength:[5,10]      输入长度必须介于 5 和 10 之间的字符串")(汉字算一个字符)
        //public bool range:[5,10]               输入值必须介于 5 和 10 之间
        public int? max { get; set; }//                         输入值不能大于5
        public int? min { get; set; }//                       输入值不能小于10
    }
    public static class DetailitemExtension
    {
        //public static ItemExtendedBase GetExtendPropery( this DETAILITEM obj )
        //{
        //    return ItemExtendedBase.GetExtendProperty( obj );
        //}
        //public static void SetExtendProperty<T>

        public static string GetValidateClass(this DETAILITEM obj)
        {
            string result = string.Empty;
            var validateInfo = new ValidateInfo();
            if (obj.ISREQUIRED)
            {
                result += string.Format(" {0} ", "required");

            }
            if (string.IsNullOrEmpty(obj.VALIDATETYPE) == false)
            {
                result += string.Format(" {0} ", obj.VALIDATETYPE);
                //var p = validateInfo.GetType().GetProperty( obj.ValidateType );
                //if( p != null )
                //{
                //    p.SetValue( validateInfo, true, null );
                //}
            }
            return result;
        }

        public static object GetExtendProperty(this DETAILITEM obj)
        {
            return ItemExtendedBase.GetExtendProperty(obj);
        }

        public static IDictionary<string, object> GetHTMLATTRIBUTES(this DETAILITEM obj)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (string.IsNullOrEmpty(obj.HTMLATTRIBUTES) == false)
            {
                var input = obj.HTMLATTRIBUTES;
                string[] items = input.TrimEnd(';').Split(';');
                foreach (string item in items)
                {
                    string[] keyValue = item.Split('=');
                    if (keyValue.Length > 1)
                    {
                        if (string.IsNullOrEmpty(keyValue[0]) == false)
                        {
                            result.Add(keyValue[0], keyValue[1]);
                        }

                    }
                    else
                    {
                        if (string.IsNullOrEmpty(keyValue[0]) == false)
                        {
                            result.Add(keyValue[0], null);
                        }
                    }
                }
            }
            if (obj.Disabled)
            {
                result.Add("disabled", "disabled");
            }
            return result;
        }
    }
}
