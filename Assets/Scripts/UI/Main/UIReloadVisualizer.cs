using System;
using System.Collections.Generic;
using System.Linq;
using Ship.Modules;
using UnityEngine;

namespace UI.Main
{
    public class UIReloadManager : MonoBehaviour
    {
        [SerializeField] private GameObject smallCannonPrefab;
        [SerializeField] private GameObject bigCannonPrefab;
        [SerializeField] private Ship.Ship playerShip;

        [SerializeField] private Transform iconContainer;

        private Dictionary<IWeapon, Action> _onNotReadyActions;
        private Dictionary<IWeapon, Action> _onReadyActions;

        private Dictionary<IWeapon, GameObject> _weaponIconDictionary;

        private void Start()
        {
            _weaponIconDictionary = new Dictionary<IWeapon, GameObject>();
            _onReadyActions = new Dictionary<IWeapon, Action>();
            _onNotReadyActions = new Dictionary<IWeapon, Action>();

            if (!ValidateSetup()) return;

            var weapons = playerShip.Weapons;
            if (weapons == null) return;

            foreach (var weapon in weapons)
            {
                if (weapon == null) continue;

                var icon = CreateIcon(weapon);
                if (icon == null) continue;

                _weaponIconDictionary.Add(weapon, icon);

                Action onReadyAction = () => HandleWeaponStateChange(weapon, true);
                Action onNotReadyAction = () => HandleWeaponStateChange(weapon, false);

                _onReadyActions.Add(weapon, onReadyAction);
                _onNotReadyActions.Add(weapon, onNotReadyAction);

                weapon.OnReady += onReadyAction;
                weapon.OnNotReady += onNotReadyAction;
            }

            SortAllIcons();
        }

        private void OnDestroy()
        {
            if (_weaponIconDictionary == null) return;
            foreach (var kvp in _weaponIconDictionary)
            {
                var weapon = kvp.Key;
                if (weapon == null) continue;
                if (_onReadyActions.TryGetValue(weapon, out var readyAction)) weapon.OnReady -= readyAction;
                if (_onNotReadyActions.TryGetValue(weapon, out var notReadyAction))
                    weapon.OnNotReady -= notReadyAction;
            }
        }

        private void HandleWeaponStateChange(IWeapon weapon, bool becameReady)
        {
            if (_weaponIconDictionary.TryGetValue(weapon, out var icon) && icon != null)
                UpdateIconPosition(icon, becameReady);
            else
                Debug.LogWarning($"Icon not found for weapon {weapon} during state change.", this);
        }

        private void UpdateIconPosition(GameObject icon, bool isReady)
        {
            if (icon == null || iconContainer == null || icon.transform.parent != iconContainer)
                return;

            if (isReady)
            {
                var readyCount = 0;
                foreach (Transform child in iconContainer)
                {
                    var weaponKvp = _weaponIconDictionary.FirstOrDefault(kvp => kvp.Value == child.gameObject);
                    if (weaponKvp.Key != null && weaponKvp.Key.IsReady() &&
                        child.gameObject != icon)
                        readyCount++;
                }

                icon.transform.SetSiblingIndex(readyCount);
            }
            else
            {
                icon.transform.SetAsLastSibling();
            }
        }

        private void SortAllIcons()
        {
            if (iconContainer == null || _weaponIconDictionary == null) return;

            var orderedWeapons = _weaponIconDictionary.Keys
                .OrderByDescending(w => w.IsReady())
                .ToList();

            for (var i = 0; i < orderedWeapons.Count; i++)
                if (_weaponIconDictionary.TryGetValue(orderedWeapons[i], out var icon) && icon != null)
                    icon.transform.SetSiblingIndex(i);
        }


        private GameObject CreateIcon(IWeapon weapon)
        {
            if (weapon == null || iconContainer == null) return null;

            var chosenPrefab = weapon switch
            {
                Cannon => smallCannonPrefab,
                _ => bigCannonPrefab
            };

            return chosenPrefab == null ? null : Instantiate(chosenPrefab, iconContainer);
        }

        private bool ValidateSetup()
        {
            var isValid = true;
            if (playerShip == null)
            {
                Debug.LogError("Player Ship reference not set!", this);
                isValid = false;
            }

            if (iconContainer == null)
            {
                Debug.LogError("Icon Holder Transform not set!", this);
                isValid = false;
            }

            if (smallCannonPrefab != null && bigCannonPrefab != null) return isValid;

            Debug.LogError("Icon prefabs not set!", this);
            return false;
        }
    }
}