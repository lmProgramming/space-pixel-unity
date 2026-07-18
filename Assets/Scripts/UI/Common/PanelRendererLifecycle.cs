using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Common
{
    public sealed class PanelRendererLifecycle
    {
        private readonly MonoBehaviour _host;
        private readonly PanelRenderer _panelRenderer;
        private readonly IPanelRenderable _renderable;

        private bool _isRegistered;
        private int _uiVersion = -1;

        public PanelRendererLifecycle(
            PanelRenderer panelRenderer,
            IPanelRenderable renderable,
            MonoBehaviour host)
        {
            _panelRenderer = panelRenderer ?? throw new ArgumentNullException(nameof(panelRenderer));
            _renderable = renderable ?? throw new ArgumentNullException(nameof(renderable));
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public bool IsBound { get; private set; }

        public void OnHostEnabled()
        {
            _uiVersion = -1;
            ReregisterCallback();
            _host.StartCoroutine(RetryBindNextFrameIfNeeded());
        }

        public void OnHostDisabled()
        {
            Unbind();
        }

        public void Unregister()
        {
            if (!_isRegistered)
                return;

            _panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            _isRegistered = false;
            Unbind();
        }

        private void ReregisterCallback()
        {
            if (_isRegistered)
            {
                _panelRenderer.UnregisterUIReloadCallback(OnUIReload);
                _isRegistered = false;
            }

            _panelRenderer.RegisterUIReloadCallback(OnUIReload);
            _isRegistered = true;
        }

        private IEnumerator RetryBindNextFrameIfNeeded()
        {
            yield return null;

            if (IsBound)
                yield break;

            _uiVersion = -1;
            ReregisterCallback();
        }

        private void OnUIReload(
            PanelRenderer renderer,
            VisualElement root,
            int version)
        {
            if (version == _uiVersion && IsBound)
                return;

            if (version != _uiVersion)
                Unbind();

            _uiVersion = version;

            if (root == null)
                return;

            _renderable.BindUI(root);
            IsBound = true;
        }

        private void Unbind()
        {
            if (!IsBound)
                return;

            _renderable.UnbindUI();
            IsBound = false;
        }
    }
}