using System;

using OpenTK.Graphics.OpenGL;
namespace SimpleScene
{
	public class SSIndexBuffer : ISSInstancable
	{
        private readonly ISSVertexBuffer _vbo;
        private readonly BufferUsageHint _usageHint;
        private int _IBOid = 0;
        private int _numIndices = 0;
        private UInt16[] _lastAssignedIndices = null;

        public int numIndices { get { return _numIndices; } }
        public UInt16[] lastAssignedIndices { get { return _lastAssignedIndices; } }

        public SSIndexBuffer (ISSVertexBuffer vbo, BufferUsageHint hint = BufferUsageHint.DynamicDraw)
		{
            _vbo = vbo;
            _usageHint = hint;
		}

        public SSIndexBuffer(UInt16[] indices, ISSVertexBuffer vbo, BufferUsageHint hint = BufferUsageHint.StaticDraw) 
        : this(vbo, hint) 
        {
            UpdateBufferData(indices);
        }

		public void Delete() 
        {
			GL.DeleteBuffer (_IBOid);
            _IBOid = 0;
            _numIndices = 0;
		}

        public void UpdateBufferData(UInt16[] indices) 
        {
            _lastAssignedIndices = indices;
            if (_IBOid == 0) {
                _IBOid = GL.GenBuffer();
            }
            _numIndices = indices.Length;
            Bind();
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                         (IntPtr)(_numIndices * sizeof(UInt16)),
                         indices, 
                         _usageHint);
            Unbind();
        }

		public void DrawElements(SSRenderConfig renderConfig, PrimitiveType primType, bool doBind = true) 
        {
            if (doBind) {
				_vbo.DrawBind(renderConfig);
                Bind();
            }
            GL.DrawElements(primType,
                            _numIndices,
                            DrawElementsType.UnsignedShort,
                            IntPtr.Zero);
            if (doBind) {
                Unbind();
                _vbo.DrawUnbind();
            }
        }

        public void drawSingle(SSRenderConfig renderConfig, PrimitiveType primType)
        {
            DrawElements(renderConfig, primType);
        }

        public void drawInstanced(SSRenderConfig renderConfig, int instanceCount, PrimitiveType primType)
        {
			_vbo.DrawBind(renderConfig);
            Bind();
            GL.DrawElementsInstanced(
                primType,
                _numIndices,
                DrawElementsType.UnsignedShort,
                IntPtr.Zero,
                instanceCount
            );
            Unbind();
            _vbo.DrawUnbind();
        }

        public void Bind() {
			GL.BindBuffer (BufferTarget.ElementArrayBuffer, _IBOid);
		}

        public void Unbind() {
			GL.BindBuffer (BufferTarget.ElementArrayBuffer, 0);
		}
	}
}

