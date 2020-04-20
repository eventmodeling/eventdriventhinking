namespace EventDrivenThinking.Ui
{
    public interface IViewModelFor<in TSource>
    {
        void LoadChild(TSource source);
    }

    public interface IViewModelChildOf<in TParent>
    {
        void SetParent(TParent parent);
    }

    public class ViewModelFor<TItem, TViewModel> : ViewModelBase<TViewModel>, IViewModelFor<TItem>
        where TViewModel : ViewModelBase<TViewModel>
    {
        private TItem _value;

        public TItem Value
        {
            get => _value;
            protected set => base.SetProperty(ref _value, value);
        }

        public void LoadChild(TItem source)
        {
            Value = source;
        }
    }
}