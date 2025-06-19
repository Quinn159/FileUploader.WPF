using FileUploader.WPF.Models;
using FileUploader.WPF.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FileUploader.WPF.ViewModels
{



    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly FileUploadService _uploadService;

        public ObservableCollection<UploadFileInfo> Files { get; } = new ObservableCollection<UploadFileInfo>();

        public ICommand SelectFileCommand { get; }
        public ICommand SelectSingleFileCommand { get; }
        public ICommand UploadCommand { get; }

        public MainWindowViewModel()
        {
            _uploadService = new FileUploadService("http://127.0.0.1:8080");
            SelectFileCommand = new RelayCommand(SelectFile);
            SelectSingleFileCommand = new RelayCommand(SelectSingleFile);
            UploadCommand = new RelayCommand(Upload);
        }

        private void SelectFile()
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                string[] filePaths = Directory.GetFiles(dialog.FolderName);
                foreach (string filePath in filePaths)
                {
                    var fileInfo = new FileInfo(filePath);
                    Files.Add(new UploadFileInfo
                    {
                        originFileName = fileInfo.FullName,
                        size = fileInfo.Length,
                        Status = "Ready",
                        Progress = 0,
                        md5 = string.Empty,
                    });
                }
            }
        }

        private void SelectSingleFile()
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                var fileInfo = new FileInfo(dialog.FileName);
                Files.Add(new UploadFileInfo
                {
                    originFileName = fileInfo.Name,
                    size = fileInfo.Length,
                    Status = "Ready",
                    Progress = 0,
                    md5 = string.Empty,

                });
            }
        }

        private async void Upload()
        {

            foreach (var file in Files.Where(f => f.Status == "Ready"))
            {

                var progress = new Progress<UploadFileInfo>(info =>
                {
                    file.Progress = info.Progress;
                    file.Status = info.Status;
                    file.md5 = info.md5;
                    OnPropertyChanged(nameof(Files));
                });

                await _uploadService.UploadFileAsync(file.originFileName, progress);

            }

        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }
}
