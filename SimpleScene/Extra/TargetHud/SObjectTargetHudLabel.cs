using System;
using OpenTK;

namespace SimpleScene.Demos
{
    public class SObjectTargetHudLabel : SSObjectGDISurface_Text
    {
        public delegate string FetchTextFunc();
        public enum AnchorType { Below, Above };

        public FetchTextFunc fetchText;

        protected readonly SObjectTargetHudOutline _outline;
        protected readonly AnchorType _anchor;

        public SObjectTargetHudLabel(SObjectTargetHudOutline outline, AnchorType anchor)
        {
            _outline = outline;
            _anchor = anchor;
            this.Name = "target label " + anchor.ToString();
            renderState.alphaBlendingOn = true;
        }

        public override void Render (SSRenderConfig renderConfig)
        {
            this.Label = fetchText();
            if (this.Label == null || this.Label.Length <= 0) return;

            var screenPos = _outline.outlineScreenPos;
            if (_anchor == AnchorType.Below) {
                screenPos.X -= _outline.outlineScreenSize.Width / 2f;
                screenPos.Y += _outline.outlineScreenSize.Height;
            } else if (_anchor == AnchorType.Above) {
                screenPos.X -= _outline.outlineScreenSize.Width / 2f;
                screenPos.Y -= _outline.outlineScreenSize.Height + getGdiSize.Height;
            }
            this.Pos = new Vector3 (screenPos.X, screenPos.Y, 0f);

            base.Render(renderConfig);
        }
    }
}

