using System;
using System.Drawing; // RectangleF
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene.Demos
{
    /// <summary>
    /// Draws a HUD element to help locate a target object in virtual space
    /// </summary>
    public class SObjectTargetHudOutline : SSObjectMesh
    {
        protected static readonly SSVertexMesh<SSVertex_Pos> _hudRectLinesMesh;

        public SSObject targetObj;
        public SSObject distFromObj = null;
        public float lineWidthWhenInFront = 5f;
        public float lineWidthWhenBehind = 1f;
        public float minPixelSz = 20f;

        public Vector2 outlineScreenPos { get { return _targetScreenPos; } }
        public SizeF outlineScreenSize { get { return _outlineSize; } }

        protected readonly SSScene _targetObj3dScene;
        protected Vector2 _targetScreenPos;
        protected SizeF _outlineSize; 

        static SObjectTargetHudOutline()
        {
            SSVertex_Pos[] vertices = {
                new SSVertex_Pos (-1f, +1f, 0f),
                new SSVertex_Pos (+1f, +1f, 0f),
                new SSVertex_Pos (+1f, -1f, 0f),
                new SSVertex_Pos (-1f, -1f, 0f),
            };
            _hudRectLinesMesh = new SSVertexMesh<SSVertex_Pos> (vertices, PrimitiveType.LineLoop);
        }

        public SObjectTargetHudOutline (SSScene targetObj3dScene,
                                        SSObject measureDistFrom = null,
                                        SSObject target = null)
            : base (_hudRectLinesMesh)
        {
            distFromObj = measureDistFrom;
            targetObj = target;
            _targetObj3dScene = targetObj3dScene;

            renderState.lighted = false;
            renderState.alphaBlendingOn = true;
            renderState.frustumCulling = false;
            renderState.noShader = true;
        }

        public override void Render (SSRenderConfig renderConfig)
        {
            if (targetObj == null) return;

            RectangleF clientRect = OpenTKHelper.GetClientRect();
            var targetRc = _targetObj3dScene.renderConfig;
            Matrix4 targetViewProj = targetRc.invCameraViewMatrix * targetRc.projectionMatrix;

            // find target screen position amd dimmensions
            Quaternion viewRotOnly = targetRc.invCameraViewMatrix.ExtractRotation();
            Quaternion viewRotInverted = viewRotOnly.Inverted();
            Vector3 viewRight = Vector3.Transform(Vector3.UnitX, viewRotInverted).Normalized();
            Vector3 viewUp = Vector3.Transform(Vector3.UnitY, viewRotInverted).Normalized();
            _targetScreenPos = OpenTKHelper.WorldToScreen(targetObj.Pos, ref targetViewProj, ref clientRect);

            // assumes target is a convential SSObject without billboarding, match scale to screen, etc.
            var size = targetObj.worldBoundingSphereRadius;
            Vector3 targetRightMost = targetObj.Pos + viewRight * size;
            Vector3 targetTopMost = targetObj.Pos + viewUp * size;
            Vector2 screenRightMostPt = OpenTKHelper.WorldToScreen(targetRightMost, ref targetViewProj, ref clientRect);
            Vector2 screenTopMostPt = OpenTKHelper.WorldToScreen(targetTopMost, ref targetViewProj, ref clientRect);
            _outlineSize.Width = 2f*(screenRightMostPt.X - outlineScreenPos.X);
            _outlineSize.Width = Math.Max(_outlineSize.Width, this.minPixelSz);
            _outlineSize.Height = 2f*(outlineScreenPos.Y - screenTopMostPt.Y);
            _outlineSize.Height = Math.Max(_outlineSize.Height, this.minPixelSz);

            this.Pos = new Vector3(outlineScreenPos.X, outlineScreenPos.Y, 0f);
            this.Scale = new Vector3 (_outlineSize.Width, _outlineSize.Height, 1f);

            Vector3 targetViewPos = Vector3.Transform(targetObj.Pos, targetRc.invCameraViewMatrix);
            bool inFrontOfCamera = targetViewPos.Z < 0f;

            GL.LineWidth(inFrontOfCamera ? this.lineWidthWhenInFront : this.lineWidthWhenBehind);
            GL.Disable(EnableCap.PointSmooth);
            // TODO draw differently when the target is behind

            base.Render(renderConfig); // SSObjectMesh.Render() will do the rest
        }
    }
}

