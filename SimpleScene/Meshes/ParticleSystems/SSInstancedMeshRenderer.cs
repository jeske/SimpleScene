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

        public enum BillboardingType { None, Global };

        public const BufferUsageHint c_usageHint = BufferUsageHint.StreamDraw;

		public SSParticleSystem ParticleSystem;

        public bool SimulateOnUpdate = true;
        public bool AlphaBlendingEnabled = true;
		public bool DepthRead = true;
		public bool DepthWrite = true;
		public BillboardingType Billboarding = BillboardingType.None;
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
			// affects next frame
			ParticleSystem.UpdateCamera (ref modelView, ref renderConfig.projectionMatrix);

			if (ParticleSystem.ActiveBlockLength <= 0) return;

			this.boundingSphere = new SSObjectSphere (ParticleSystem.Radius);

            base.Render(ref renderConfig);

            if (Billboarding == BillboardingType.Global) {
                // Setup "global" billboarding. (entire particle system is rendered as a camera-facing
                // billboard and will show the same position of particles from all angles)
                modelView = OpenTKHelper.BillboardMatrix(ref modelView);
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadMatrix(ref modelView);
            }
            // Update buffers early for better streaming
            if (AlphaBlendingEnabled) {
                // Must be called before updating buffers
                ParticleSystem.SortByDepth(ref modelView);
            }
			#if false
            m_posBuffer.UpdateBufferData(ParticleSystem.Positions);
			m_orientationXYBuffer.UpdateBufferData(ParticleSystem.OrientationsXY);
			m_orientationZBuffer.UpdateBufferData (ParticleSystem.OrientationsZ);
            m_masterScaleBuffer.UpdateBufferData(ParticleSystem.MasterScales);
            m_componentScaleXYBuffer.UpdateBufferData(ParticleSystem.ComponentScalesXY);
			m_componentScaleZBuffer.UpdateBufferData (ParticleSystem.ComponentScalesZ);
            m_colorBuffer.UpdateBufferData(ParticleSystem.Colors);

			//m_spriteIndexBuffer.UpdateBufferData(m_ps.SpriteIndices);
            m_spriteOffsetUBuffer.UpdateBufferData(ParticleSystem.SpriteOffsetsU);
            m_spriteOffsetVBuffer.UpdateBufferData(ParticleSystem.SpriteOffsetsV);
            m_spriteSizeUBuffer.UpdateBufferData(ParticleSystem.SpriteSizesU);
            m_spriteSizeVBuffer.UpdateBufferData(ParticleSystem.SpriteSizesV);
			#endif

            if (AlphaBlendingEnabled) {
				//GL.Enable(EnableCap.AlphaTest);
				//GL.AlphaFunc(AlphaFunction.Greater, 0.01f);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                GL.Disable(EnableCap.Lighting);

                // Fixes flicker issues for particles with "fighting" view depth values
                // Also assumes the particle system is the last to be drawn in a scene
                GL.DepthFunc(DepthFunction.Lequal);
            }
			if (DepthRead) {
				GL.Enable (EnableCap.DepthTest);
			} else {
				GL.Disable (EnableCap.DepthTest);
			}

			GL.DepthMask (DepthWrite);

            #if MAIN_SHADER_INSTANCING
            // draw using the main shader
            // TODO: debug with bump mapped lighting mode
            SSMainShaderProgram shader = renderConfig.MainShader;
            shader.UniAmbTexEnabled = true;
            shader.UniDiffTexEnabled = false;
            shader.UniSpecTexEnabled = false;
            shader.UniBumpTexEnabled = false;
            // texture slot setup
            GL.ActiveTexture(TextureUnit.Texture2);
            #else
            // draw using the instancing shader
            SSInstanceShaderProgram shader = renderConfig.InstanceShader;
            // texture slot setup
            GL.ActiveTexture(TextureUnit.Texture0);
            #endif
            //GL.Disable(EnableCap.ColorMaterial);

			// activate shader first.... 
			shader.Activate();

            // texture binding setup
            if (m_texture != null) {
                GL.Enable(EnableCap.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, m_texture.TextureID);
            }

            
            // prepare uniforms
            #if MAIN_SHADER_INSTANCING
            shader.UniInstanceDrawEnabled = true;
            #endif

            // prepare attribute arrays for draw
            GL.PushClientAttrib(ClientAttribMask.ClientAllAttribBits);
            prepareAttribute(m_posBuffer, shader.AttrInstancePos, ParticleSystem.Positions);
			prepareAttribute(m_orientationXYBuffer, shader.AttrInstanceOrientationXY, ParticleSystem.OrientationsXY);
			prepareAttribute(m_orientationZBuffer, shader.AttrInstanceOrientationZ, ParticleSystem.OrientationsZ);
            prepareAttribute(m_masterScaleBuffer, shader.AttrInstanceMasterScale, ParticleSystem.MasterScales);
			prepareAttribute(m_componentScaleXYBuffer, shader.AttrInstanceComponentScaleXY, ParticleSystem.ComponentScalesXY);
			prepareAttribute(m_componentScaleZBuffer, shader.AttrInstanceComponentScaleZ, ParticleSystem.ComponentScalesZ);
            prepareAttribute(m_colorBuffer, shader.AttrInstanceColor, ParticleSystem.Colors);

			//prepareAttribute(m_spriteIndexBuffer, shader.AttrInstanceSpriteIndex, m_ps.SpriteIndices);
            prepareAttribute(m_spriteOffsetUBuffer, shader.AttrInstanceSpriteOffsetU, ParticleSystem.SpriteOffsetsU);
            prepareAttribute(m_spriteOffsetVBuffer, shader.AttrInstanceSpriteOffsetV, ParticleSystem.SpriteOffsetsV);
            prepareAttribute(m_spriteSizeUBuffer, shader.AttrInstanceSpriteSizeU, ParticleSystem.SpriteSizesU);
            prepareAttribute(m_spriteSizeVBuffer, shader.AttrInstanceSpriteSizeV, ParticleSystem.SpriteSizesV);

            // do the draw
            Mesh.RenderInstanced(ref renderConfig, ParticleSystem.ActiveBlockLength, PrimitiveType.Triangles);
             
            GL.PopClientAttrib();
            #if MAIN_SHADER_INSTANCING
            shader.UniInstanceDrawEnabled = false;
            #endif

            #if false
            m_posBuffer.DisableAttribute(shader.AttrInstancePos);
            m_masterScaleBuffer.DisableAttribute(shader.AttrInstanceMasterScale);
            m_componentScaleBuffer.DisableAttribute(shader.AttrInstanceComponentScale);
            m_colorBuffer.DisableAttribute(shader.AttrInstanceColor);

            m_spriteIndexBuffer.DisableAttribute(shader.AttrInstanceSpriteIndex);
            m_spriteOffsetUBuffer.DisableAttribute(shader.AttrInstanceSpriteOffsetU);
            m_spriteOffsetVBuffer.DisableAttribute(shader.AttrInstanceSpriteOffsetV);
            m_spriteSizeUBuffer.DisableAttribute(shader.AttrInstanceSpriteSizeU);
            m_spriteSizeVBuffer.DisableAttribute(shader.AttrInstanceSpriteSizeV);
            #endif
            //this.boundingSphere.Render(ref renderConfig);
        }

        void prepareAttribute<AB, A>(AB attrBuff, int attrLoc, A[] array) 
            where A : struct, ISSAttributeLayout 
            where AB : SSAttributeBuffer<A>
        {
            int numActive = ParticleSystem.ActiveBlockLength;
            int numInstancesPerValue = array.Length < numActive ? numActive : 1;
			int numToUpdate = array.Length < numActive ? 1 : numActive;
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

