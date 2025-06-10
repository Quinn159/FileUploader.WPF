using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUploader.WPF.Models
{

    public class ChunkInfo
    {
        public string Url { get; set; }
        public byte[] Data { get; set; }
        public int Index { get; set; }
        public string Hash { get; set; }
    }


}
