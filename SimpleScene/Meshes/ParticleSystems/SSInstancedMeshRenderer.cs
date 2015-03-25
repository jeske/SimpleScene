using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public interface ISSInstancable
    {
        /// <summary>
        /// Render a number of instances of the mesh. Attribute arrays must be prepared prior to use.
        /// </summary>
		void RenderInstanced(ref SSRenderConfig renderConfig, int instanceCount, PrimitiveType primType);
    }

    /// <summary>
    /// Renders particle system with attribute buffers and an ISSInstancable object
    /// </summary>
    public class SSInstancedMeshRenderer : SSObject
    {
        // TODO Draw any ibo/vbo mesh

        public const BufferUsageHint c_usageHint = BufferUsageHint.StreamDraw;

		public SSParticleSystem ParticleSystem;

        public bool SimulateOnUpdate = true;
        public bool AlphaBlendingEnabled = true;
		public bool DepthRead = true;
		public bool DepthWrite = true;
		public bool GlobalBillboarding = false;
        public ISSInstancable Mesh;


        protected SSAttributeBuffer<SSAttributeVec3> m_posBuffer;
		protected SSAttributeBuffer<SSAttributeVec2> m_orientationXYBuffer;
		protected SSAttributeBuffer<SSAttributeFloat> m_orientationZBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> m_masterScaleBuffer;
        protected SSAttributeBuffer<SSAttributeVec2> m_componentScaleXYBuffer;
		protected SSAttributeBuffer<SSAttributeFloat> m_componentScaleZBuffer;
        protected SSAttributeBuffer<SSAttributeColor> m_colorBuffer;

		//protected SSAttributeBuffer<SSAttributeByte> m_spriteIndexBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> m_spriteOffsetUBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> m_spriteOffsetVBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> m_spriteSizeUBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> m_spriteSizeVBuffer;
        protected SSTexture m_texture;

        public SSInstancedMeshRenderer (SSParticleSystem ps, 
										SSTexture texture, 
										ISSInstancable mesh = null,
										BufferUsageHint hint = BufferUsageHint.StreamDraw)
        {
            Mesh = mesh;
            ParticleSystem = ps;
            m_texture = texture;
			m_posBuffer = new SSAttributeBuffer<SSAttributeVec3> (hint);
			m_orientationXYBuffer = new SSAttributeBuffer<SSAttributeVec2> (hint);
			m_orientationZBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
			m_masterScaleBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
			m_componentScaleXYBuffer = new SSAttributeBuffer<SSAttributeVec2> (hint);
			m_componentScaleZBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
			m_colorBuffer = new SSAttributeBuffer<SSAttributeColor> (hint);

			//m_spriteIndexBuffer = new SSAttributeBuffer<SSAttributeByte> (hint);
			m_spriteOffsetUBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
			m_spriteOffsetVBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
			m_spriteSizeUBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
			m_spriteSizeVBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
        }

        public override void Render (ref SSRenderConfig renderConfig)
        {
			Matrix4 modelView = this.worldMat * renderConfig.invCameraViewMat;

			// allow particle system to react to camera/worldview
			ParticleSystem.UpdateCamera (ref modelView, ref renderConfig.projectionMatrix);

			// do we have anything to draw?
			if (ParticleSystem.ActiveBlockLength <= 0) return;

			// allow frustum culling to react to particle system expanding/shrinking
			this.boundingSphere = new SSObjectSphere (ParticleSystem.Radius);

            base.Render(ref renderConfig);

			// select either instance shader or instance pssm shader
			ISSInstancableShaderProgram instanceShader = renderConfig.InstanceShader;

			if (renderConfig.drawingShadowMap) {
				if (renderConfig.drawingPssm) {
					renderConfig.InstancePssmShader.Activate ();
					renderConfig.InstancePssmShader.UniObjectWorldTransform = this.worldMat;
					instanceShader = renderConfig.InstancePssmShader;
				} 
			} else {
				if (!GlobalBillboarding && AlphaBlendingEnabled) {
					// Must be called before updating buffers
					ParticleSystem.SortByDepth (ref modelView);

					//GL.Enable(EnableCap.AlphaTest);
					//GL.AlphaFunc(AlphaFunction.Greater, 0.01f);
					GL.Enable (EnableCap.Blend);
					GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
					GL.Disable (EnableCap.Lighting);

					// Fixes flicker issues for particles with "fighting" view depth values
					// Also assumes the particle system is the last to be drawn in a scene
					GL.DepthFunc (DepthFunction.Lequal);
				}
				if (DepthRead) {
					GL.Enable (EnableCap.DepthTest);
				} else {
					GL.Disable (EnableCap.DepthTest);
				}
				GL.DepthMask (DepthWrite);

				// texture binding setup
				if (m_texture != null) {
					GL.ActiveTexture (TextureUnit.Texture0);
					GL.Enable(EnableCap.Texture2D);
					GL.BindTexture(TextureTarget.Texture2D, m_texture.TextureID);
				}
			}

			if (GlobalBillboarding) {
				// Setup "global" billboarding. (entire particle system is rendered as a camera-facing
				// billboard and will show the same position of particles from all angles)
				modelView = OpenTKHelper.BillboardMatrix (ref modelView);
				GL.MatrixMode (MatrixMode.Modelview);
				GL.LoadMatrix (ref modelView);
			}

			instanceShader.Activate ();

            // prepare attribute arrays for draw
            GL.PushClientAttrib(ClientAttribMask.ClientAllAttribBits);
            prepareAttribute(m_posBuffer, instanceShader.AttrInstancePos, 
				ParticleSystem.Positions);
			prepareAttribute(m_orientationXYBuffer, instanceShader.AttrInstanceOrientationXY,
				ParticleSystem.OrientationsXY);
			prepareAttribute(m_orientationZBuffer, instanceShader.AttrInstanceOrientationZ, 
				ParticleSystem.OrientationsZ);
            prepareAttribute(m_masterScaleBuffer, instanceShader.AttrInstanceMasterScale, 
				ParticleSystem.MasterScales);
			prepareAttribute(m_componentScaleXYBuffer, instanceShader.AttrInstanceComponentScaleXY, 
				ParticleSystem.ComponentScalesXY);
			prepareAttribute(m_componentScaleZBuffer, instanceShader.AttrInstanceComponentScaleZ,
				ParticleSystem.ComponentScalesZ);
            prepareAttribute(m_colorBuffer, instanceShader.AttrInstanceColor, 
				ParticleSystem.Colors);

			//prepareAttribute(m_spriteIndexBuffer, instanceShader.AttrInstanceSpriteIndex, m_ps.SpriteIndices);
            prepareAttribute(m_spriteOffsetUBuffer, instanceShader.AttrInstanceSpriteOffsetU, 
				ParticleSystem.SpriteOffsetsU);
            prepareAttribute(m_spriteOffsetVBuffer, instanceShader.AttrInstanceSpriteOffsetV, 
				ParticleSystem.SpriteOffsetsV);
            prepareAttribute(m_spriteSizeUBuffer, instanceShader.AttrInstanceSpriteSizeU, 
				ParticleSystem.SpriteSizesU);
            prepareAttribute(m_spriteSizeVBuffer, instanceShader.AttrInstanceSpriteSizeV, 
				ParticleSystem.SpriteSizesV);

            // do the draw
            Mesh.RenderInstanced(ref renderConfig, ParticleSystem.ActiveBlockLength, PrimitiveType.Triangles);
             
            GL.PopClientAttrib();
            //this.boundingSphere.Render(ref renderConfig);
        }

        void prepareAttribute<AB, A>(AB attrBuff, int attrLoc, A[] array) 
            where A : struct, ISSAttributeLayout 
            where AB : SSAttributeBuffer<A>
        {
            int numActive = ParticleSystem.ActiveBlockLength;
            int numInstancesPerValue = array.Length < numActive ? numActive : 1;
			attrBuff.PrepareAttributeAndUpdate(attrLoc, numInstancesPerValue, array);
        }

        public override void Update (float fElapsedMS)
        {
            if (SimulateOnUpdate) {
                ParticleSystem.Simulate(fElapsedMS);
            }
        }
    }
}

