using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;

namespace FileUploader.WPF.Models
{
    public class InitUploadResponse
    {
        public string uploadId { get; set; }
        public List<string> urls { get; set; }
    }
}