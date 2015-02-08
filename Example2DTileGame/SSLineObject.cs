
using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OpenTK.Input;
using SimpleScene;



namespace Example2DTileGame
{
    public class SSLineObject : SSObject
    {
        #region Variables
        Vector3 mapPosition;
        float squareWidth = 4;

        Vector3[,] mapArray = new Vector3[50, 50]; // W x D map (X & Z)

        bool isGenerating = true;
        int x = 0;

        float vHeight = 0;
        float currentHeight;


        struct LineData
        {
            public Vector3 Pos;

            public LineData(Vector3 pos)
            {
                this.Pos = pos;
            }
        }

        List<LineData> vectorList = new List<LineData>();
        
        // Default values of square - should never actually set anything to anything
        Vector3 p0 = new Vector3(0, 0, 0);
        Vector3 p1 = new Vector3(0, 0, 0);
        Vector3 p2 = new Vector3(0, 0, 0);
        Vector3 p3 = new Vector3(0, 0, 0);
        Vector3 middle = new Vector3(0, 0, 0);

        #endregion
        
        /// <summary>
        /// draw a 'wire - frame' of the map
        /// </summary>
        public void drawWireFrame()
        {
            GL.Begin(PrimitiveType.Lines);
            foreach (LineData v in vectorList)
            {

                #region Color coding

                #endregion
                // Draw each point added
                GL.Vertex3(v.Pos);

            }
            GL.End();
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
                GL.Color3(1f, 1f, 1f);
                drawWireFrame(); // Draw it               
        }

        /// <summary>
        /// Adds points into array-list
        /// </summary>
        public void AddToArray(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 Middle)
        {
            vectorList.Add(new LineData(p0)); vectorList.Add(new LineData(p1));
            vectorList.Add(new LineData(p0)); vectorList.Add(new LineData(p2));
            vectorList.Add(new LineData(p2)); vectorList.Add(new LineData(p3));
            vectorList.Add(new LineData(p3)); vectorList.Add(new LineData(p1));
            vectorList.Add(new LineData(p0)); vectorList.Add(new LineData(Middle));
            vectorList.Add(new LineData(p1)); vectorList.Add(new LineData(Middle));
            vectorList.Add(new LineData(p2)); vectorList.Add(new LineData(Middle));
            vectorList.Add(new LineData(p3)); vectorList.Add(new LineData(Middle));
        }

        /// <summary>
        /// Relax the map
        /// </summary>
        public void Smoothing()
        {

        }

        public SSLineObject (Vector3 mapPos) : base()
        {
            Random rand = new Random();
            Console.WriteLine("Set points");
            for (int i = 0; i < mapArray.GetLength(0); i++)
            {
                for (int j = 0; j < mapArray.GetLength(1); j++)
                {
                    float Middle = squareWidth / 2; // Middle point of the square
                    float squareCX = i * squareWidth;
                    float squareCY = j * squareWidth;

                    p0 = new Vector3(squareCX, vHeight, squareCY);
                    p1 = new Vector3(squareCX + squareWidth, vHeight, squareCY);
                    p2 = new Vector3(squareCX, vHeight, squareCY + squareWidth);
                    p3 = new Vector3(squareCX + squareWidth, vHeight, squareCY + squareWidth);

                    // Determines height
                    middle = new Vector3(squareCX + Middle, rand.Next(0, 10), squareCY + Middle);

                    AddToArray(p0, p1, p2, p3, middle);

                }

            }
            
        }



    }

}

