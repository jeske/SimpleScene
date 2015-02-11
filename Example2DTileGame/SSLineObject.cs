
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
        float squareWidth = 4;

        static int arrayW = 50;
        static int arrayH = 50;

        float[,] mapArray = new float[arrayW, arrayH]; // Holds base-heights
        float[,] peakArray = new float[arrayW, arrayH];

        bool isGenerating = true;
        int x = 0;

        float baseHeight = 0;
        float peakHeight = 0;

        int R = 25, G = 25, B = 25; // Default values for color

        struct LineData
        {
            public Vector3 Pos;
            public Color4 Color;

            public LineData(Vector3 pos, Color4 color)
            {
                this.Pos = pos;
                this.Color = color; 
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
            Vector3 p2, Vector3 p3, Vector3 Middle, Color4 lineColor)
        {
            #region Vector adding
            vectorList.Add(new LineData(p0, lineColor)); vectorList.Add(new LineData(p1, lineColor));
            vectorList.Add(new LineData(p0, lineColor)); vectorList.Add(new LineData(p2, lineColor));
            vectorList.Add(new LineData(p2, lineColor)); vectorList.Add(new LineData(p3, lineColor));
            vectorList.Add(new LineData(p3, lineColor)); vectorList.Add(new LineData(p1, lineColor));
            vectorList.Add(new LineData(p0, lineColor)); vectorList.Add(new LineData(Middle, lineColor));
            vectorList.Add(new LineData(p1, lineColor)); vectorList.Add(new LineData(Middle, lineColor));
            vectorList.Add(new LineData(p2, lineColor)); vectorList.Add(new LineData(Middle, lineColor));
            vectorList.Add(new LineData(p3, lineColor)); vectorList.Add(new LineData(Middle, lineColor));
            #endregion
        }

        public SSLineObject (Vector3 mapPos) : base()
        {
            Console.WriteLine("Set points");
            Random rand = new Random();
            float avgHeight = 0;
            float totalHeight = 0;


            ///////////////////////
            //Gather height data//
            /////////////////////
            {
                for (int i = 0; i < mapArray.GetLength(0) - 1; i++)
                {
                    for (int j = 0; j < mapArray.GetLength(1) - 1; j++)
                    {
                        // Stores a random height in every position of the
                        // mapArray[,]
                        peakHeight = rand.Next(0, 10); // Peak heights
                        peakArray[i, j] = peakHeight; // Store peak heights
                        baseHeight = rand.Next(0, 4); // Base heights                       
                        mapArray[i, j] = baseHeight; // Store base heights

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
                    for (int i = 0; i < mapArray.GetLength(0) - 1; i ++)
                    {
                        for (int j = 0; j < mapArray.GetLength(1) - 1; j ++)
                        {
                            // TODO -
                            // Get total heights [X]
                            // Get average of heights around the squares...[X]
                            // Set points equal to average of points[X]
                            h1 = peakArray[i, j];
                            h2 = peakArray[i + 1, j];
                            h3 = peakArray[i, j + 1];
                            h4 = peakArray[i + 1, j + 1];

                            totalHeight = h1 + h2 + h3 + h4; // Total peak height
                            avgHeight = totalHeight / 4; // Average peak height around

                            mapArray[i, j] = avgHeight;                           
                            
                        }
                    }
                }
            }

            //////////////////////////
            //Generate the map mesh//
            ////////////////////////            
            for (int i = 0; i < mapArray.GetLength(0) - 1; i++)
            {
                for (int j = 0; j < mapArray.GetLength(1) - 1; j++)
                {
                    float Middle = squareWidth / 2; // Middle point of the square
                    float squareCX = i * squareWidth;
                    float squareCY = j * squareWidth;

                    Color4 color;

                    #region Color switch
                    color = new Color4(0f, 0f, 0f, 1f);
                    float heightFactor = 0.1f * (float)peakArray[i, j];
                    color.G += heightFactor;
                    color.B += heightFactor;
                    color.R += heightFactor;

                    if (heightFactor == 0)
                    {
                        color.G = 0.5f;
                        color.B = 0.1f;
                        color.R = 0.1f;
                    }

                    #endregion

                    p0 = new Vector3(squareCX, mapArray[i, j], squareCY);
                    p1 = new Vector3(squareCX + squareWidth, mapArray[i + 1, j], squareCY);
                    p2 = new Vector3(squareCX, mapArray[i, j + 1], squareCY + squareWidth);
                    p3 = new Vector3(squareCX + squareWidth, mapArray[i + 1, j + 1], squareCY + squareWidth);

                    // Determines height
                    middle = new Vector3(squareCX + Middle, peakArray[i, j], squareCY + Middle);


                    AddToArray(p0, p1, p2, p3, middle, color);


                }

            }
            
        }



    }

}

