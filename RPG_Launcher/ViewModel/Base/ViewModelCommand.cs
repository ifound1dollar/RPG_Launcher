using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RPG_Launcher.ViewModel.Base
{
    public class ViewModelCommand : ICommand
    {
        private readonly Action<object?> executeAction;
        private readonly Predicate<object?>? canExecutePredicate;

        public ViewModelCommand(Action<object?> executeAction)
        {
            this.executeAction = executeAction;
            this.canExecutePredicate = null;
        }

        public ViewModelCommand(Action<object?> executeAction, Predicate<object?> canExecutePredicate)
        {
            this.executeAction = executeAction;
            this.canExecutePredicate = canExecutePredicate;
        }



        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }



        public bool CanExecute(object? parameter)
        {
            return (canExecutePredicate == null) ? true : canExecutePredicate(parameter);
        }

        public void Execute(object? parameter)
        {
            executeAction(parameter);
        }
    }
}
