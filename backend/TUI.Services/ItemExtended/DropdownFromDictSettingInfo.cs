using TUI.Services.DBModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TUI.Services.Model
{

    public class ItemExtendedBase
    {
        public ItemExtendedBase()
        {

        }
        public static ItemExtendedBase GetExtendProperty(DETAILITEM item)
        {
            if (item.COLUMNTYPE == "TextBox")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new TextBoxSettingInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<TextBoxSettingInfo>();
                }
            }
            if (item.COLUMNTYPE == "Password")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new PasswordSettingInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<PasswordSettingInfo>();
                }
            }
            else if (item.COLUMNTYPE == "DropdownFromDict")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new DropdownFromDICTSETTINGInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<DropdownFromDICTSETTINGInfo>();
                }
            }
            else if (item.COLUMNTYPE == "Selector")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new SelectorInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<SelectorInfo>();
                }
            }
            else if (item.COLUMNTYPE == "Lookup")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new LookupSettingInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<LookupSettingInfo>();
                }
            }
            else if (item.COLUMNTYPE == "LookupSelector")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new LookupSelectorSettingInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<LookupSelectorSettingInfo>();
                }
            }
            else if (item.COLUMNTYPE == "RadioButtonListFromDict")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new RadioButtonListFromDICTSETTINGInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<RadioButtonListFromDICTSETTINGInfo>();
                }
            }
            else if (item.COLUMNTYPE == "DatePicker")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new DatePickerSettingInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<DatePickerSettingInfo>();
                }
            }
            else if (item.COLUMNTYPE == "Prompt")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new PromptSettingInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<PromptSettingInfo>();
                }
            }
            else if (item.COLUMNTYPE == "DropdownAjax")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new DropdownAjaxSettingInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<DropdownAjaxSettingInfo>();
                }
            }
            else if (item.COLUMNTYPE == "ReadOnlyTextBox")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new ReadOnlyTextBoxSettingInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<ReadOnlyTextBoxSettingInfo>();
                }
            }
            else if (item.COLUMNTYPE == "TextArea")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new TextAreaSettingInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<TextAreaSettingInfo>();
                }
            }
            else if (item.COLUMNTYPE == "Editor")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new EditorSettingInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<EditorSettingInfo>();
                }
            }
            else if (item.COLUMNTYPE == "UploadButton")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new UploadButtonSettingInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<UploadButtonSettingInfo>();
                }
            }
            else if (item.COLUMNTYPE == "WordFile")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new WordFileSettingInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<WordFileSettingInfo>();
                }
            }
            else if (item.COLUMNTYPE == "UploadImage")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new UploadImageSettingInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<UploadImageSettingInfo>();
                }
            }
            else if (item.COLUMNTYPE == "SwitchButton")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new SwitchButtonSettingInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<SwitchButtonSettingInfo>();
                }
            }
            return new ItemExtendedBase();
        }
        public static void SetExtendPrperty<T>(DETAILITEM item, T extendInfo) where T : ItemExtendedBase
        {
            item.EXTENDEDPROPERTIES = extendInfo.ToJSON();
        }

    }
    public class SelectorInfo : DropdownFromDICTSETTINGInfo
    {
        public SelectorInfo()
        {

        }
        public string TableName { get; set; }
        public string Type { get; set; }
        public bool WithEmpty { get; set; }

    }
    public class DropdownFromDICTSETTINGInfo : ItemExtendedBase
    {
        public DropdownFromDICTSETTINGInfo()
        {

        }
        public string TableName { get; set; }
        public string Type { get; set; }
        public bool WithEmpty { get; set; }

    }
    public class RadioButtonListFromDICTSETTINGInfo : ItemExtendedBase
    {
        public RadioButtonListFromDICTSETTINGInfo()
        {

        }
        public string TableName { get; set; }
        public string Type { get; set; }


    }

    public class TextBoxSettingInfo : ItemExtendedBase
    {
        public string PlaceHolder { get; set; }
        public string AddonText { get; set; }
        public string AddonIconClass { get; set; }

    }
    public class PasswordSettingInfo : TextBoxSettingInfo
    {


    }
    public class DatePickerSettingInfo : ItemExtendedBase
    {
        public DatePickerSettingInfo()
        {
            this.Format = "yyyy-MM-dd";
        }
        public string Format { get; set; }
    }

    public class PromptSettingInfo : ItemExtendedBase
    {
        public string UrlParams { get; set; }
        public string ReturnParams { get; set; }
        public string URL { get; set; }
        public string Display { get; set; }
        public PromptSettingInfo()
        {
            this.Display = "xxxx.aaa";
            this.UrlParams = (new { a = "xxx", b = "yyy" }).ToJSON();
            this.ReturnParams = (new { a = "xxx", b = "yyy" }).ToJSON();
        }

    }
    public class WordFileSettingInfo : ItemExtendedBase
    {
        public string FileType { get; set; }
        public string ObjType { get; set; }
        public string ObjIDName { get; set; }

    }
    public class DropdownAjaxSettingInfo : ItemExtendedBase
    {
        public string UrlParams { get; set; }
        public string UrlParamsInPage { get; set; }
        public string ReturnParams { get; set; }
        public string URL { get; set; }
        public string Display { get; set; }
        public int minimumInputLength { get; set; }
        public int? dropdownWidth { get; set; }
        public DropdownAjaxSettingInfo()
        {
            this.Display = "xxxx.aaa";
            this.UrlParams = (new { a = "xxx", b = "yyy" }).ToJSON();
            this.UrlParamsInPage = (new { a = "xxx", b = "yyy" }).ToJSON();
            this.ReturnParams = (new { a = "xxx", b = "yyy" }).ToJSON();
            this.minimumInputLength = 1;
        }

    }
    public class LookupSettingInfo : ItemExtendedBase
    {
        public LookupSettingInfo()
        {

        }
        public string TableName { get; set; }
        public string NameField { get; set; }
        public string ValueField { get; set; }
        public string PlusSQL { get; set; }
        public bool WithEmpty { get; set; }
        public bool IsMutil { get; set; }
    }
    public class LookupSelectorSettingInfo : LookupSettingInfo
    { }
    public class ReadOnlyTextBoxSettingInfo : ItemExtendedBase
    {
        public ReadOnlyTextBoxSettingInfo()
        {
            this.Rows = 5;
            this.IsTextArea = false;
        }
        public string Display { get; set; }
        public bool IsTextArea { get; set; }
        public int Rows { get; set; }
    }
    public class TextAreaSettingInfo : ItemExtendedBase
    {
        public TextAreaSettingInfo()
        {
            this.Rows = 5;
        }
        public int Rows { get; set; }
    }
    public class EditorSettingInfo : ItemExtendedBase
    {
        public EditorSettingInfo()
        {
            this.Height = 100;
        }
        public int Height { get; set; }
    }
    public class SwitchButtonSettingInfo : ItemExtendedBase
    {
        public SwitchButtonSettingInfo()
        {
            this.YesLabel = "是";
            this.NoLabel = "否";
        }
        public string Label { get; set; }
        public string YesLabel { get; set; }
        public string NoLabel { get; set; }
    }
    public class UploadButtonSettingInfo : ItemExtendedBase
    {
        public UploadButtonSettingInfo()
        {
            this.Params = new { ID = "@ID", Type = "@xxx" }.ToJSON();
            this.uploadFileName = "UploadFile";
            this.uploadText = "上传";
            this.ExtId = "ExtId";
            this.ReloadEvent = "Uploaded";
            this.UploadUrl = "Upload/Index";
        }
        public string Params { get; set; }
        public string uploadFileName { get; set; }
        public string uploadText { get; set; }
        public string ExtId { get; set; }
        public bool ReloadPage { get; set; }
        public string ReloadEvent { get; set; }
        public string UploadUrl { get; set; }
    }
    public class UploadImageSettingInfo : ItemExtendedBase
    {
        public UploadImageSettingInfo()
        {
            this.Params = new { ID = "@ID", Type = "@xxx" }.ToJSON();
            this.uploadFileName = "UploadFile";
            this.uploadText = "上传";
            this.ExtId = "ExtId";
            this.ReloadEvent = "Uploaded";
            this.UploadUrl = "Upload/Index";
            this.Width = 140;
            this.Height = 140;
            this.AttchementID = "AttachmentID";
        }
        public string Params { get; set; }
        public string uploadFileName { get; set; }
        public string uploadText { get; set; }
        public string ExtId { get; set; }
        public bool ReloadPage { get; set; }
        public string ReloadEvent { get; set; }
        public string UploadUrl { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string AttchementID { get; set; }
    }
}
