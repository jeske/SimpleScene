
using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OpenTK.Input;
using SimpleScene;
using System.Collections;


namespace Example2DTileGame
{
    public class SSLineObject : SSObject
    {

        Vector3 mapPosition;
        float squareWidth = 4;
        Vector3[,] mapArray = new Vector3[10, 10]; // W x D map (X & Z)
        bool isGenerating = true;
        int x = 0;

        ArrayList vectorList = new ArrayList();
        
        // Default values of square - should never actually set anything to anything
        Vector3 p0 = new Vector3(0, 0, 0);
        Vector3 p1 = new Vector3(0, 0, 0);
        Vector3 p2 = new Vector3(0, 0, 0);
        Vector3 p3 = new Vector3(0, 0, 0);
        Vector3 middle = new Vector3(0, 0, 0);

        /// <summary>
        /// draw a 'wire - frame' of the map
        /// </summary>
        public void drawWireFrame(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 middle)
        {
            // Set points of line
            GL.Vertex3(p0); // alternated to different point every time it is run
            GL.Vertex3(p1);

            GL.Vertex3(p0);
            GL.Vertex3(p2);

            GL.Vertex3(p2);
            GL.Vertex3(p3);

            GL.Vertex3(p3);
            GL.Vertex3(p1);

            ///////////////Middle
            GL.Vertex3(p0);
            GL.Vertex3(middle);

            GL.Vertex3(p1);
            GL.Vertex3(middle);

            GL.Vertex3(p2);
            GL.Vertex3(middle);

            GL.Vertex3(p3);
            GL.Vertex3(middle);
        }

        /// <summary>
        /// Render line object
        /// </summary>
        public override void Render(ref SSRenderConfig renderConfig)
        {
                
                base.Render(ref renderConfig);

                //!important!
                // mode setup
                SSShaderProgram.DeactivateAll(); // disable GLSL
                GL.Disable(EnableCap.Texture2D);
                
                GL.Disable(EnableCap.Lighting);
                //!important!

                
                GL.Begin(PrimitiveType.Lines);
                GL.Color3(1f, 0f, 0.8f);

                Console.WriteLine("Set points");
                GL.Begin(PrimitiveType.Lines);

                for (int i = 0; i < mapArray.GetLength(0); i++)
                {
                    for (int j = 0; j < mapArray.GetLength(1); j++)
                    {
                        float Middle = squareWidth / 2; // Middle point of the square
                        float squareCX = i * squareWidth;
                        float squareCY = j * squareWidth;

                        p0 = new Vector3(squareCX, 0, squareCY);
                        p1 = new Vector3(squareCX + squareWidth, 0, squareCY);
                        p2 = new Vector3(squareCX, 0, squareCY + squareWidth);
                        p3 = new Vector3(squareCX + squareWidth, 0, squareCY + squareWidth);

                        // Determines height
                        middle = new Vector3(squareCX + Middle, i, squareCY + Middle);

                        vectorList.Add(p0); // 0, 5, etc
                        vectorList.Add(p1); // 1, 6, etc
                        vectorList.Add(p2); // 2, 7, etc
                        vectorList.Add(p3); // 3, 8, etc
                        vectorList.Add(middle); // 4, 9, etc

                        // Draw the wire-frame
                        
                        drawWireFrame((Vector3)vectorList[x], (Vector3)vectorList[x + 1], (Vector3)vectorList[x + 2], 
                            (Vector3)vectorList[x + 3], (Vector3)vectorList[x + 4]);

                        // Helps us accurately move through array-list
                        x += 5;
                    }

                }

                GL.End();
        }
            
            
        

        public SSLineObject (Vector3 mapPos) : base()
        {
           
            
            
        }
    }

}

