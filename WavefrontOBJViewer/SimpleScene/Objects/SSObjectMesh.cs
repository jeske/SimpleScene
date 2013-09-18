// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace WavefrontOBJViewer
{
    public class SSObjectMesh : SSObject
    {
        public readonly SSMesh mesh;
        
        public override void Render(SSRenderConfig renderConfig) {
            this.mesh.RenderMesh(renderConfig);
        }
        public SSObjectMesh (SSMesh mesh) : base() {
            this.mesh = mesh;
        }
    }
}

