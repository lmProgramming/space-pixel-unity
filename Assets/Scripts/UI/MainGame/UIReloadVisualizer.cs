using System;
using System.Collections.Generic;
using Core.Constants;
using Core.Gameplay.Combat;
using Core.Ships;
using UI.Common;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;
using ZLinq;

namespace UI.MainGame
{
    [RequireComponent(typeof(PanelRenderer))]
    public class UIReloadVisualizer : MonoBehaviour
    {
        private bool _isBound;
        private Dictionary<IWeapon, Action> _onNotReadyActions;
        private Dictionary<IWeapon, Action> _onReadyActions;
        private PanelRenderer _panelRenderer;
        [Inject(Id = Constants.PlayerShipId)] private IShip _playerShip;
        private int _uiVersion = -1;
        private VisualElement _weaponQueue;
        private Dictionary<IWeapon, VisualElement> _weaponSlotDictionary;

        private void Awake()
        {
            _panelRenderer = GetComponent<PanelRenderer>();
            if (_panelRenderer == null)
                throw new UnityException("[UIReloadVisualizer] PanelRenderer is required.");
        }

        private void OnEnable()
        {
            _panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            _panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            UnbindUi();
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            if (version == _uiVersion && _isBound)
                return;

            if (version != _uiVersion)
                UnbindUi();

            _uiVersion = version;
            BindUi(root);
        }

        private void BindUi(VisualElement root)
        {
            if (_isBound || root == null)
                return;

            if (!ValidateSetup())
                return;

            _weaponQueue = root.Q<ChildAnnotator>("weapon-queue");
            if (_weaponQueue == null)
            {
                Debug.LogError("weapon-queue VisualElement not found on HUD PanelRenderer.", this);
                return;
            }

            _weaponSlotDictionary = new Dictionary<IWeapon, VisualElement>();
            _onReadyActions = new Dictionary<IWeapon, Action>();
            _onNotReadyActions = new Dictionary<IWeapon, Action>();

            var weapons = _playerShip.Weapons;
            if (weapons == null)
                return;

            foreach (var weapon in weapons)
            {
                if (weapon == null) continue;

                var slot = CreateWeaponSlot(weapon);
                if (slot == null) continue;

                _weaponQueue.Add(slot);
                _weaponSlotDictionary.Add(weapon, slot);
                ApplyReadyVisuals(weapon, slot);

                Action onReadyAction = () => HandleWeaponStateChange(weapon, true);
                Action onNotReadyAction = () => HandleWeaponStateChange(weapon, false);

                _onReadyActions.Add(weapon, onReadyAction);
                _onNotReadyActions.Add(weapon, onNotReadyAction);

                weapon.OnReady += onReadyAction;
                weapon.OnNotReady += onNotReadyAction;
            }

            SortAllSlots();
            RefreshWeaponQueueChildClasses();
            _isBound = true;
        }

        private void UnbindUi()
        {
            if (!_isBound && _weaponSlotDictionary == null)
                return;

            UnsubscribeWeaponEvents();
            ClearWeaponQueueSlots();

            _weaponQueue = null;
            _weaponSlotDictionary = null;
            _onReadyActions = null;
            _onNotReadyActions = null;
            _isBound = false;
        }

        private void UnsubscribeWeaponEvents()
        {
            if (_weaponSlotDictionary == null)
                return;

            foreach (var weapon in _weaponSlotDictionary.AsValueEnumerable().Select(kvp => kvp.Key)
                         .Where(weapon => weapon != null))
            {
                if (_onReadyActions != null && _onReadyActions.TryGetValue(weapon, out var readyAction))
                    weapon.OnReady -= readyAction;
                if (_onNotReadyActions != null && _onNotReadyActions.TryGetValue(weapon, out var notReadyAction))
                    weapon.OnNotReady -= notReadyAction;
            }
        }

        private void ClearWeaponQueueSlots()
        {
            if (_weaponQueue == null || _weaponSlotDictionary == null)
                return;

            foreach (var slot in _weaponSlotDictionary.Values)
                slot?.RemoveFromHierarchy();
        }

        private void HandleWeaponStateChange(IWeapon weapon, bool becameReady)
        {
            if (!_isBound || _weaponSlotDictionary == null ||
                !_weaponSlotDictionary.TryGetValue(weapon, out var slot) || slot == null)
            {
                Debug.LogWarning($"Weapon slot not found for {weapon} during state change.", this);
                return;
            }

            ApplyReadyVisuals(weapon, slot);
            UpdateSlotIndex(slot, becameReady);
        }

        private static void ApplyReadyVisuals(IWeapon weapon, VisualElement slot)
        {
            var ready = weapon != null && weapon.IsReady();
            slot.EnableInClassList("is-ready", ready);
            slot.EnableInClassList("is-reloading", !ready);
        }

        private void UpdateSlotIndex(VisualElement slot, bool isReady)
        {
            if (_weaponQueue == null || slot.parent != _weaponQueue)
                return;

            if (isReady)
            {
                var readyCount = (from child in _weaponQueue.Children().AsValueEnumerable()
                    let weaponKvp = _weaponSlotDictionary.AsValueEnumerable()
                        .FirstOrDefault(kvp => kvp.Value == child)
                    where weaponKvp.Key != null && weaponKvp.Key.IsReady() && child != slot
                    select child).Count();

                _weaponQueue.Insert(readyCount, slot);
            }
            else
            {
                _weaponQueue.Add(slot);
            }

            RefreshWeaponQueueChildClasses();
        }

        private void SortAllSlots()
        {
            if (_weaponQueue == null || _weaponSlotDictionary == null) return;

            var orderedWeapons = _weaponSlotDictionary.Keys.AsValueEnumerable()
                .OrderByDescending(w => w.IsReady())
                .ToList();

            foreach (var w in orderedWeapons)
            {
                if (!_weaponSlotDictionary.TryGetValue(w, out var slot) || slot == null || slot.parent != _weaponQueue)
                    continue;
                _weaponQueue.Remove(slot);
            }

            foreach (var w in orderedWeapons)
            {
                if (!_weaponSlotDictionary.TryGetValue(w, out var slot) || slot == null)
                    continue;
                _weaponQueue.Add(slot);
            }

            RefreshWeaponQueueChildClasses();
        }

        private void RefreshWeaponQueueChildClasses()
        {
            if (_weaponQueue is ChildAnnotator annotator)
                annotator.ChildChanger.CheckChildChange();
        }

        private static VisualElement CreateWeaponSlot(IWeapon weapon)
        {
            var sprite = weapon?.GetSprite();

            var slot = new VisualElement();
            slot.AddToClassList("hud-weapon-slot");
            if (sprite != null)
                slot.style.backgroundImage = new StyleBackground(sprite);

            return slot;
        }

        private bool ValidateSetup()
        {
            var isValid = true;
            if (_playerShip == null)
            {
                Debug.LogError("Player Ship reference not set on UIReloadVisualizer.", this);
                isValid = false;
            }

            if (_panelRenderer == null)
            {
                Debug.LogError("PanelRenderer not set on UIReloadVisualizer.", this);
                isValid = false;
            }

            return isValid;
        }
    }
}