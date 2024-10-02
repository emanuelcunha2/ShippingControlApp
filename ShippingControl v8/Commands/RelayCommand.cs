using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ShippingControl_v8.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Action<object> _executeWithParameter;
        private readonly Func<bool> _canExecute;
        private readonly Func<object, bool> _canExecuteWithParameter;

        // Event for notifying UI about changes in the command's ability to execute
        public event EventHandler CanExecuteChanged;

        // Constructor for non-parameterized command
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Constructor for parameterized command
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _executeWithParameter = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteWithParameter = canExecute;
        }

        // Implementations of ICommand interface

        public bool CanExecute(object parameter)
        {
            if (_canExecute != null)
                return _canExecute();
            if (_canExecuteWithParameter != null)
                return _canExecuteWithParameter(parameter);
            return true;
        }

        public void Execute(object parameter)
        {
            if (_execute != null)
                _execute();
            else if (_executeWithParameter != null)
                _executeWithParameter(parameter);
        }

        // Method to manually raise the CanExecuteChanged event
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
