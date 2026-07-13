using UnityEngine;
using UnityEngine.UI;

namespace DevKit.UI.MVVM.Bases
{
    public class AttachableCanvasWindow<T> : AttachableCanvasScreen<T> where T : IScreenViewModel
    {
        [SerializeField] protected Button _closeButton;
        [SerializeField] protected Button _closeBackground;

        protected override void OnBind(T viewModel)
        {
            base.OnBind(viewModel);
            BindClosingControls();
        }

        /// <summary>
        /// Initializes event subscriptions for close controls (<see cref="_closeButton"/>,
        /// <see cref="_closeBackground"/>)
        /// </summary>
        protected virtual void BindClosingControls()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);
            if (_closeBackground != null)
                _closeBackground.onClick.AddListener(OnCloseClicked);
        }

        protected virtual void OnCloseClicked()
        {
            ViewModel.StartClosing();
        }

        public override void Dispose()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseClicked);
            if (_closeBackground != null)
                _closeBackground.onClick.RemoveListener(OnCloseClicked);
                
            base.Dispose();
        }
    }
}