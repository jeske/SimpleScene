using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public interface ISSVertexLayout 
    {
		void BindGlAttributes(SSRenderConfig renderConfig);
        Vector3 _position { get; }
    }

    public interface ISSVertexBuffer
    {
		void DrawBind(SSRenderConfig renderConfig);
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

		public void DrawArrays(SSRenderConfig renderConfig, PrimitiveType primType, bool doBind = true) {
			if (doBind) DrawBind(renderConfig);
            drawPrivate(primType);
            if (doBind) DrawUnbind();
        }

        /// <summary>
        /// Draws the arrays instanced. Attribute arrays must be prepared prior to use.
        /// </summary>
		public void drawInstanced(SSRenderConfig renderConfig, int numInstances, PrimitiveType primType)
        {
			DrawBind(renderConfig);
            GL.DrawArraysInstanced(primType, 0, numElements, numInstances);
            DrawUnbind();
        }

        public void drawSingle(SSRenderConfig renderConfig, PrimitiveType primType)
        {
            DrawArrays(renderConfig, primType);
        }

		public void UpdateAndDrawArrays(SSRenderConfig renderConfig, 
										Vertex[] vertices,
                                        PrimitiveType primType,
                                        bool doBind = true)
        {
            genBufferPrivate();
			if (doBind) DrawBind(renderConfig);
            updatePrivate(vertices);
            drawPrivate(primType);
            if (doBind) DrawUnbind();
        }

		public void DrawBind(SSRenderConfig renderConfig) {
            // bind for use and setup for drawing
            bind();
            GL.PushClientAttrib(ClientAttribMask.ClientAllAttribBits);
			c_dummyElement.BindGlAttributes(renderConfig);
        }

        public void DrawUnbind() {
            // unbind from use and undo draw settings
            GL.PopClientAttrib();
            unbind();
        }

        protected void drawPrivate(PrimitiveType primType) {
            GL.DrawArrays(primType, 0, numElements);
        }
	}
}

