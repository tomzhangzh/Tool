using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Text;

namespace TUI.Services.DBModel
{
    public partial class Attachment
    {
        [NotMapped]
        public string WebPath
        {
            get
            {
                if (string.IsNullOrEmpty(this.FileName))
                    return string.Empty;
                else
                {
                    var vFormat = "/Upload/{0}/{1}/";
                    var vPath = string.Format(vFormat, this.ObjType ?? "Default", (this.ObjID ?? 0) == 0 ? this.TempIdForNew : this.ObjID.ToString());
                    return vPath;
                }

            }
        }
        [NotMapped]
        public string WebFilePath
        {
            get
            {
                return this.WebPath + this.FileName;

            }
        }
        [NotMapped]
        public string WebPathThumb
        {
            get
            {
                return this.WebPath.Replace("Upload/", "Upload/Thumb/");
            }
        }
        [NotMapped]
        public string WebFilePathThumb
        {
            get
            {
                return this.WebPathThumb + this.FileName;

            }
        }
        [NotMapped]
        public string Extension
        {
            get
            {
                if (string.IsNullOrEmpty(this.FileName))
                    return string.Empty;
                else
                {
                    return Path.GetExtension(this.FileName);
                }

            }
        }

    }
}
