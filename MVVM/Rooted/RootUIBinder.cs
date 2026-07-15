using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace DevKit.UI.MVVM
{
    /// <summary>
    /// Root UI Object with sorted Canvas and UIDocument layers and Object pooling.
    /// </summary>
    public class RootUIBinder : MonoBehaviour, IRootUIBinder, IRootUI
    {
        [SerializeField] private Transform _layersRoot;

        [Header("UIDocument Pool"), Space(4)]
        [SerializeField] private UIDocument _documentPrefab;
        [SerializeField] private PanelSettings _panelSettingsAsset;
        [SerializeField, Min(0)] private int _maxUIDocuments = 10;
        [SerializeField, Min(0)] private int _defaultUIDocuments = 1;

        [Header("Canvas Pool"), Space(4)]
        [SerializeField] private Canvas _canvasPrefab;
        [SerializeField, Min(0)] private int _maxCanvases = 10;
        [SerializeField, Min(0)] private int _defaultCanvases = 1;

        private readonly HashSet<IAttachableView> _boundViews = new();
        private ObjectPool<UIDocument> _documentPool;
        private ObjectPool<Canvas> _canvasPool;
        private readonly Dictionary<int, Canvas> _canvasLayersMap = new();
        private readonly Dictionary<float, UIDocument> _documentLayersMap = new();
        private readonly Dictionary<GameObject, Canvas> _canvasViewParentMap = new();
        private readonly Dictionary<VisualElement, UIDocument> _documentViewParentMap = new();
        private readonly HashSet<Canvas> _activeCanvases = new();
        private readonly HashSet<UIDocument> _activeDocuments = new();

        #region MonoBehaviour
        private void Awake()
        {
            _documentPool = new ObjectPool<UIDocument>(
                createFunc: () =>
                {
                    var document = Instantiate(_documentPrefab, _layersRoot, false);
                    document.panelSettings = Instantiate(_panelSettingsAsset);
                    return document;
                },
                actionOnGet: x => x.gameObject.SetActive(true),
                actionOnRelease: x => x.gameObject.SetActive(false),
                actionOnDestroy: x => Destroy(x.gameObject),
                defaultCapacity: _defaultUIDocuments,
                maxSize: _maxUIDocuments);

            _canvasPool = new ObjectPool<Canvas>(
                createFunc: () =>
                {
                    var canvas = Instantiate(_canvasPrefab, _layersRoot, false);
                    return canvas;
                },
                actionOnGet: x => x.gameObject.SetActive(true),
                actionOnRelease: x => x.gameObject.SetActive(false),
                actionOnDestroy: x => Destroy(x.gameObject),
                defaultCapacity: _defaultCanvases,
                maxSize: _maxCanvases);
        }

        private void OnDestroy()
        {
            _documentPool?.Clear();
            _canvasPool?.Clear();
        }
        #endregion

        #region IRootUIBinder
        public void SetView(IAttachableView view)
        {
            ClearViews();
            AddView(view);
        }

        public void SetViews(IEnumerable<IAttachableView> views)
        {
            ClearViews();
            AddViews(views);
        }

        public void SetViews(params IAttachableView[] views)
        {
            ClearViews();
            AddViews(views);
        }

        public void AddView(IAttachableView view)
        {
            view.Attach(this);
            _boundViews.Add(view);
        }

        public void AddViews(params IAttachableView[] views)
        {
            foreach (var view in views)
                AddView(view);
        }

        public void AddViews(IEnumerable<IAttachableView> views)
        {
            foreach (var view in views)
                AddView(view);
        }

        public void ClearView(IAttachableView view)
        {
            view.Detach(this);
            _boundViews.Remove(view);
        }

        public void ClearViews()
        {
            foreach (var view in _boundViews.ToArray())
                ClearView(view);
        }
        #endregion

        #region IRootUI
        /// <summary>
        /// This is only used by UI Toolkit Views in terms of implementation Visitor pattern. 
        /// For the scene UI binding use SetViews or AddViews method instead.
        /// </summary>
        void IRootUI.Attach(VisualElement visualElement, int sortOrder)
        {
            if (!_documentLayersMap.TryGetValue(sortOrder, out var document))
            {
                document = _documentPool.Get();
                document.panelSettings.sortingOrder = sortOrder;
                _activeDocuments.Add(document);

                _documentLayersMap.Add(sortOrder, document);
            }

            if (_documentViewParentMap.TryGetValue(visualElement, out var oldParentDocument))
            {
                // reattaching existing UI
                visualElement.RemoveFromHierarchy(); // mb for event dispatch?

                document.rootVisualElement.Add(visualElement);
                _documentViewParentMap[visualElement] = document;

                // release the parent if needed
                if (oldParentDocument.rootVisualElement.childCount == 0)
                {
                    _documentLayersMap.Remove(oldParentDocument.panelSettings.sortingOrder);
                    if (_activeDocuments.Remove(oldParentDocument))
                        _documentPool.Release(oldParentDocument);
                    else
                        Debug.LogWarning($"<color=#FF8F5C>UIDocument repeated releasing detected</color>");
                }
            }
            else
            {
                document.rootVisualElement.Add(visualElement);
                _documentViewParentMap[visualElement] = document;
            }
        }

        /// <summary>
        /// This is used by Canvas Views in terms of implementation Visitor pattern. 
        /// Can also be used with UI Toolkit View gameobjects just to hold them.
        /// For the scene UI binding use SetViews or AddViews method instead.
        /// </summary>
        void IRootUI.Attach(GameObject gameObjectUI, int sortOrder)
        {
            if (!_canvasLayersMap.TryGetValue(sortOrder, out var canvas))
            {
                canvas = _canvasPool.Get();
                canvas.sortingOrder = sortOrder;
                _activeCanvases.Add(canvas);

                _canvasLayersMap.Add(sortOrder, canvas);
            }

            if (_canvasViewParentMap.TryGetValue(gameObjectUI, out var oldParentCanvas))
            {
                // reattaching existing UI
                gameObjectUI.transform.SetParent(canvas.transform, false);
                _canvasViewParentMap[gameObjectUI] = canvas;

                if (oldParentCanvas.transform.childCount == 0)
                {
                    _canvasLayersMap.Remove(oldParentCanvas.sortingOrder);
                    if (_activeCanvases.Remove(oldParentCanvas))
                        _canvasPool.Release(oldParentCanvas);
                    else
                        Debug.LogWarning($"<color=#FF8F5C>Canvas repeated releasing detected</color>");
                }
            }
            else
            {
                gameObjectUI.transform.SetParent(canvas.transform, false);
                _canvasViewParentMap[gameObjectUI] = canvas;
            }
        }

        void IRootUI.Detach(VisualElement visualElement)
        {
            visualElement.RemoveFromHierarchy();

            if (!_documentViewParentMap.TryGetValue(visualElement, out var document))
                return;

            _documentViewParentMap.Remove(visualElement);

            if (document.rootVisualElement.childCount != 0)
                return;

            _documentLayersMap.Remove(document.panelSettings.sortingOrder);

            if (_activeDocuments.Remove(document))
                _documentPool.Release(document);
            else
                Debug.LogWarning($"<color=#FF8F5C>UIDocument repeated releasing detected</color>");
        }

        void IRootUI.Detach(GameObject gameObjectUI)
        {
            Destroy(gameObjectUI);
            gameObjectUI.transform.SetParent(null);

            if (!_canvasViewParentMap.TryGetValue(gameObjectUI, out var canvas))
                return;

            _canvasViewParentMap.Remove(gameObjectUI);

            if (canvas.transform.childCount != 0)
                return;

            _canvasLayersMap.Remove(canvas.sortingOrder);

            if (_activeCanvases.Remove(canvas))
                _canvasPool.Release(canvas);
            else
                Debug.LogWarning($"<color=#FF8F5C>Canvas repeated releasing detected</color>");
        }
        #endregion
    }
}