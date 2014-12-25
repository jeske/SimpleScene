using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSObjectSunFlare : SSObject
    {
        private static readonly RectangleF[] c_texCoords = {
            new RectangleF (0f, 0f, 1f, 1f)
        };

        private Matrix4 m_sceneProjMatrix;
        private SSCamera m_camera;
        private SSObjectBillboard m_sun;

        private SSTexture m_texture;

        public SSObjectSunFlare (SSObjectBillboard sun,
                                 SSCamera camera, 
                                 Matrix4 sceneProjMatrix,
                                 SSTexture texture)
        {
            m_sun = sun;
            m_camera = camera;
            m_sceneProjMatrix = sceneProjMatrix;
            m_texture = texture;
        }

        void AddTexture(SSTexture texture) {
            
        }

        public override void Render (ref SSRenderConfig renderConfig)
        {
            Matrix4 viewProj = m_camera.worldMat.Inverted() * m_sceneProjMatrix;
            Vector4 sunPos = Vector4.Transform(new Vector4(m_sun.Pos, 1f), viewProj);
            sunPos /= sunPos.W;

            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            Vector2 screenOrig = new Vector2 (viewport [0], viewport [1]);
            Vector2 clientRect = new Vector2 (viewport [2], viewport [3]);
            Vector2 sunScreenPos = screenOrig + sunPos.Xy * clientRect;

            GL.Translate(sunScreenPos.X, sunScreenPos.Y, 0f);

            base.Render(ref renderConfig);
        }
    }
}

