using System;

namespace SimpleScene
{
    public class SSQuad
    {
        public static readonly SSVertexBuffer<SSVertex_Pos> singleFaceInstance;
        public static readonly SSVertexBuffer<SSVertex_Pos> doubleFaceInstance;

        public static readonly SSVertex_Pos[] singleFaceVertices = {
            // CCW quad; no indexing
            new SSVertex_Pos(-.5f, -.5f, 0f),
            new SSVertex_Pos(+.5f, -.5f, 0f),
            new SSVertex_Pos(+.5f, +.5f, 0f),

            new SSVertex_Pos(-.5f, -.5f, 0f),
            new SSVertex_Pos(+.5f, +.5f, 0f),
            new SSVertex_Pos(-.5f, +.5f, 0f),
        };

        public static readonly SSVertex_Pos[] doubleFaceVertices = {
            // CCW quad; no indexing
            new SSVertex_Pos(-.5f, -.5f, 0f),
            new SSVertex_Pos(+.5f, -.5f, 0f),
            new SSVertex_Pos(+.5f, +.5f, 0f),

            new SSVertex_Pos(-.5f, -.5f, 0f),
            new SSVertex_Pos(+.5f, +.5f, 0f),
            new SSVertex_Pos(-.5f, +.5f, 0f),

            new SSVertex_Pos(-.5f, -.5f, 0f),
            new SSVertex_Pos(+.5f, +.5f, 0f),
            new SSVertex_Pos(+.5f, -.5f, 0f),

            new SSVertex_Pos(-.5f, -.5f, 0f),
            new SSVertex_Pos(-.5f, +.5f, 0f),
            new SSVertex_Pos(+.5f, +.5f, 0f),
        };

        static SSQuad()
        {
            singleFaceInstance = new SSVertexBuffer<SSVertex_Pos> (singleFaceVertices);
            doubleFaceInstance = new SSVertexBuffer<SSVertex_Pos> (doubleFaceVertices);
        }
    }
}

