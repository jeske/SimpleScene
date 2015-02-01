
using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OpenTK.Input;
using SimpleScene;
using System.Collections;


namespace Example2DTileGame
{
    public class WireFrame : SSObject
    {
        /// <summary>
        /// draw a 'wire - frame' of the map
        /// </summary>
        public void drawWireFrame(Vector3 p0, Vector3 p1)
        {
            // Set points of line
            GL.Vertex3(p0);
            GL.Vertex3(p1);

        }

        /// <summary>
        /// Render drawWireFrame(p0, p1)
        /// </summary>
        public override void Render(ref SSRenderConfig renderConfig)
        {

            base.Render(ref renderConfig);

           
            Vector3 p0 = new Vector3 (0, 0, 0);
            Vector3 p1 = new Vector3 (1, 1, 1);
            GL.Begin(BeginMode.Lines);
            {
                GL.Color4(0.0f, 0.0f, 0.0f, 0.0f);
                drawWireFrame(p0, p1);
            }
            GL.End();
        }

        public WireFrame() : base()
        {

        }
    }
}
