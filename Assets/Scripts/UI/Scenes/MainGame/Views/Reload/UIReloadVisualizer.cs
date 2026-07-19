using System;
using System.Collections.Generic;
using Core.Services;
using Core.Ships.Module;
using UI.Common;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;
using ZLinq;

namespace UI.Scenes.MainGame.Views.Reload
{
    public class UIReloadVisualizer : PanelRendererBase
    {
        [Inject] private IActivePlayerShipProvider _activePlayerShipProvider;
        private Dictionary<IWeapon, Action> _onNotReadyActions;
        private Dictionary<IWeapon, Action> _onReadyActions;
        private VisualElement _weaponQueue;
        private Dictionary<IWeapon, VisualElement> _weaponSlotDictionary;

        protected override void BindUiCore(
            VisualElement root)
        {
            if (!_activePlayerShipProvider.HasPlayerShip)
                return;

            ValidateSetup();

            _weaponQueue = root.Q<ChildAnnotator>("weapon-queue");
            if (_weaponQueue == null)
                throw new InvalidOperationException(
                    "[UIReloadVisualizer] weapon-queue VisualElement is missing in UXML.");

            _weaponSlotDictionary = new Dictionary<IWeapon, VisualElement>();
            _onReadyActions = new Dictionary<IWeapon, Action>();
            _onNotReadyActions = new Dictionary<IWeapon, Action>();

            var playerShip = _activePlayerShipProvider.ActiveShip;
            var weapons = playerShip.Weapons;
            if (weapons == null)
                throw new InvalidOperationException("[UIReloadVisualizer] Player ship weapons are required.");

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
        }

        protected override void UnbindUiCore()
        {
            UnsubscribeWeaponEvents();
            ClearWeaponQueueSlots();
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
            if (!IsUiBound || _weaponSlotDictionary == null ||
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

        private void ValidateSetup()
        {
            if (PanelRenderer == null)
                throw new InvalidOperationException("[UIReloadVisualizer] PanelRenderer is required.");
        }
    }
}