using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DesignSystem.Runtime
{
    /// <summary>
    ///     Runtime helpers for the ds-* design system. Auto-attaches to every
    ///     UIDocument in the scene at load. Provides:
    ///     - Looping spinner rotation (USS transitions can't loop natively)
    ///     - Toggle-knob auto-injection: every
    ///     <Toggle class="ds-toggle" />
    ///     gets a
    ///     child
    ///     <VisualElement class="ds-toggle__knob" />
    ///     if one is missing
    ///     - Skeleton shimmer translation (sliding overlay)
    ///     - Dropdown popup chrome: Unity's GenericDropdownMenu renders under
    ///     panel.visualTree (a sibling of rootVisualElement), so UXML-imported
    ///     DesignSystem.uss never reaches the open list — this runtime loads
    ///     DesignSystemDropdownPopup.uss onto the panel root once per panel.
    ///     Authoring tip: hand-author the toggle knob in UXML when you can — it
    ///     avoids a one-frame "no knob" flash during template clone. The runtime
    ///     is the safety net for screens that didn't.
    /// </summary>
    [DisallowMultipleComponent]
    public class DesignSystemRuntime : MonoBehaviour
    {
        // ReSharper disable once UnusedMember.Local
        private const string SpinnerClass = "ds-spinner";
        private const string SpinnerActiveClass = "is-spinning";
        private const string ToggleClass = "ds-toggle";
        private const string ToggleKnobClass = "ds-toggle__knob";
        private const string SkeletonClass = "ds-skeleton";
        private const string ShimmerClass = "ds-skeleton__shimmer";
        private const string DropdownPopupStyleResource = "DesignSystemDropdownPopup";
        private const string RuntimeCursorTextureResource = "UI/Cursors/bibata-modern-classic-cursor-arrow";
        private const int RuntimeCursorMaxSizePx = 32;

        private static readonly Vector2 RuntimeCursorHotspot = new(2f, 2f);

        private static StyleSheet _dropdownPopupStylesheet;
        private static Texture2D _runtimeCursorTexture;
        private static Texture2D _runtimeCursorTexturePrepared;
        private static bool _runtimeCursorMissingWarningLogged;

        private UIDocument _doc;
        private float _spinAngle;
        private IVisualElementScheduledItem _spinTask;

        private void OnEnable()
        {
            _doc = GetComponent<UIDocument>();
            if (_doc == null) return;
            var root = _doc.rootVisualElement;
            if (root == null)
            {
                // The visual tree hasn't materialized yet (common when this
                // component is added in Awake). Defer one frame.
                _doc.rootVisualElement?.schedule.Execute(() => InitFor(_doc.rootVisualElement)).StartingIn(0);
                // Fallback: poll briefly until the root exists.
                SchedulePollRoot();
                return;
            }

            InitFor(root);
        }

        private void OnDisable()
        {
            _spinTask?.Pause();
            _spinTask = null;
            CancelInvoke();
        }

        private void SchedulePollRoot()
        {
            // schedule via a temporary helper element since UIDocument.schedule
            // isn't available without a root. Use MonoBehaviour-side coroutine
            // semantics through Invoke.
            Invoke(nameof(TryInit), 0.05f);
        }

        private void TryInit()
        {
            if (_doc == null) return;
            var root = _doc.rootVisualElement;
            if (root == null)
            {
                Invoke(nameof(TryInit), 0.05f);
                return;
            }

            InitFor(root);
        }

        private void InitFor(VisualElement root)
        {
            if (root == null) return;
            EnsureDropdownPopupStyles(root);
            EnsureInteractiveCursorTexture(root);
            EnsureToggleKnobs(root);
            EnsureSkeletonShimmers(root);
            StartSpinners(root);

            // Periodic re-scan: ScreenBase and similar consumers clone screen
            // templates lazily when the user navigates to them. The first
            // EnsureToggleKnobs/Shimmers pass only sees what's in the tree at
            // attach time — toggles cloned in later (e.g. Settings on first
            // open) would otherwise stay knob-less and render as a flat pill.
            // 250 ms is fast enough that the user never notices a missing knob
            // after a screen transition, and cheap enough to ignore (a Query
            // with an existence check on already-knobbed toggles is O(N) over
            // the small number of ds-toggle elements). Idempotent — both
            // helpers no-op if the children already exist.
            root.schedule.Execute(() =>
            {
                EnsureInteractiveCursorTexture(root);
                EnsureToggleKnobs(root);
                EnsureSkeletonShimmers(root);
            }).Every(250);
        }

        private void StartSpinners(VisualElement root)
        {
            // Rotate every element carrying `.is-spinning`, regardless of whether
            // it's a `.ds-spinner` ring, a `.ds-icon` glyph (e.g. a refresh icon
            // turning into a loading indicator on a button), or any other
            // element a screen wants to spin. The class is purely behavioral —
            // visual styling stays on whatever class the element already has.
            _spinTask = root.schedule.Execute(() =>
            {
                _spinAngle = (_spinAngle + 6f) % 360f;
                root.Query(className: SpinnerActiveClass).ForEach(el =>
                {
                    el.style.rotate = new StyleRotate(new Rotate(_spinAngle));
                });
            }).Every(16);
        }

        /// <summary>
        ///     Toggle a spinning state on any element. Adds/removes the
        ///     `is-spinning` marker class which the runtime's tick rotates.
        ///     When stopping, snaps the rotation back to 0° so the next time
        ///     the element shows it's not frozen at a random angle.
        /// </summary>
        public static void SetSpinning(VisualElement el, bool spinning)
        {
            if (el == null) return;
            if (spinning)
            {
                if (!el.ClassListContains(SpinnerActiveClass))
                    el.AddToClassList(SpinnerActiveClass);
            }
            else
            {
                el.RemoveFromClassList(SpinnerActiveClass);
                el.style.rotate = new StyleRotate(new Rotate(0f));
            }
        }

        /// <summary>
        ///     Attach dropdown-popup USS to <c>panel.visualTree</c>. Unity's
        ///     GenericDropdownMenu is a sibling of <c>rootVisualElement</c>, so
        ///     stylesheets imported via UXML never reach the open list.
        /// </summary>
        public static void EnsureDropdownPopupStyles(VisualElement root)
        {
            if (root == null) return;

            root.schedule.Execute(() =>
            {
                var panelRoot = root.parent;
                if (panelRoot == null) return;

                _dropdownPopupStylesheet ??=
                    Resources.Load<StyleSheet>(DropdownPopupStyleResource);
                if (_dropdownPopupStylesheet == null) return;
                if (panelRoot.styleSheets.Contains(_dropdownPopupStylesheet)) return;
                panelRoot.styleSheets.Add(_dropdownPopupStylesheet);
            }).StartingIn(0);
        }

        /// <summary>
        ///     Runtime UI Toolkit only supports non-default cursors via texture.
        ///     Assign one shared cursor texture for the whole document.
        /// </summary>
        public static void EnsureInteractiveCursorTexture(VisualElement root)
        {
            if (root == null) return;

            _runtimeCursorTexture ??= Resources.Load<Texture2D>(RuntimeCursorTextureResource);
            if (_runtimeCursorTexture == null)
            {
                if (!_runtimeCursorMissingWarningLogged)
                {
                    _runtimeCursorMissingWarningLogged = true;
                    Debug.LogWarning(
                        $"[DesignSystemRuntime] Could not load cursor texture at Resources/{RuntimeCursorTextureResource}.png");
                }

                return;
            }

            var cursorTexture = GetPreparedRuntimeCursorTexture(_runtimeCursorTexture);
            var cursor = new UnityEngine.UIElements.Cursor
            {
                texture = cursorTexture,
                hotspot = RuntimeCursorHotspot
            };

            var styleCursor = new StyleCursor(cursor);
            root.style.cursor = styleCursor;
        }

        private static Texture2D GetPreparedRuntimeCursorTexture(Texture2D source)
        {
            if (source == null) return null;
            if (_runtimeCursorTexturePrepared != null)
                return _runtimeCursorTexturePrepared;

            var sizeScale = Mathf.Min(
                RuntimeCursorMaxSizePx / (float)source.width,
                RuntimeCursorMaxSizePx / (float)source.height);
            var useScale = source.width > RuntimeCursorMaxSizePx || source.height > RuntimeCursorMaxSizePx;
            if (!useScale) sizeScale = 1f;

            var targetWidth = Mathf.Max(1, Mathf.RoundToInt(source.width * sizeScale));
            var targetHeight = Mathf.Max(1, Mathf.RoundToInt(source.height * sizeScale));

            var rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;

            _runtimeCursorTexturePrepared = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false)
            {
                name = $"{source.name}_runtimeCursorScaled"
            };
            _runtimeCursorTexturePrepared.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            // Cursor validation requires a readable RGBA32 texture with no mip chain.
            _runtimeCursorTexturePrepared.Apply(false, false);

            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(rt);
            return _runtimeCursorTexturePrepared;
        }

        /// <summary>
        ///     Inject `
        ///     <VisualElement class="ds-toggle__knob" />
        ///     ` into every
        ///     `.ds-toggle` whose unity-toggle__input wrapper doesn't already
        ///     have one. Idempotent. Call this from screen bootstrap right
        ///     after a template clones so the knob is present on the first
        ///     frame the toggle is visible.
        /// </summary>
        public static void EnsureToggleKnobs(VisualElement root)
        {
            if (root == null) return;
            root.Query<Toggle>(className: ToggleClass).ForEach(toggle =>
            {
                var input = toggle.Q(className: "unity-toggle__input");
                if (input == null) return;
                if (input.Q(className: ToggleKnobClass) != null) return;

                var knob = new VisualElement();
                knob.AddToClassList(ToggleKnobClass);
                knob.pickingMode = PickingMode.Ignore;
                input.Add(knob);
            });
        }

        /// <summary>
        ///     Wire a drawer's open / close state. Adds an `is-open` class to
        ///     <paramref name="wrapperOrDrawer" /> when <paramref name="opener" />
        ///     is clicked, and removes it when any of <paramref name="closers" />
        ///     (typically the close button + an optional `.ds-drawer__backdrop`)
        ///     is clicked. Idempotent — calling twice with the same elements
        ///     re-registers the handlers (cheap; UI Toolkit deduplicates by
        ///     delegate identity).
        ///     Pass the `.ds-drawer-wrap` ancestor as <paramref name="wrapperOrDrawer" />
        ///     so backdrop + drawer respond to the same class (recommended). Or
        ///     pass the drawer itself for freestanding usage — the USS rules
        ///     support both targets.
        ///     Pure-CSS authors don't need this helper at all: any code that
        ///     flips `is-open` (or `ds-drawer--open` on a self-driven drawer)
        ///     triggers the same animation.
        /// </summary>
        public static void WireDrawer(Button opener, VisualElement wrapperOrDrawer, params VisualElement[] closers)
        {
            if (opener == null || wrapperOrDrawer == null) return;

            // Closed-state pointer hygiene. `opacity: 0` does NOT disable
            // picking in UI Toolkit — an invisible backdrop still captures
            // clicks and shadows the burger button beneath it. Track which
            // closers are non-button overlays (typically the backdrop) and
            // toggle their `pickingMode` in lockstep with `is-open` so they
            // only receive clicks while actually visible.
            var nonButtonClosers = new List<VisualElement>();

            void SyncOpenState()
            {
                var open = wrapperOrDrawer.ClassListContains("is-open");
                if (opener.ClassListContains("ds-burger"))
                {
                    if (open) opener.AddToClassList("is-open");
                    else opener.RemoveFromClassList("is-open");
                }

                foreach (var c in nonButtonClosers)
                    c.pickingMode = open ? PickingMode.Position : PickingMode.Ignore;
            }

            opener.clicked += () =>
            {
                if (wrapperOrDrawer.ClassListContains("is-open"))
                    wrapperOrDrawer.RemoveFromClassList("is-open");
                else
                    wrapperOrDrawer.AddToClassList("is-open");
                SyncOpenState();
            };

            if (closers == null)
            {
                SyncOpenState();
                return;
            }

            foreach (var closer in closers)
            {
                if (closer == null) continue;
                if (closer is Button btn)
                {
                    btn.clicked += () =>
                    {
                        wrapperOrDrawer.RemoveFromClassList("is-open");
                        SyncOpenState();
                    };
                }
                else
                {
                    nonButtonClosers.Add(closer);
                    closer.RegisterCallback<PointerDownEvent>(_ =>
                    {
                        wrapperOrDrawer.RemoveFromClassList("is-open");
                        SyncOpenState();
                    });
                }
            }

            // Initial sync: in case the drawer ships with `is-open` already
            // applied (some screens want a starts-open variant), the backdrop
            // is interactive on first paint instead of one click later.
            SyncOpenState();
        }

        /// <summary>
        ///     Touch-friendly auto-hide: flash the scrollbars on for ~700 ms
        ///     whenever the user scrolls, even on devices that don't fire
        ///     `:hover`. Pure-USS auto-hide via the `:hover` rule still works
        ///     for mouse users; this helper adds the `is-scrolling` marker
        ///     the auto-hide rule also responds to.
        /// </summary>
        public static void WireScrollAutoHide(VisualElement scrollView)
        {
            if (scrollView == null) return;

            IVisualElementScheduledItem clearTask = null;

            void Flash()
            {
                if (!scrollView.ClassListContains("is-scrolling"))
                    scrollView.AddToClassList("is-scrolling");
                clearTask?.Pause();
                clearTask = scrollView.schedule.Execute(() =>
                    scrollView.RemoveFromClassList("is-scrolling")).StartingIn(700);
            }

            scrollView.RegisterCallback<WheelEvent>(_ => Flash(), TrickleDown.TrickleDown);
            scrollView.RegisterCallback<PointerDownEvent>(_ => Flash(), TrickleDown.TrickleDown);
        }

        public static void EnsureSkeletonShimmers(VisualElement root)
        {
            if (root == null) return;
            root.Query(className: SkeletonClass).ForEach(el =>
            {
                if (el.Q(className: ShimmerClass) != null) return;
                var shimmer = new VisualElement();
                shimmer.AddToClassList(ShimmerClass);
                shimmer.pickingMode = PickingMode.Ignore;
                el.Add(shimmer);

                el.schedule.Execute(() =>
                {
                    var t = Time.realtimeSinceStartup % 1.4f / 1.4f;
                    shimmer.style.translate = new StyleTranslate(
                        new Translate(new Length(t * 200f - 100f, LengthUnit.Percent), 0));
                }).Every(16);
            });
        }

        // ──────────────────────────────────────────────────────────────────
        // Auto-attach: every UIDocument in the project gets the runtime
        // without per-prefab inspector wiring. Re-scan on every scene load
        // so Activator-spawned UIDocuments are covered.
        // ──────────────────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterAutoAttach()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // Fires on every Play-mode entry, including when Project Settings →
        // Enter Play Mode Options has "Reload Scene" disabled. sceneLoaded does
        // NOT fire in that case, which is why sceneLoaded alone left game scenes
        // without a runtime helper while the showcase (AfterSceneLoad bootstrap)
        // still worked.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachOnPlayModeStart()
        {
            AttachToAllUIDocuments();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AttachToAllUIDocuments();
        }

        public static void AttachToAllUIDocuments()
        {
            var docs = FindObjectsByType<UIDocument>();
            foreach (var doc in docs)
            {
                if (doc == null) continue;
                if (doc.GetComponent<DesignSystemRuntime>() == null)
                    doc.gameObject.AddComponent<DesignSystemRuntime>();
            }
        }
    }
}