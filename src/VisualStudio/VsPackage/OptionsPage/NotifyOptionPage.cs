using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Shell;

namespace GoogleTestAdapter.Remote.VisualStudio.Package.OptionsPage
{
    public abstract class NotifyOptionPage : DialogPage, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetProperty<T>(ref T field,
                                      T newValue,
                                      [CallerMemberName] string? propertyName = null)
            => SetProperty(ref field, newValue, EqualityComparer<T>.Default, propertyName);
        protected void SetProperty<T>(ref T field,
                                      T newValue,
                                      IEqualityComparer<T> comparer,
                                      [CallerMemberName] string? propertyName = null)
        {
            if (!comparer.Equals(field, newValue))
            {
                field = newValue;
                this.PropertyChanged?.Invoke(this, new(propertyName));
            }
        }
    }
}