// ChildAnnotator — MIT License, Copyright (c) 2022 Shane Celis
// https://gist.github.com/shanecelis/1ab175c46313da401138ccacceeb0c90

using UnityEngine.UIElements;

namespace UI.Common
{
    public class ChildChangeEvent : EventBase<ChildChangeEvent>, IChangeEvent
    {
        public ChildChangeEvent()
        {
            LocalInit();
        }

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        private void LocalInit()
        {
            bubbles = false;
            tricklesDown = false;
        }

        public static ChildChangeEvent GetPooled(int previousValue, int newValue)
        {
            var pooled = GetPooled();
            return pooled;
        }
    }

    public class ChildChangeTracker
    {
        private int _checkInterval;
        private int _lastChildCount;
        private IVisualElementScheduledItem _pollTask;
        private VisualElement _target;

        public VisualElement Target
        {
            set
            {
                _target = value;
                RestartPoll();
            }
        }

        public int CheckInterval
        {
            get => _checkInterval;
            set
            {
                if (_checkInterval == value)
                    return;
                _checkInterval = value;
                RestartPoll();
            }
        }

        public void CheckChildChange()
        {
            if (_target?.childCount != _lastChildCount)
                SendChildChange();
        }

        private void SendChildChange()
        {
            if (_target == null)
                return;
            var changeEvent = ChildChangeEvent.GetPooled(_lastChildCount, _target.childCount);
            changeEvent.target = _target;
            _lastChildCount = _target.childCount;
            _target.SendEvent(changeEvent);
        }

        private void RestartPoll()
        {
            _pollTask?.Pause();
            _pollTask = null;
            if (_target == null || _checkInterval <= 0)
                return;
            _pollTask = _target.schedule.Execute(CheckChildChange).Every(_checkInterval);
        }
    }

    /// <summary>
    ///     Applies <c>.first-child</c> / <c>.last-child</c> USS classes to direct
    ///     children (Unity does not support those pseudo-classes). Set
    ///     <c>check-interval</c> when children are added, removed, or reordered at
    ///     runtime.
    /// </summary>
    [UxmlElement]
    public partial class ChildAnnotator : VisualElement
    {
        public readonly ChildChangeTracker ChildChanger = new();
        private VisualElement _firstChild;
        private VisualElement _lastChild;

        // ReSharper disable once MemberCanBePrivate.Global
        public ChildAnnotator()
        {
            RegisterCallback<ChildChangeEvent>(OnChildChange);
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                ChildChanger.Target = this;
                ChildChanger.CheckChildChange();
            });
            schedule.Execute(() => ChildChanger.CheckChildChange());
        }

        [UxmlAttribute("check-interval")]
        public int CheckInterval
        {
            get => ChildChanger.CheckInterval;
            set => ChildChanger.CheckInterval = value;
        }

        private VisualElement FirstChild
        {
            set
            {
                if (_firstChild == value)
                    return;
                _firstChild?.RemoveFromClassList("first-child");
                _firstChild = value;
                _firstChild?.AddToClassList("first-child");
            }
        }

        private VisualElement LastChild
        {
            set
            {
                if (_lastChild == value)
                    return;
                _lastChild?.RemoveFromClassList("last-child");
                _lastChild = value;
                _lastChild?.AddToClassList("last-child");
            }
        }

        private void OnChildChange(ChildChangeEvent evt)
        {
            if (childCount == 0)
            {
                FirstChild = null;
                LastChild = null;
                return;
            }

            FirstChild = this[0];
            LastChild = this[childCount - 1];
        }
    }
}