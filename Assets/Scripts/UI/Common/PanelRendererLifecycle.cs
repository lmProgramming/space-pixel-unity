using System;
using UnityEngine.UIElements;

namespace UI.Common
{
    public sealed class PanelRendererLifecycle
    {
        private readonly PanelRenderer _panelRenderer;
        private readonly IPanelRenderable _renderable;

        private int _uiVersion = -1;

        public PanelRendererLifecycle(PanelRenderer panelRenderer,
            IPanelRenderable renderable)
        {
            _panelRenderer = panelRenderer ?? throw new ArgumentNullException(nameof(panelRenderer));
            _renderable = renderable ?? throw new ArgumentNullException(nameof(renderable));
        }

        public VisualElement Root { get; private set; }

        public void OnHostEnabled()
        {
            _uiVersion = -1;
            _panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        public void OnHostDisabled()
        {
            _panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }

        private void OnUIReload(
            PanelRenderer renderer,
            VisualElement root,
            int version)
        {
            if (version == _uiVersion)
                return;

            _uiVersion = version;

            Root = root;

            _renderable.BindUI(root);
        }
    }
}