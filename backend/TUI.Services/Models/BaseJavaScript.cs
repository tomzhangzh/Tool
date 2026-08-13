using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TUI.Services.Models
{

    public class BaseJavaScript
    {
        public virtual string Script
        {
            get
            {
                return "";
            }
        }
    }
    public enum FlashMessageTypeEnum
    {
        info,
        success,
        error,
        warning,
    }
    public class CloseDialogJavaScript : BaseJavaScript
    {
        public CloseDialogJavaScript()
        {

        }
        public override string Script
        {
            get
            {
                return "var win=$this.closest('.modal').data('modal');\r\n if (win){win.hide();}";
            }
        }
    }
    public class FlashMessageJavaScript : FireEventJavaScript
    {
        public FlashMessageJavaScript()
        {
            this.SaveSuccessFireEvent = true;
            this.Message = "Save success.";
            this.Title = "Message";
            this.MessageType = FlashMessageTypeEnum.success;
        }
        public bool SaveSuccessFireEvent
        {
            get; set;
        }
        public string Title { get; set; }
        public string Message { get; set; }
        public FlashMessageTypeEnum MessageType { get; set; }
        public override string Script
        {
            get
            {
                var result = String.Format("var obj={0};\r\n;utility.notification(obj.MessageType,obj.Message,obj.Title);\r\n", JsonConvert.SerializeObject(new { Title, Message, MessageType = MessageType.ToString() }));
                if (this.SaveSuccessFireEvent)
                {
                    var ssf = new FlashSaveSuccessJavaScript();
                    if (string.IsNullOrEmpty(this.EventName)) ssf.EventName = "SaveSuccess";
                    else ssf.EventName = this.EventName;
                    ssf.Data = this.Data;
                    result += ssf.Script;
                }
                return result;
            }
        }

    }
    public class FireEventJavaScript : BaseJavaScript
    {
        public string EventName { get; set; }
        public object Data { get; set; }
        public override string Script
        {
            get
            {
                return String.Format("var objFire={0};\r\n;$('body').myFire(objFire.EventName,objFire.Data);\r\n", JsonConvert.SerializeObject(new { EventName = EventName, Data = Data }));
            }
        }
    }
    public class RedirectLocal : BaseJavaScript
    {
        public string url { get; set; }
        public override string Script
        {
            get
            {
                return String.Format("var obj={0};\r\n;window.location=obj.url;\r\n", JsonConvert.SerializeObject(new { url = url }));
            }
        }
    }
    public class FlashSaveSuccessJavaScript : FireEventJavaScript
    {

        public FlashSaveSuccessJavaScript()
        {
            this.EventName = "SaveSuccess";
        }
    }
    public class OpenWindowJavaScript : BaseJavaScript
    {
        public string url { get; set; }
        public override string Script
        {
            get
            {
                return $"utility.dialog.myLoad({{}},'${url}');";
            }
        }
    }
    public class DownloadFileJavaScript : BaseJavaScript
    {
        public string url { get; set; } = "/Home/DownLoadFile";
        public string fileName { get; set; }
        public string downloadName { get; set; }
        public override string Script
        {
            get
            {
                return $"var url='{this.url}?fileName={ System.Text.Encodings.Web.UrlEncoder.Default.Encode( this.fileName)}&downloadName={System.Text.Encodings.Web.UrlEncoder.Default.Encode(downloadName??"")}';\r\n;window.open(url);\r\n";
            }
        }
    }
    public class AlertMessageJavaScript : FireEventJavaScript
    {
        public string Message { get; set; }
        public string RedirectUrl { get; set; }
        public bool IsFireEvent { get; set; }
        public override string Script
        {
            get
            {
                string result = String.Format("utility.alert('{0}').done(function(){{{1}}});", (Message ?? "").Replace("\\", "\\\\").Replace("'", "\\'"),
                    string.IsNullOrEmpty(RedirectUrl) ? "" : $"window.location='{RedirectUrl}';");
                if (this.IsFireEvent)
                {
                    var f = new FireEventJavaScript();
                    f.EventName = this.EventName;
                    f.Data = this.Data;
                    result += f.Script;
                }
                return result;
            }
        }

    }
}
