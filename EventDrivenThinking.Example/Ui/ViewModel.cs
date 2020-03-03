using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EventDrivenThinking.Example.Ui
{
    public class ViewModel : Model, INotifyPropertyChanged
    {
        private ICollection<ClassX> _proxyItems;

        public override ICollection<ClassX> NestedItems
        {
            get
            {
                if(_proxyItems == null)
                    _proxyItems = new List<ClassX>(base.NestedItems);
                return _proxyItems;
            }
        }

        public override string StringArg
        {
            get { return base.StringArg; }
            set
            {
                var prv = StringArg;
                if (!EqualityComparer<string>.Default.Equals(prv, value))
                {
                    base.StringArg = value;
                    OnPropertyChanged();
                }
            }
        }

        public override DateTime DateTimeArg
        {
            get { return base.DateTimeArg; }
            set
            {
                var prv = DateTimeArg;
                if (!EqualityComparer<DateTime>.Default.Equals(prv, value))
                {
                    base.DateTimeArg = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}