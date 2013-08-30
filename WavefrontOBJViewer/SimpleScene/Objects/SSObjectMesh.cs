using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace WavefrontOBJViewer
{
    public class SSObjectMesh : SSObject
    {
        public readonly SSMesh mesh;
        
        public override void Render() {
            this.mesh.Render();
        }
        public SSObjectMesh (SSMesh mesh) : base() {
            this.mesh = mesh;
        }
    }
}

