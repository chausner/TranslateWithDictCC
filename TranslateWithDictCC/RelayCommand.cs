using System;
using System.Windows.Input;

namespace TranslateWithDictCC
{
    class RelayCommand : ICommand
    {
        Action action;
        Func<bool> canExecuteEvaluator;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action action, Func<bool> canExecuteEvaluator)
        {
            this.action = action;
            this.canExecuteEvaluator = canExecuteEvaluator;
        }

        public RelayCommand(Action action)
            : this(action, null)
        {
        }

        public bool CanExecute(object parameter)
        {
            if (canExecuteEvaluator == null)
                return true;
            else
                return canExecuteEvaluator();
        }

        public void Execute(object parameter)
        {
            action();
        }

        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    class RelayCommand<T> : ICommand
    {
        Action<T> action;
        Func<T, bool> canExecuteEvaluator;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<T> action, Func<T, bool> canExecuteEvaluator)
        {
            this.action = action;
            this.canExecuteEvaluator = canExecuteEvaluator;
        }

        public RelayCommand(Action<T> action)
            : this(action, null)
        {
        }

        public bool CanExecute(object parameter)
        {
            if (canExecuteEvaluator == null)
                return true;
            else
                return canExecuteEvaluator((T)parameter);
        }

        public void Execute(object parameter)
        {
            action((T)parameter);
        }

        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
