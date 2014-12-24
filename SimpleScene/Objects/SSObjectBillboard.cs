using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSObjectBillboard : SSObjectMesh
    {
        public SSObjectBillboard ()
        {
        }

        public SSObjectBillboard(SSAbstractMesh mesh)
            : base()
        {
            Mesh = mesh;
        }

        public override void Render(ref SSRenderConfig renderConfig)
        {
            if (Mesh != null) {
                base.Render(ref renderConfig);

                // override matrix setup to get rid of any rotation in view
                // http://stackoverflow.com/questions/5467007/inverting-rotation-in-3d-to-make-an-object-always-face-the-camera/5487981#5487981
                Matrix4 modelViewMat = this.worldMat * renderConfig.invCameraViewMat;
                Vector3 trans = modelViewMat.ExtractTranslation();
                Vector3 scale = modelViewMat.ExtractScale();
                modelViewMat = new Matrix4 (
                    scale.X, 0f, 0f, trans.X,
                    0f, scale.Y, 0f, trans.Y,
                    0f, 0f, scale.Z, trans.Z,
                    0f, 0f, 0f, 1f);
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadMatrix(ref modelViewMat);

                // TODO occulsion queuery

                Mesh.RenderMesh(ref renderConfig);

                // TODO occulsion queuery
            }
        }
    }
}

