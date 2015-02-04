
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
        float x;
        
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
            GL.Vertex3(p0);
            GL.Vertex3(p1);

            GL.Vertex3(p3);
            GL.Vertex3(p1);

            GL.Vertex3(p0);
            GL.Vertex3(p2);

            GL.Vertex3(p2);
            GL.Vertex3(p3); 

            // Setting up middle

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
                GL.Disable(EnableCap.Blend);
                GL.Disable(EnableCap.Lighting);

                //!important!

                float Middle = squareWidth / 2; // Middle point of the square


                GL.Begin(PrimitiveType.Lines);
                GL.Color3(1f, 0f, 0.8f);
                Random rand = new Random();


                for (int i = 0; i < mapArray.GetLength(0); i++)
                {
                    for (int j = 0; j < mapArray.GetLength(1); j++)
                    {

                        float squareCX = i * squareWidth;
                        float squareCY = j * squareWidth;
                        
                        p0 = new Vector3(squareCX, 0, squareCY);
                        p1 = new Vector3(squareCX + squareWidth, 0, squareCY);
                        p2 = new Vector3(squareCX, 0, squareCY + squareWidth);
                        p3 = new Vector3(squareCX + squareWidth, 0, squareCY + squareWidth);

                        // Determines height
                        middle = new Vector3(squareCX + Middle, i, squareCY + Middle);

                        mapArray[i, j] = middle;
                        middle = mapArray[i, j];

                        drawWireFrame(p0, p1, p2, p3, middle); // Draw the wire-frame
                        

                        /*// If height is 'x' then adjuste other points to match
                        if (mapArray[i, j].Equals
                            (new Vector3(squareCX + Middle, rand.Next(), squareCY + Middle)))
                        {
                            p0 = new Vector3(squareCX, 0, squareCY);
                            p1 = new Vector3(squareCX + squareWidth, 0, squareCY);
                            p2 = new Vector3(squareCX, 0, squareCY + squareWidth);
                            p3 = new Vector3(squareCX + squareWidth, 0, squareCY + squareWidth);
                        }

                        Console.WriteLine("Middle: " + mapArray[i, j].ToString());*/

                    }
                   
                }


                isGenerating = false; // No longer generating
                GL.End();

            }
            
            
        

        public SSLineObject (Vector3 mapPos) : base()
        {
            mapPosition = mapPos; // get the map objects position
            // This runs the base (SSObject) which runs Render (...) which runs drawWireFrame(...)
        }
    }

}

