using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RPG_Launcher.ViewModel.Base
{
    /// <summary>
    /// Defines a custom ICommand object with an associated CanExecute() and Execute() method. Supports both synchronous
    ///  and async methods. Asynchronous methods must always return a Task object, while synchronous methods must return void.
    /// </summary>
    public class ViewModelCommand : ICommand
    {
        private readonly Action<object?>? executeAction;
        private readonly Func<object?, Task>? executeFunctionAsync;
        private readonly Predicate<object?>? canExecutePredicate;
        private bool isWorking = false;

        public ViewModelCommand(Action<object?> executeAction)
        {
            this.executeAction = executeAction;
            this.canExecutePredicate = null;

            this.executeFunctionAsync = null;
        }

        public ViewModelCommand(Func<object?, Task> executeFunction)
        {
            this.executeFunctionAsync = executeFunction;
            this.canExecutePredicate = null;

            this.executeAction = null;
        }

        public ViewModelCommand(Action<object?> executeAction, Predicate<object?> canExecutePredicate)
        {
            this.executeAction = executeAction;
            this.canExecutePredicate = canExecutePredicate;

            this.executeFunctionAsync = null;
        }

        public ViewModelCommand(Func<object?, Task> executeFunction, Predicate<object?> canExecutePredicate)
        {
            this.executeFunctionAsync = executeFunction;
            this.canExecutePredicate = canExecutePredicate;

            this.executeAction = null;
        }



        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }



        public bool CanExecute(object? parameter)
        {
            if (isWorking) return false;

            return (canExecutePredicate == null) ? true : canExecutePredicate(parameter);
        }

        public async void Execute(object? parameter)
        {
            if (executeAction != null)
            {
                executeAction.Invoke(parameter);
                return;
            }
            
            if (executeFunctionAsync != null)
            {
                isWorking = true;
                await executeFunctionAsync.Invoke(parameter);
                isWorking = false;
            }
        }
    }
}
