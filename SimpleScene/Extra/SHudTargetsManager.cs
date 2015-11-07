using System;
using System.Drawing; // RectangleF
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

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
            public static readonly SSVertexMesh<SSVertex_Pos> hudRectLinesMesh;
            public static readonly SSVertexMesh<SSVertex_Pos> hudTriMesh;
            public static readonly SSVertexMesh<SSVertex_Pos> hudCircleMesh;

            protected static Quaternion[] outlineOrients = {
                // 0000 - center
                Quaternion.Identity,
                // 0001 - above
                Quaternion.Identity,
                // 0010 - below
                Quaternion.FromAxisAngle(Vector3.UnitZ, +(float)Math.PI),
                // 0011 - skip
                Quaternion.Identity,
                // 0100 - to the left
                Quaternion.FromAxisAngle(Vector3.UnitZ, -(float)Math.PI/2f),
                // 0101 - to the left and above
                Quaternion.FromAxisAngle(Vector3.UnitZ, -(float)Math.PI/4f),
                // 0110 - to the left and below
                Quaternion.FromAxisAngle(Vector3.UnitZ, -(float)Math.PI*3f/4f), 
                // 0111 - skip
                Quaternion.Identity,
                // 1000 - to the right
                Quaternion.FromAxisAngle(Vector3.UnitZ, +(float)Math.PI/2f),
                // 1001 - to the right and above
                Quaternion.FromAxisAngle(Vector3.UnitZ, +(float)Math.PI/4f),
                // 1010 - to the right and below
                Quaternion.FromAxisAngle(Vector3.UnitZ, +(float)Math.PI*3f/4f),
                // 1011, 11xx - skip the rest
            };

            static TargetSpecific()
            {
                SSVertex_Pos[] rectVertices = {
                    new SSVertex_Pos (-1f, +1f, 0f),
                    new SSVertex_Pos (+1f, +1f, 0f),
                    new SSVertex_Pos (+1f, -1f, 0f),
                    new SSVertex_Pos (-1f, -1f, 0f),
                };
                hudRectLinesMesh = new SSVertexMesh<SSVertex_Pos> (rectVertices, PrimitiveType.LineLoop);

                SSVertex_Pos[] triVertices = {
                    new SSVertex_Pos (0f, -1f, 0f),
                    new SSVertex_Pos (2f, 1f, 0f),
                    new SSVertex_Pos (-2f, 1f, 0f),
                };
                hudTriMesh = new SSVertexMesh<SSVertex_Pos> (triVertices, PrimitiveType.LineLoop);

                const int numCircleVertices = 16;
                float angleSlice = (float)Math.PI * 2f / numCircleVertices;
                SSVertex_Pos[] circleVertices = new SSVertex_Pos[numCircleVertices];
                for (int i = 0; i < numCircleVertices; ++i) {
                    float angle = i * angleSlice;
                    circleVertices [i] = new SSVertex_Pos (
                        (float)Math.Cos(angle),
                        (float)Math.Sin(angle),
                        0f);
                }
                hudCircleMesh = new SSVertexMesh<SSVertex_Pos> (circleVertices, PrimitiveType.LineLoop);
            }

            public SSObject target = null;
            public float lineWidthWhenInFront = 3f;
            public float lineWidthWhenBehind = 1f;
            public float minPixelSz = 20f;

            public FetchTextFunc fetchTextBelow = null;
            public FetchTextFunc fetchTextAbove = null;

            protected readonly SSScene _targetObj3dScene;
            protected readonly SSObjectMesh _outline;
            protected readonly SSObjectGDISurface_Text _labelBelow;
            protected readonly SSObjectGDISurface_Text _labelAbove;
            protected bool _targetIsInFront;

            protected Color4 _color;

            public Color4 color {
                get { return _outline.MainColor; }
                set {
                    _outline.MainColor = value;
                    _labelBelow.MainColor = value;
                    _labelAbove.MainColor = value;
                }
            }

            public TargetSpecific(SSScene main3dScene, SSScene hud2dScene)
            {
                _targetObj3dScene = main3dScene;
                main3dScene.preRenderHooks += preRenderUpdate;

                _outline = new SSObjectMesh();
                _outline.renderState.lighted = false;
                _outline.renderState.alphaBlendingOn = true;
                _outline.renderState.frustumCulling = false;
                _outline.renderState.noShader = true;
                _outline.Name = "hud target outline";
                _outline.preRenderHook += (obj, rc) => {
                    GL.LineWidth(_targetIsInFront ? lineWidthWhenInFront : lineWidthWhenBehind);
                    GL.Disable(EnableCap.LineSmooth);
                };
                hud2dScene.AddObject(_outline);

                _labelBelow = new SSObjectGDISurface_Text();
                _labelBelow.renderState.alphaBlendingOn = true;
                _labelBelow.Name = "hud target label below";
                hud2dScene.AddObject(_labelBelow);

                _labelAbove = new SSObjectGDISurface_Text();
                _labelAbove.renderState.alphaBlendingOn = true;
                _labelAbove.Name = "hud target label above";
                hud2dScene.AddObject(_labelAbove);
            }

            ~TargetSpecific()
            {
                _outline.renderState.toBeDeleted = true;
                _labelBelow.renderState.toBeDeleted = true;
                _labelAbove.renderState.toBeDeleted = true;
            }

            public void preRenderUpdate(float timeElapsed)
            {
                bool visible = (target != null);
                _outline.renderState.visible = visible;          
                _labelBelow.renderState.visible = visible;
                _labelAbove.renderState.visible = visible;
                if (!visible) return;

                RectangleF clientRect = OpenTKHelper.GetClientRect();
                var targetRc = _targetObj3dScene.renderConfig;
                Matrix4 targetViewProj = targetRc.invCameraViewMatrix * targetRc.projectionMatrix;

                // outline
                Quaternion viewRotOnly = targetRc.invCameraViewMatrix.ExtractRotation();
                Quaternion viewRotInverted = viewRotOnly.Inverted();
                Vector3 viewRight = Vector3.Transform(Vector3.UnitX, viewRotInverted).Normalized();
                Vector3 viewUp = Vector3.Transform(Vector3.UnitY, viewRotInverted).Normalized();
                Vector2 targetScreenPos = OpenTKHelper.WorldToScreen(target.Pos, ref targetViewProj, ref clientRect);

                // assumes target is a convential SSObject without billboarding, match scale to screen, etc.
                var size = target.worldBoundingSphereRadius;
                Vector3 targetRightMost = target.Pos + viewRight * size;
                Vector3 targetTopMost = target.Pos + viewUp * size;
                Vector2 screenRightMostPt = OpenTKHelper.WorldToScreen(targetRightMost, ref targetViewProj, ref clientRect);
                Vector2 screenTopMostPt = OpenTKHelper.WorldToScreen(targetTopMost, ref targetViewProj, ref clientRect);
                float outlineHalfWidth = 2f*(screenRightMostPt.X - targetScreenPos.X);
                outlineHalfWidth = Math.Max(outlineHalfWidth, this.minPixelSz);
                float outlineHalfHeight = 2f*(targetScreenPos.Y - screenTopMostPt.Y);
                outlineHalfHeight = Math.Max(outlineHalfHeight, this.minPixelSz);

                Vector3 targetViewPos = Vector3.Transform(target.Pos, targetRc.invCameraViewMatrix);
                _targetIsInFront = targetViewPos.Z < 0f;
                float lineWidth = _targetIsInFront ? lineWidthWhenInFront : lineWidthWhenBehind;
                bool above, below, left, right;
                if (_targetIsInFront) {
                    left = targetScreenPos.X + outlineHalfWidth < 0f;
                    right = !left && targetScreenPos.X - outlineHalfWidth > clientRect.Width;
                    above = targetScreenPos.Y + outlineHalfHeight < 0f;
                    below = !above && targetScreenPos.Y + outlineHalfHeight > clientRect.Height;
                } else { // target is behind
                    float halfScrWidth = clientRect.Width / 2f;
                    float halfScrHeight = clientRect.Height / 2f;
                    left = targetScreenPos.X < halfScrWidth;
                    right = !left;
                    above = targetScreenPos.Y < halfScrHeight;
                    below = !above;
                }
                int orientIdx = (above ? 1 : 0) + (below ? 2 : 0) + (left ? 4 : 0) + (right ? 8 : 0);
                bool inTheCenter = _targetIsInFront && (orientIdx == 0);
                if (!inTheCenter) {
                    outlineHalfWidth = minPixelSz;
                    outlineHalfHeight = minPixelSz;
                    if (left) {
                        targetScreenPos.X = outlineHalfWidth;
                    } else if (right) {
                        targetScreenPos.X = clientRect.Width - outlineHalfWidth - lineWidth*2f;
                    }
                    if (above) {
                        targetScreenPos.Y = outlineHalfHeight  + _labelAbove.getGdiSize.Height; 
                    } else if (below) {
                        targetScreenPos.Y = clientRect.Height - outlineHalfHeight - _labelBelow.getGdiSize.Height;
                    }
                }


                _outline.Mesh = inTheCenter ? hudRectLinesMesh : hudTriMesh;
                _outline.Scale = new Vector3 (outlineHalfWidth, outlineHalfHeight, 1f);
                _outline.Orient(outlineOrients [orientIdx]);
                _outline.Pos = new Vector3(targetScreenPos.X, targetScreenPos.Y, 0f);

                // labels
                _labelBelow.Label = fetchTextBelow(target);
                var labelBelowPos = targetScreenPos;
                if (left) {
                    labelBelowPos.X = 0f;
                } else if (right) {
                    labelBelowPos.X = clientRect.Width - _labelBelow.getGdiSize.Width - 10f;
                } else {
                    labelBelowPos.X -= _labelBelow.getGdiSize.Width/2f;
                }
                labelBelowPos.Y += outlineHalfHeight;
                if ((left || right) && !below) {
                    labelBelowPos.Y += outlineHalfHeight;
                }
                _labelBelow.Pos = new Vector3(labelBelowPos.X, labelBelowPos.Y, 0f);

                _labelAbove.Label = fetchTextAbove(target);
                var labelAbovePos = targetScreenPos;
                if (left) {
                    labelAbovePos.X = 0f;
                } else if (right) {
                    labelAbovePos.X = clientRect.Width - _labelAbove.getGdiSize.Width - 10f;
                } else {
                    labelAbovePos.X -= _labelAbove.getGdiSize.Width/2f;
                }
                if ((left || right) && !above) {
                    labelAbovePos.Y -= outlineHalfHeight;
                }
                labelAbovePos.Y -= (outlineHalfHeight + _labelAbove.getGdiSize.Height);
                _labelAbove.Pos = new Vector3(labelAbovePos.X, labelAbovePos.Y, 0f);
            }
        }
    }

}

