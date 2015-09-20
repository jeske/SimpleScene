using System;
using System.Collections.Generic;
using System.Drawing;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSMeshDisk : SSIndexedMesh<SSVertex_PosTex>
    {
        public SSMeshDisk (int divisions = 50, float texOffset = 0.1f)
            : base(BufferUsageHint.StaticDraw, BufferUsageHint.StaticDraw)
        {
            // generate vertices
            SSVertex_PosTex[] vertices = new SSVertex_PosTex[divisions + 1];
            vertices [0] = new SSVertex_PosTex (0f, 0f, 0f, 0.5f, 0.5f);

            float angleStep = 2f * (float)Math.PI / divisions;
            float Tr = 0.5f + texOffset;

            for (int i = 0; i < divisions; ++i) {
                float angle = i * angleStep;
                float x = (float)Math.Cos(angle);
                float y = (float)Math.Sin(angle);
                vertices [i + 1] = new SSVertex_PosTex (
                    x, y, 0f,
                    0.5f + Tr * x, 0.5f + Tr * y
                );       
            }
            updateVertices(vertices);

            // generate indices
            UInt16[] indices = new UInt16[divisions * 3];
            for (int i = 0; i < divisions; ++i) {
                int baseIdx = i * 3;
                indices [baseIdx] = 0;
                indices [baseIdx + 1] = (UInt16)(i + 1);
                indices [baseIdx + 2] = (UInt16)(i + 2);
            }
            // last one is a special case (wraparound)
            indices [indices.Length - 1] = 1;
            updateIndices(indices);
        }

        public override void renderMesh(SSRenderConfig renderConfig)
        {
            SSShaderProgram.DeactivateAll();

            base.renderMesh(renderConfig);
        }
    }
}

