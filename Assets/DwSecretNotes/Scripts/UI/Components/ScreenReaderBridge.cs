using System;
using System.Linq;
using DwSecretNotes.Core;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace DwSecretNotes.UI
{
    /// <summary>Exposes the currently visible UI Toolkit controls to Android TalkBack.</summary>
    public sealed class ScreenReaderBridge
    {
        readonly VisualElement root;
        VisualElement currentScope;
        public ScreenReaderBridge(VisualElement root)
        {
            this.root = root;
#if UNITY_ANDROID && !UNITY_EDITOR
            AssistiveSupport.screenReaderStatusChanged += enabled =>
            {
                DwLog.Var("A11y", "screenReaderEnabled", enabled);
                if (enabled && currentScope?.panel != null) Refresh(currentScope);
            };
#endif
        }

        public void Refresh(VisualElement scope)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            currentScope = scope;
            if (scope == null || scope.panel == null) return;
            var hierarchy = new AccessibilityHierarchy();
            var count = 0;
            foreach (var element in scope.Query<VisualElement>().ToList())
            {
                var interactive = AppShell.CanFocus(element);
                var secretText = element is Label label && element.ClassListContains("dw-secret");
                if (!interactive && !secretText) continue;
                var title = LabelFor(element);
                if (string.IsNullOrWhiteSpace(title)) continue;
                var node = hierarchy.AddNode(title, null);
                count++;
                node.role = secretText ? AccessibilityRole.StaticText : element is TextField ? AccessibilityRole.SearchField : AccessibilityRole.Button;
                node.frame = FrameFor(element);
                node.frameGetter = () => FrameFor(element);
                if (interactive)
                    node.invoked += () =>
                    {
                        if (!AppShell.CanFocus(element)) return false;
                        element.Focus();
                        if (!(element is TextField))
                            using (var submit = NavigationSubmitEvent.GetPooled()) element.SendEvent(submit);
                        return true;
                    };
            }
            hierarchy.RefreshNodeFrames();
            AssistiveSupport.activeHierarchy = hierarchy;
            DwLog.I("A11y", $"hierarchy nodes={count}, reader={AssistiveSupport.isScreenReaderEnabled}");
#endif
        }

        static string LabelFor(VisualElement element)
        {
            if (!string.IsNullOrWhiteSpace(element.tooltip)) return element.tooltip;
            if (element is DwButton button) return button.Text.text;
            if (element is TextField field) return field.label;
            if (element is Label label) return label.text;
            return element.Query<Label>().First()?.text;
        }

        Rect FrameFor(VisualElement element)
        {
            var bounds = element.worldBound;
            var scale = Screen.width / Mathf.Max(1, root.resolvedStyle.width);
            return new Rect(bounds.x * scale, bounds.y * scale, bounds.width * scale, bounds.height * scale);
        }
    }
}
