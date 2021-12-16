using Microsoft.Toolkit.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NewLaserProject.Classes
{
    internal class TeachCommand //: ICommand
    {
        public event EventHandler? CanExecuteChanged;

        private Task _execute;
        private Func<bool> _canExecute;

        public TeachCommand(Task execute, Func<bool> canExecute = null)
        {
            Guard.IsNotNull(execute, $"{nameof(execute)} musn't be null");
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute()
        {
            return _canExecute == null || _canExecute();
        }

        public async Task Execute()
        {
            await _execute;
        }
    }
}
