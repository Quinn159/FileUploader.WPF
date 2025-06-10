using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUploader.WPF.Models
{
    public class UploadFileInfo
    {
        public string originFileName { get; set; }
        public long size { get; set; }
        public string md5 { get; set; }
        public int Progress { get; set; }
        public int chunkCount { get; set; }
        public string Status { get; set; }
        public long UploadedSize { get; set; }
        public string uploadId { get; set; }
        public List<int> UploadedParts { get; set; } = [];
    }
}
