using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUI.Services.Model
{
    public class UploadFlieInfo
    {
        public UploadFlieInfo()
        {

        }
        public UploadFlieInfo(FileInfo FileInfo)
        {
            this.FileInfo = FileInfo;
        }
        public FileInfo FileInfo { get; set; }
        public string FileName
        {
            get
            {
                return this.FileInfo.Name;
            }
        }
        public string FileExtension
        {
            get
            {
                return this.FileInfo.Extension;
            }
        }
        public string Src { get; set; }

        public static List<UploadFlieInfo> List(int ID, string Type, string ExtId)
        {
            var result = new List<UploadFlieInfo>();
            if (ID == 0 && string.IsNullOrEmpty(ExtId))
            {
                return result;
            }

            var vFormat = "Upload/{0}/{1}/";
            var vPath = string.Format(vFormat, Type ?? "Default", ID == 0 ? ExtId : ID.ToString());
            var dirPath = System.IO.Path.Combine( App.WebHostEnvironment.WebRootPath, vPath);
            if (Directory.Exists(dirPath))
            {
                var dir = new DirectoryInfo(dirPath);
                foreach (var file in dir.GetFiles())
                {
                    var info = new UploadFlieInfo(file);
                    info.Src = vPath + file.Name;
                    result.Add(info);
                }
            }

            return result;


        }
    }

}
