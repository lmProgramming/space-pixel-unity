using System;
using Core.Pixelation;
using Core.ShipFactory;
using UI.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShipFactory.UI.Components
{
    [UxmlElement]
    public partial class ModulePaletteCard : VisualElement
    {
        private const string TemplateResourcePath = "UI/ModulePaletteCard";
        private const string DraggingCardClass = "palette-card--dragging";
        private const string DisabledCardClass = "palette-card--disabled";

        public static ModulePaletteCard Create()
        {
            var asset = Resources.Load<VisualTreeAsset>(TemplateResourcePath);
            if (!asset)
                throw new InvalidOperationException(
                    $"[ModulePaletteCard] VisualTreeAsset '{TemplateResourcePath}' was not found in Resources.");

            return asset.Instantiate().Q<ModulePaletteCard>()
                   ?? throw new InvalidOperationException(
                       "[ModulePaletteCard] ModulePaletteCard root is missing in ModulePaletteCard.uxml.");
        }

        public void Bind(
            ShipModuleSO moduleSO,
            bool isPlacementBlocked,
            Func<bool> isInputLocked,
            Func<bool> isAnyCardDragging,
            Action<ModulePaletteCard, ShipModuleSO, Vector2> onDragStarted,
            Action<ModulePaletteCard> onDragFinished,
            Action<ShipModuleSO> onHoverStarted,
            Action<ShipModuleSO> onHoverEnded,
            Action onBlockedPlacementClicked)
        {
            if (moduleSO == null)
                throw new ArgumentNullException(nameof(moduleSO));

            if (isPlacementBlocked)
                AddToClassList(DisabledCardClass);

            var spriteImage = this.Q<Image>("module-card-sprite")
                              ?? throw new InvalidOperationException(
                                  "[ModulePaletteCard] 'module-card-sprite' is missing in ModulePaletteCard.uxml.");
            var titleClip = this.Q<VisualElement>("module-card-title-clip")
                            ?? throw new InvalidOperationException(
                                "[ModulePaletteCard] 'module-card-title-clip' is missing in ModulePaletteCard.uxml.");
            var title = this.Q<Label>("module-card-title")
                        ?? throw new InvalidOperationException(
                            "[ModulePaletteCard] 'module-card-title' is missing in ModulePaletteCard.uxml.");
            var dimensions = this.Q<Label>("module-card-dimensions")
                             ?? throw new InvalidOperationException(
                                 "[ModulePaletteCard] 'module-card-dimensions' is missing in ModulePaletteCard.uxml.");

            var pixelatedRigidbody = moduleSO.Prefab.GetComponent<IPixelatedSprite>();
            var sprite = pixelatedRigidbody?.GetSprite();
            if (sprite)
            {
                spriteImage.sprite = sprite;
                spriteImage.scaleMode = ScaleMode.ScaleToFit;
            }
            else
            {
                spriteImage.visible = false;
            }

            title.text = moduleSO.Name;
            dimensions.text = $"{moduleSO.Dimensions.x}x{moduleSO.Dimensions.y}";

            _ = new HoverMarqueeLabel(this, titleClip, title);

            RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (isInputLocked() || isAnyCardDragging())
                    return;
                onHoverStarted?.Invoke(moduleSO);
            });

            RegisterCallback<PointerLeaveEvent>(_ => onHoverEnded?.Invoke(moduleSO));

            RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || isInputLocked())
                    return;

                if (isPlacementBlocked)
                {
                    onBlockedPlacementClicked?.Invoke();
                    evt.StopPropagation();
                    return;
                }

                AddToClassList(DraggingCardClass);
                onDragStarted?.Invoke(this, moduleSO, evt.position);
                evt.StopPropagation();
            });

            RegisterCallback<PointerUpEvent>(_ => onDragFinished?.Invoke(this));
        }

        public void ClearDraggingVisual()
        {
            RemoveFromClassList(DraggingCardClass);
        }
    }
}