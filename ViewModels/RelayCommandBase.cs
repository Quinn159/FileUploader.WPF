using System.Windows.Input;

namespace FileUploader.WPF.ViewModels
{
    public class RelayCommandBase
    {

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}