# DevKit UI
[![Unity Version](https://img.shields.io/badge/Unity-2022.3+-blue.svg)](https://unity.com/)
[![Size](https://img.shields.io/github/repo-size/Quaximur/DevKit.UI?label=Size&color=blue)](https://github.com/Quaximur/DevKit.UI)    
Minimalist MVVM library for Unity.    

* 🫧Overwhelmingly Neat;    
* ⚡️Lighting Fast;    
* ♻️Charmingly Reusable;    
* 🔥Helluva Extensible.    

## 🚀 Quick Start

### Installation

**Unity Package Manager**

Add to `Packages/manifest.json`:
```json
"com.quaximur.devkit.ui": "https://github.com/Quaximur/DevKit.UI.git"
```

## 💡Usage Example
The ViewModel handles your UI logic and state, completely independent of the view:
```csharp
using DevKit.UI.MVVM.Bases;
using UnityEngine;

namespace Courier.UI
{
    public class MainMenuViewModel : ScreenViewModel
    {
        public void StartGameplay()
        {
            Debug.Log("Start Gameplay!");
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}
```
The View is responsible for UI elements and user interactions. It binds to the ViewModel:
```csharp
using DevKit.UI.MVVM.Bases;
using UnityEngine;
using UnityEngine.UIElements;

namespace Courier.UI
{
    public class MainMenuView : AttachableToolkitScreen<MainMenuViewModel>
    {
        // Add elements with specified names in uxml file
        [SerializeField] private string _playButtonName = "PlayButton";
        [SerializeField] private string _exitButtonName = "ExitButton";

        private Button _playButton;
        private Button _exitButton;

        protected override void OnInit()
        {
            // Find references. Initialize elements
            _playButton = Root.Q<Button>(name: _playButtonName);
            _exitButton = Root.Q<Button>(name: _exitButtonName);
        }

        protected override void OnBind(MainMenuViewModel viewModel)
        {
            // Bind to ViewModel events. Pass input to the ViewModel.
            base.OnBind(viewModel);

            _playButton.RegisterCallback<ClickEvent>(OnStartClicked);
            _exitButton.RegisterCallbackOnce<ClickEvent>(OnQuitClicked);
        }

        private void OnStartClicked(ClickEvent clickEvent)
        {
            ViewModel.StartGameplay();
        }

        private void OnQuitClicked(ClickEvent clickEvent)
        {
            ViewModel.Quit();
        }
    }
}
```
Connect your View and ViewModel using a Binder:
```csharp
using System;
using Courier.UI;
using DevKit.UI.MVVM;
using DevKit.UI.MVVM.Bases;
using UnityEngine;

namespace Courier.Infrastructure.Bootstraps
{
    public class MainMenuBootstrap : MonoBehaviour
    {
        [SerializeField] private MainMenuView _mainMenuViewPrefab;
        [SerializeField] private RootUIBinder _rootUIBinderPrefab;

        private void Start()
        {
            var rootBinder = Instantiate(_rootUIBinderPrefab);

            Func<MainMenuViewModel> viewModelFactory = () => new MainMenuViewModel();
            Func<MainMenuView> viewFactory = () => Instantiate(_mainMenuViewPrefab);

            var menuBinder = new SimpleAttachBinder<MainMenuView, MainMenuViewModel>(viewModelFactory, 
                rootBinder, viewFactory);

            var mainMenuViewModel = menuBinder.Open();
        }
    }
}
```
## 🎯 Sample Project

Check out the [ITCafe Sample Project](https://github.com/Quaximur/ITCafe)
