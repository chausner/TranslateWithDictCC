using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TranslateWithDictCC.ViewModels;

abstract class ViewModel : INotifyPropertyChanged
{
    protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}
