
using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OpenTK.Input;
using SimpleScene;



namespace Example2DTileGame
{
    public class SSObjectMapMesh : SSObject
    {
        
        #region Variables
        // --------------------------------------------------------------------------------

        float squareWidth = 4;

        static int arrayW = 50;
        static int arrayH = 50;

        float[,] mapHeights = new float[arrayW, arrayH]; // Holds base-heights       
 
        float MAX_HEIGHT = 40.0f;

        bool isGenerating = true;                

        int R = 25, G = 25, B = 25; // Default values for color

        struct VertexData
        {
            public Vector3 Pos;
            public Color4 Color;

            public VertexData(Vector3 pos, Color4 color)
            {
                this.Pos = pos;
                this.Color = color; 
            }
        }

        List<VertexData> vectorList = new List<VertexData>();
        
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
            foreach (VertexData v in vectorList)
            {
                
                GL.Color4(v.Color);
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
                drawWireFrame(); // Draw it               
        }

        /// <summary>
        /// Adds points into array-list
        /// </summary>
        public void AddToArray(Vector3 p0, Vector3 p1, 
            Vector3 p2, Vector3 p3, Vector3 Middle)
        {
            vectorList.Add(new VertexData(p0, colorForHeight(p0.Y))); vectorList.Add(new VertexData(p1, colorForHeight(p0.Y)));
            vectorList.Add(new VertexData(p0, colorForHeight(p0.Y))); vectorList.Add(new VertexData(p2, colorForHeight(p0.Y)));
            vectorList.Add(new VertexData(p2, colorForHeight(p0.Y))); vectorList.Add(new VertexData(p3, colorForHeight(p0.Y)));
            vectorList.Add(new VertexData(p3, colorForHeight(p0.Y))); vectorList.Add(new VertexData(p1, colorForHeight(p0.Y)));
            vectorList.Add(new VertexData(p0, colorForHeight(p0.Y))); vectorList.Add(new VertexData(Middle, colorForHeight(p0.Y)));
            vectorList.Add(new VertexData(p1, colorForHeight(p0.Y))); vectorList.Add(new VertexData(Middle, colorForHeight(p0.Y)));
            vectorList.Add(new VertexData(p2, colorForHeight(p0.Y))); vectorList.Add(new VertexData(Middle, colorForHeight(p0.Y)));
            vectorList.Add(new VertexData(p3, colorForHeight(p0.Y))); vectorList.Add(new VertexData(Middle, colorForHeight(p0.Y)));            
        }

        public SSObjectMapMesh () : base()
        {
            Console.WriteLine("Set points");
            Random rand = new Random();
            float avgHeight = 0;
            float totalHeight = 0;


            ///////////////////////
            //Gather height data//
            /////////////////////
            {
                for (int i = 0; i < mapHeights.GetLength(0) - 1; i++)
                {
                    for (int j = 0; j < mapHeights.GetLength(1) - 1; j++)
                    {
                        // Stores a random height in every position of the
                        // mapHeights[,] array                        
                        mapHeights[i, j] = (float)rand.NextDouble() * MAX_HEIGHT; 
                    }
                }
            }
            
            ///////////////////
            //Relax the data//
            /////////////////
            {
                float h1;
                float h2;
                float h3;
                float h4;
                float peakH;

                for (int n = 0; n < 6; n++)
                {
                    // make a temp array to hold the output
                    float[,] tempMapHeights = new float[arrayW, arrayH]; // Holds base-heights        

                    for (int i = 1; i < mapHeights.GetLength(0) - 1; i ++)
                    {
                        for (int j = 1; j < mapHeights.GetLength(1) - 1; j ++)
                        {
                            // TODO -
                            // Get total heights [X]
                            // Get average of heights around the squares...[X]
                            // Set points equal to average of points[X]
                            h1 = mapHeights[i - 1, j - 1];
                            h2 = mapHeights[i - 1, j + 0];
                            h3 = mapHeights[i + 1, j + 0];
                            h4 = mapHeights[i + 1, j + 1];

                            totalHeight = h1 + h2 + h3 + h4; // Total peak height
                            avgHeight = totalHeight / 4; // Average peak height around

                            tempMapHeights[i, j] = avgHeight;                                                       
                        }
                    }

                    // now that we're done computing the relax, replace the previous mapHeights array
                    mapHeights = tempMapHeights;
                }
            }

            //////////////////////////
            //Generate the map mesh//
            ////////////////////////            
            for (int i = 1; i < mapHeights.GetLength(0) - 1; i++)
            {
                for (int j = 1; j < mapHeights.GetLength(1) - 1; j++)
                {
                    float middleHeight = 0.1f * (float)mapHeights[i, j];

                    float Middle = squareWidth / 2; // Middle point of the square
                    float squareCX = i * squareWidth;
                    float squareCY = j * squareWidth;

                    // height of the middle point is easy, it's just our height..
                    middle = new Vector3(squareCX + Middle, mapHeights[i, j], squareCY + Middle);

                    // height of other points is the average of the 2x2 grid around that point.
                    p0 = new Vector3(squareCX, average2x2Height(i,j), squareCY);
                    p1 = new Vector3(squareCX + squareWidth, average2x2Height(i + 1, j), squareCY);
                    p2 = new Vector3(squareCX, average2x2Height(i, j + 1), squareCY + squareWidth);
                    p3 = new Vector3(squareCX + squareWidth, average2x2Height(i + 1, j + 1), squareCY + squareWidth);
                    
                    AddToArray(p0, p1, p2, p3, middle);

                }

            }
            
        } // end of constructor

        Color4 colorForHeight(float height) {
            return new Color4(height / MAX_HEIGHT, height / MAX_HEIGHT, height / MAX_HEIGHT, 0);
        }

        // x,y is the coordinate of the lower-right corner of the 2x2 
        float average2x2Height(int x, int y) {
            return ( mapHeights[x-1,y-1] + 
                     mapHeights[x-1,y] + 
                     mapHeights[x,y-1] +
                     mapHeights[x,y]) / 4.0f;
        }

    }

}

