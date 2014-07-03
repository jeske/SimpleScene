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
        public readonly SSAbstractMesh mesh;
        
        public override void Render(ref SSRenderConfig renderConfig) {
			base.Render (ref renderConfig);
            this.mesh.RenderMesh(ref renderConfig);
        }
        public SSObjectMesh (SSAbstractMesh mesh) : base() {
            this.mesh = mesh;
        }
    }
}

