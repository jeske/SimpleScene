using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public interface ISSVertexLayout 
    {
        void BindGlAttributes();
    }

    public interface ISSVertexBuffer
    {
        void DrawBind();
        void DrawUnbind();
    }

    // http://www.opentk.com/doc/graphics/geometry/vertex-buffer-objects
    public class SSVertexBuffer<Vertex> : SSArrayBuffer<Vertex>, ISSVertexBuffer, ISSInstancable
        where Vertex : struct, ISSVertexLayout 
    {
        public SSVertexBuffer(BufferUsageHint hint = BufferUsageHint.DynamicDraw)
            : base(hint)
        { }

        public SSVertexBuffer (Vertex[] vertices, 
                               BufferUsageHint hint = BufferUsageHint.StaticDraw) 
            : base(vertices, hint)
        { }

        public void DrawArrays(PrimitiveType primType, bool doBind = true) {
            if (doBind) DrawBind();
            drawPrivate(primType);
            if (doBind) DrawUnbind();
        }

        /// <summary>
        /// Draws the arrays instanced. Attribute arrays must be prepared prior to use.
        /// </summary>
        public void RenderInstanced(int numInstances, PrimitiveType primType)
        {
            DrawBind();
            GL.DrawArraysInstanced(primType, 0, NumElements, numInstances);
            DrawUnbind();
        }

        public void UpdateAndDrawArrays(Vertex[] vertices,
                                        PrimitiveType primType,
                                        bool doBind = true)
        {
            genBufferPrivate();
            if (doBind) DrawBind();
            updatePrivate(vertices);
            drawPrivate(primType);
            if (doBind) DrawUnbind();
        }

        public void DrawBind() {
            // bind for use and setup for drawing
            bind();
            GL.PushClientAttrib(ClientAttribMask.ClientAllAttribBits);
            c_dummyElement.BindGlAttributes();
        }

        public void DrawUnbind() {
            // unbind from use and undo draw settings
            GL.PopClientAttrib();
            unbind();
        }

        protected void drawPrivate(PrimitiveType primType) {
            GL.DrawArrays(primType, 0, NumElements);
        }
	}
}

