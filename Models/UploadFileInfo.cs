using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace FileUploader.WPF.Models
{
    public class UploadFileInfo : INotifyPropertyChanged
    {
        private string _md5;
        private int _progress;
        private string _status;
        private long _uploadedSize;
        
        public string originFileName { get; set; }
        public long size { get; set; }
        public string md5 
        { 
            get => _md5; 
            set
            {
                if (_md5 != value)
                {
                    _md5 = value;
                    OnPropertyChanged(nameof(md5));
                }
            } 
        }
        public int Progress 
        { 
            get => _progress; 
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged(nameof(Progress));
                }
            } 
        }
        public int chunkCount { get; set; }
        public string Status 
        { 
            get => _status; 
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            } 
        }
        public long UploadedSize 
        { 
            get => _uploadedSize; 
            set
            {
                if (_uploadedSize != value)
                {
                    _uploadedSize = value;
                    OnPropertyChanged(nameof(UploadedSize));
                }
            } 
        }
        public string uploadId { get; set; }
        public HashSet<int> UploadedParts { get; set; } = new HashSet<int>();
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
