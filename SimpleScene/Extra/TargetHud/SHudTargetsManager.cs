using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene.Demos
{
    public class SHudTargetsManager
    {
        public delegate string FetchTextFunc(SSObject target);
        protected static FetchTextFunc _defaultFetchText = (obj) => "";

        protected readonly SSScene _main3dScene;
        protected readonly SSScene _hud2dScene;
        protected List<TargetSpecific> _targets = new List<TargetSpecific>();

        public SHudTargetsManager (SSScene main3dScene, SSScene hud2dScene)
        {
            _main3dScene = main3dScene;
            _hud2dScene = hud2dScene;
        }

        public TargetSpecific addTarget(
            Color4 color,
            FetchTextFunc fetchTextBelow = null,
            FetchTextFunc fetchTextAbove = null,
            SSObject target = null)
        {
            var newTarget = new TargetSpecific (_main3dScene, _hud2dScene);
            newTarget.target = target;
            newTarget.color = color;
            newTarget.fetchTextBelow = fetchTextBelow ?? _defaultFetchText;
            newTarget.fetchTextAbove = fetchTextAbove ?? _defaultFetchText;

            return newTarget;
        }

        public void clear()
        {
            _targets.Clear();
        }

        public void removeTargets(SSObject target)
        {
            _targets.RemoveAll((t) => (t.target == target));
        }
        
        public class TargetSpecific
        {
            public readonly SObjectTargetHudOutline targetOutline;
            public readonly SObjectTargetHudLabel labelBelow;
            public readonly SObjectTargetHudLabel labelAbove;

            public SSObject measureDistFrom = null;
            public FetchTextFunc fetchTextBelow;
            public FetchTextFunc fetchTextAbove;

            protected Color4 _color;

            public Color4 color {
                get { return targetOutline.MainColor; }
                set {
                    targetOutline.MainColor = value;
                    labelBelow.MainColor = value;
                    labelAbove.MainColor = value;
                }
            }

            public SSObject target {
                get { return targetOutline.targetObj; }
                set { 
                    targetOutline.targetObj = value; 
                    bool targetOk = (value != null);
                    targetOutline.renderState.visible = targetOk;          
                    labelBelow.renderState.visible = targetOk;
                    labelAbove.renderState.visible = targetOk;
                }
            }

            public TargetSpecific(SSScene main3dScene, SSScene hud2dScene)
            {
                targetOutline = new SObjectTargetHudOutline(main3dScene, null, null);
                hud2dScene.AddObject(targetOutline);

                labelBelow = new SObjectTargetHudLabel(targetOutline, SObjectTargetHudLabel.AnchorType.Below);
                labelBelow.fetchText = () => { return this.fetchTextBelow(target); };
                hud2dScene.AddObject(labelBelow);

                labelAbove = new SObjectTargetHudLabel(targetOutline, SObjectTargetHudLabel.AnchorType.Above);
                labelAbove.fetchText = () => { return this.fetchTextAbove(target); };
                hud2dScene.AddObject(labelAbove);

                target = null;
            }

            ~TargetSpecific()
            {
                targetOutline.renderState.toBeDeleted = true;
                targetOutline.renderState.toBeDeleted = true;
            }


        }
    }

}

