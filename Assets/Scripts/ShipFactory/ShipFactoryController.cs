using Ships;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShipFactory
{
    [RequireComponent(typeof(UIDocument))]
    public class ShipFactoryController : MonoBehaviour
    {
        [SerializeField] private ModulePrefabLibrary modulePrefabLibrary;
        [SerializeField] private Ship initialShip;
        private ShipFactoryCanvasController _canvasController;
        private ModulePaletteController _paletteController;

        private UIDocument _uiDocument;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();

            if (modulePrefabLibrary == null)
                Debug.LogError("[ShipFactoryController] ModulePrefabLibrary is not assigned!", this);
        }

        private void Update()
        {
            _canvasController?.UpdateOverlays();
        }

        private void OnEnable()
        {
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            _canvasController = new ShipFactoryCanvasController(root);
            _paletteController = new ModulePaletteController(root, modulePrefabLibrary);

            _paletteController.OnModuleDragStarted += OnModuleDragStarted;

            if (initialShip != null)
                _canvasController.SetShip(initialShip);
        }

        private void OnDisable()
        {
            if (_paletteController != null)
                _paletteController.OnModuleDragStarted -= OnModuleDragStarted;
        }


        private void OnModuleDragStarted(GameObject prefab, Vector2 startPointerPosition)
        {
            _canvasController.BeginModuleDrop(prefab, startPointerPosition);
        }
    }
}