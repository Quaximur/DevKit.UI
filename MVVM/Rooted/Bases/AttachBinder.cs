using System;

namespace DevKit.UI.MVVM.Bases
{
    public abstract class AttachBinder<TView, TViewModel> : AttachBinder<TView, TViewModel, TViewModel>
        where TView : IScreenAttach<TViewModel>, IDisposableNotifier
        where TViewModel : IScreenViewModel
    {
        protected AttachBinder(IRootUIBinder rootUIBinder) : base(rootUIBinder)
        {
        }
    }

    public abstract class AttachBinder<TView, TViewModel, AViewModel> : BaseBinder<TView, TViewModel, AViewModel>
        where TView : IScreenAttach<AViewModel>, IDisposableNotifier
        where TViewModel : AViewModel
        where AViewModel : IScreenViewModel
    {
        protected readonly IRootUIBinder _rootUIBinder;

        public AttachBinder(IRootUIBinder rootUIBinder)
        {
            _rootUIBinder = rootUIBinder;
        }

        public override AViewModel Open()
        {
            if (_currentViewModel != null) // if already exists
                return _currentViewModel;

            _currentViewModel = GetViewModel();

            _view = GetView();
            _view.Init();
            _view.Bind(_currentViewModel);

            var viewNoRef = _view;

            void OnClosingOnce()
            {
                _currentViewModel.OnClosingCompleted -= OnClosingOnce;
                _rootUIBinder.ClearView(viewNoRef);
            }

            _currentViewModel.OnClosingCompleted += OnClosingOnce;

            void OnDisposeOnce()
            {
                viewNoRef.OnDisposed -= OnDisposeOnce;
                
                _currentViewModel?.Dispose();
                _currentViewModel = default; // null

                DisposeInstances();
            }

            viewNoRef.OnDisposed += OnDisposeOnce;

            _rootUIBinder.AddView(viewNoRef);
            _currentViewModel.Open();

            return _currentViewModel;
        }

        public override void Close()
        {
            _currentViewModel?.StartClosing();
        }

        protected abstract TViewModel GetViewModel();

        protected abstract TView GetView();

        protected virtual void DisposeInstances()
        {
        }
    }
}