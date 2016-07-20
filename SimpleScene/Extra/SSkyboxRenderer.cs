using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene.Demos
{
    public class SSkyboxRenderer : SSObject
    {
        public readonly string[] defaultTextures = {
            "skybox/purple_nebula/purple_nebula_right1.png",
            "skybox/purple_nebula/purple_nebula_left2.png",
            "skybox/purple_nebula/purple_nebula_top3.png",
            "skybox/purple_nebula/purple_nebula_bottom4.png",
            "skybox/purple_nebula/purple_nebula_front5.png",
            "skybox/purple_nebula/purple_nebula_back6.png",
        };

        public enum Face {
            Right = 0,
            Left = 1,
            Top = 2,
            Bottom = 3,
            Front = 4,
            Back = 5,
            NumFaces = 6
        };

        public const float piOvr2 = (float)Math.PI / 2f;
        public static readonly Matrix4 flipMat = Matrix4.CreateRotationY((float)Math.PI);

        public static Matrix4[] orientations = {
            flipMat * Matrix4.CreateRotationY(-piOvr2), // right
            flipMat * Matrix4.CreateRotationY(+piOvr2), // left 
            flipMat * Matrix4.CreateRotationX(-piOvr2), // top
            flipMat * Matrix4.CreateRotationX(+piOvr2), // bottom
            flipMat * Matrix4.Identity,                 // front (+Z)
            Matrix4.Identity                            // back (-Z)
        };

        public SSTexture[] textures = null;

        public SSkyboxRenderer(SSTexture[] textures = null)
        {
            this.renderState.depthTest = true;
            this.renderState.lighted = false;
            this.renderState.frustumCulling = false;
            this.renderState.noShader = true;

            this.textures = new SSTexture[(int)Face.NumFaces];
            for (int i = 0; i < (int)Face.NumFaces; ++i) {
                SSTexture tex = (textures != null) ? textures [i] : null;
                tex = tex ?? SSAssetManager.GetInstance<SSTexture>(defaultTextures [i]);
                this.textures [i] = tex;
            }
        }

        public override void Render (SSRenderConfig renderConfig)
        {
            if (textures == null) return;

            base.Render(renderConfig);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.Enable(EnableCap.Texture2D);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 
                (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 
                (int)TextureWrapMode.Clamp);

            Matrix4 scaleAndPosMat = Matrix4.CreateScale(this.Scale)
                * Matrix4.CreateTranslation(0f, 0f, -this.Scale.X/2f);

            for (int i = 0; i < (int)Face.NumFaces; ++i) {
            //for (int i = 0; i < 1; ++i) {
                var tex = textures [i];
                if (tex != null) {
                    var mvMat = scaleAndPosMat * orientations [i] * renderConfig.invCameraViewMatrix;
                    GL.LoadMatrix(ref mvMat);

                    GL.BindTexture(TextureTarget.Texture2D, textures [i].TextureID);

                    SSTexturedQuad.singleFaceInstance.drawSingle(renderConfig, PrimitiveType.Triangles);
                }
            }
        }
    }
}

