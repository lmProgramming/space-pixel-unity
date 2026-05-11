using System;
using System.Collections.Generic;
using Core.Gameplay.Combat;
using Ships;
using UnityEngine;
using UnityEngine.UIElements;
using ZLinq;
using Image = UnityEngine.UI.Image;

namespace UI.MainGame
{
    public class UIReloadVisualizer : MonoBehaviour
    {
        [SerializeField] private Ship playerShip;
        [SerializeField] private UIDocument hudDocument;
        private Dictionary<IWeapon, Action> _onNotReadyActions;
        private Dictionary<IWeapon, Action> _onReadyActions;

        private VisualElement _weaponQueue;

        private Dictionary<IWeapon, VisualElement> _weaponSlotDictionary;

        private void Awake()
        {
            ResolveHudDocument();
        }

        private void Start()
        {
            ResolveHudDocument();

            _weaponSlotDictionary = new Dictionary<IWeapon, VisualElement>();
            _onReadyActions = new Dictionary<IWeapon, Action>();
            _onNotReadyActions = new Dictionary<IWeapon, Action>();

            if (!ValidateSetup())
                return;

            _weaponQueue = hudDocument.rootVisualElement.Q<VisualElement>("weapon-queue");
            if (_weaponQueue == null)
            {
                Debug.LogError("weapon-queue VisualElement not found on HUD UIDocument.", this);
                return;
            }

            var weapons = playerShip.Weapons;
            if (weapons == null) return;

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
        }

        private void OnDestroy()
        {
            if (_weaponSlotDictionary == null) return;
            foreach (var weapon in _weaponSlotDictionary.AsValueEnumerable().Select(kvp => kvp.Key)
                         .Where(weapon => weapon != null))
            {
                if (_onReadyActions.TryGetValue(weapon, out var readyAction)) weapon.OnReady -= readyAction;
                if (_onNotReadyActions.TryGetValue(weapon, out var notReadyAction))
                    weapon.OnNotReady -= notReadyAction;
            }
        }

        private void ResolveHudDocument()
        {
            if (hudDocument != null)
                return;

            var statusPanel = FindAnyObjectByType<ShipStatusPanelController>(FindObjectsInactive.Include);
            if (statusPanel != null)
                hudDocument = statusPanel.GetComponent<UIDocument>();
        }

        private void HandleWeaponStateChange(IWeapon weapon, bool becameReady)
        {
            if (!_weaponSlotDictionary.TryGetValue(weapon, out var slot) || slot == null)
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
                var readyCount = 0;
                foreach (var child in _weaponQueue.Children())
                {
                    var weaponKvp = _weaponSlotDictionary.AsValueEnumerable()
                        .FirstOrDefault(kvp => kvp.Value == child);
                    if (weaponKvp.Key != null && weaponKvp.Key.IsReady() && child != slot)
                        readyCount++;
                }

                _weaponQueue.Insert(readyCount, slot);
            }
            else
            {
                _weaponQueue.Add(slot);
            }
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
        }

        private static VisualElement CreateWeaponSlot(IWeapon weapon)
        {
            var iconPrefab = weapon?.GetIcon();
            var sprite = TryGetSpriteFromIconPrefab(iconPrefab);

            var slot = new VisualElement();
            slot.AddToClassList("hud-weapon-slot");
            if (sprite != null)
                slot.style.backgroundImage = new StyleBackground(sprite);

            return slot;
        }

        private static Sprite TryGetSpriteFromIconPrefab(GameObject iconPrefab)
        {
            if (iconPrefab == null) return null;
            var image = iconPrefab.GetComponentInChildren<Image>(true);
            return image?.sprite;
        }

        private bool ValidateSetup()
        {
            var isValid = true;
            if (playerShip == null)
            {
                Debug.LogError("Player Ship reference not set on UIReloadVisualizer.", this);
                isValid = false;
            }

            if (hudDocument == null)
            {
                Debug.LogError("HUD UIDocument reference not set on UIReloadVisualizer.", this);
                isValid = false;
            }

            return isValid;
        }
    }
}