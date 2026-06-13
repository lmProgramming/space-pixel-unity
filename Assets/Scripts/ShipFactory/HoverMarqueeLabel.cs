using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShipFactory
{
    internal sealed class HoverMarqueeLabel
    {
        private const float PauseSeconds = 0.5f;
        private const float OverflowEpsilon = 3f;
        private const float EndScrollPadding = 4f;

        private readonly VisualElement _clip;
        private readonly VisualElement _hoverTarget;
        private readonly Label _label;
        private float _maxOffset;
        private IVisualElementScheduledItem _measureJob;
        private MarqueePhase _phase;
        private float _phaseStartTime;
        private float _scrollDuration;

        private IVisualElementScheduledItem _scrollJob;

        public HoverMarqueeLabel(VisualElement hoverTarget, VisualElement clip, Label label)
        {
            _hoverTarget = hoverTarget;
            _clip = clip;
            _label = label;
            _clip.style.overflow = Overflow.Hidden;
            _label.style.whiteSpace = WhiteSpace.NoWrap;
            _label.style.unityTextAlign = TextAnchor.MiddleLeft;

            _hoverTarget.RegisterCallback<PointerEnterEvent>(_ => Start());
            _hoverTarget.RegisterCallback<PointerLeaveEvent>(_ => Stop());
        }

        private void Start()
        {
            Stop();
            _measureJob = _hoverTarget.schedule.Execute(TryBeginScroll).StartingIn(0);
        }

        private void TryBeginScroll()
        {
            _measureJob = null;
            var clipWidth = _clip.contentRect.width;
            if (clipWidth <= 0)
            {
                _measureJob = _hoverTarget.schedule.Execute(TryBeginScroll);
                return;
            }

            var textWidth = _label.MeasureTextSize(_label.text, 0, VisualElement.MeasureMode.Undefined, 0,
                VisualElement.MeasureMode.Undefined).x;
            if (textWidth < clipWidth - OverflowEpsilon)
                return;

            _maxOffset = Mathf.Max(1f, textWidth - clipWidth) + EndScrollPadding;
            _scrollDuration = Mathf.Clamp(_maxOffset * 0.025f, 0.8f, 3f);
            _phase = MarqueePhase.ScrollForward;
            _phaseStartTime = Time.realtimeSinceStartup;
            _label.style.translate = new StyleTranslate(new Translate(0, 0, 0));

            _scrollJob = _label.schedule.Execute(Tick).Every(16);
        }

        private void Tick()
        {
            var elapsed = Time.realtimeSinceStartup - _phaseStartTime;

            switch (_phase)
            {
                case MarqueePhase.PauseAtStart:
                    SetOffset(0f);
                    if (elapsed >= PauseSeconds)
                        BeginPhase(MarqueePhase.ScrollForward);
                    break;

                case MarqueePhase.ScrollForward:
                    var forwardT = Mathf.Clamp01(elapsed / _scrollDuration);
                    SetOffset(_maxOffset * forwardT);
                    if (forwardT >= 1f)
                        BeginPhase(MarqueePhase.PauseAtEnd);
                    break;

                case MarqueePhase.PauseAtEnd:
                    SetOffset(_maxOffset);
                    if (elapsed >= PauseSeconds)
                        BeginPhase(MarqueePhase.ScrollBack);
                    break;

                case MarqueePhase.ScrollBack:
                    var backT = Mathf.Clamp01(elapsed / _scrollDuration);
                    SetOffset(_maxOffset * (1f - backT));
                    if (backT >= 1f)
                        BeginPhase(MarqueePhase.PauseAtStart);
                    break;
                case MarqueePhase.Idle:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void BeginPhase(MarqueePhase phase)
        {
            _phase = phase;
            _phaseStartTime = Time.realtimeSinceStartup;
        }

        private void SetOffset(float offset)
        {
            _label.style.translate =
                new StyleTranslate(new Translate(new Length(-offset, LengthUnit.Pixel), 0, 0));
        }

        private void Stop()
        {
            _measureJob?.Pause();
            _measureJob = null;
            _scrollJob?.Pause();
            _scrollJob = null;
            _phase = MarqueePhase.Idle;
            _label.style.translate = new StyleTranslate(new Translate(0, 0, 0));
            _label.style.unityTextAlign = TextAnchor.MiddleLeft;
        }

        private enum MarqueePhase
        {
            Idle,
            PauseAtStart,
            ScrollForward,
            PauseAtEnd,
            ScrollBack
        }
    }
}