---
name: ui-overlays-and-templates
description: Build Unity UI Toolkit overlays with shared UXML templates, reliable full-screen layering, and Design System styling. Use when implementing or refactoring menus, pause/settings dialogs, slider/input layouts, or template instance wiring.
disable-model-invocation: true
---

# UI Overlays And Templates

## Use This Skill When
- Working on Unity UI Toolkit menus, pause screens, and modal settings overlays.
- Extracting duplicated UI into shared UXML templates.
- Fixing overlays that render in the wrong area, block clicks, or appear clipped.
- Styling slider + numeric input combinations with Design System classes.

## Core Patterns

### 1) Shared template extraction
- Put shared overlay markup in `Assets/Scripts/UI/Common/*.uxml`.
- In consuming UXMLs, declare:
  - `<ui:Template name="..." src="project://database/..."/>`
  - `<ui:Instance template="..." style="position:absolute; left:0; right:0; top:0; bottom:0;"/>`
- Keep common element names stable (`settings-overlay`, `pause-overlay`, button names, slider names).

### 2) Overlay host pattern (prevents click blocking)
- Wrap each overlay instance in a host:
  - `name="...-overlay-host"`
  - `position:absolute; left:0; right:0; top:0; bottom:0; display:none;`
- In controller:
  - Show: host `display:flex`, overlay `display:flex`
  - Hide: overlay `display:none`, host `display:none`
- Reason: hidden overlay content should not leave an always-on transparent container that eats input.

### 3) Absolute fill on all layers
- Overlay root should be absolute fill (`left/right/top/bottom = 0`), not only `width/height:100%`.
- `ui:Instance` generated `TemplateContainer` must also be absolute fill or overlays may appear in only part of screen.

### 4) Assign assets, avoid runtime bootstrap/loading
- Prefer inspector-assigned `UIDocument.sourceAsset`.
- Avoid bootstrapping UI objects and avoid `Resources.Load` for main flow UI unless explicitly requested.

### 5) Slider input fields
- Put `show-input-field="true|false"` in UXML (declarative source of truth).
- If enabled, style generated `TextField` from controller:
  - add `ds-input`
  - fixed width
  - `alignSelf = Center`
  - left margin
- If DS colors are missing, first verify class assignment and theme USS import.

## Layout Spacing Rule
- Use `16px` spacing between important pause/menu elements (title -> action rows).
- Use `8px` spacing between related elements (slider -> input field, button -> tooltip, action rows between each other).
- Keep consistent margin rhythm across reused templates.

## Quick Checklist
- [ ] Shared UXML extracted for repeated overlay.
- [ ] Host + overlay dual-toggle implemented.
- [ ] Host, instance, and overlay all absolute-fill.
- [ ] Hidden hosts are truly `display:none`.
- [ ] Spacing follows 16px rhythm.
- [ ] Sliders and optional number fields look intentional in DS.
