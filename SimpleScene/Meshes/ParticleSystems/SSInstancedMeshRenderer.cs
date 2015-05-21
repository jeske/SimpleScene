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

        public const BufferUsageHint _defaultUsageHint = BufferUsageHint.StreamDraw;

		public SSParticleSystem particleSystem;

        public bool simulateOnUpdate = true;
		public bool depthRead = true;
		public bool depthWrite = true;
		public bool globalBillboarding = false;
        public ISSInstancable mesh;

        protected SSAttributeBuffer<SSAttributeVec3> _posBuffer;
		protected SSAttributeBuffer<SSAttributeVec2> _orientationXYBuffer;
		protected SSAttributeBuffer<SSAttributeFloat> _orientationZBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> _masterScaleBuffer;
        protected SSAttributeBuffer<SSAttributeVec2> _componentScaleXYBuffer;
		protected SSAttributeBuffer<SSAttributeFloat> _componentScaleZBuffer;
        protected SSAttributeBuffer<SSAttributeColor> _colorBuffer;

		//protected SSAttributeBuffer<SSAttributeByte> m_spriteIndexBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> _spriteOffsetUBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> _spriteOffsetVBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> _spriteSizeUBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> _spriteSizeVBuffer;

		public override Vector3 localBoundingSphereCenter {
			get { return Vector3.Zero; }
		}

		public override float localBoundingSphereRadius {
			get { return particleSystem.radius; }
		}

        public SSInstancedMeshRenderer (SSParticleSystem ps, 
										BufferUsageHint hint = BufferUsageHint.StreamDraw)
        {
            particleSystem = ps;
			_posBuffer = new SSAttributeBuffer<SSAttributeVec3> (hint);
			_orientationXYBuffer = new SSAttributeBuffer<SSAttributeVec2> (hint);
			_orientationZBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
			_masterScaleBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
			_componentScaleXYBuffer = new SSAttributeBuffer<SSAttributeVec2> (hint);
			_componentScaleZBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
			_colorBuffer = new SSAttributeBuffer<SSAttributeColor> (hint);

			//m_spriteIndexBuffer = new SSAttributeBuffer<SSAttributeByte> (hint);
			_spriteOffsetUBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
			_spriteOffsetVBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
			_spriteSizeUBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
			_spriteSizeVBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
        }

		public SSInstancedMeshRenderer (SSParticleSystem ps, 
			ISSInstancable mesh = null,
			BufferUsageHint hint = BufferUsageHint.StreamDraw)
			: this(ps, hint)
		{
			this.mesh = mesh;
		}

        public override void Render (ref SSRenderConfig renderConfig)
        {
			Matrix4 modelView = this.worldMat * renderConfig.invCameraViewMat;

			// allow particle system to react to camera/worldview
			particleSystem.updateCamera (ref modelView, ref renderConfig.projectionMatrix);

			// do we have anything to draw?
			if (particleSystem.activeBlockLength <= 0) return;

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
				if (!globalBillboarding && base.alphaBlendingEnabled) {
					// Must be called before updating buffers
					particleSystem.sortByDepth (ref modelView);

					// Fixes flicker issues for particles with "fighting" view depth values
					// Also assumes the particle system is the last to be drawn in a scene
					GL.DepthFunc (DepthFunction.Lequal);
				}
				if (depthRead) {
					GL.Enable (EnableCap.DepthTest);
				} else {
					GL.Disable (EnableCap.DepthTest);
				}
				GL.DepthMask (depthWrite);

				// texture binding setup
                renderConfig.InstanceShader.Activate();
				renderConfig.InstanceShader.UniObjectWorldTransform = this.worldMat;
				if (base.textureMaterial != null) {
					renderConfig.InstanceShader.SetupTextures (base.textureMaterial);
				}
			}

			if (globalBillboarding) {
				// Setup "global" billboarding. (entire particle system is rendered as a camera-facing
				// billboard and will show the same position of particles from all angles)
				modelView = OpenTKHelper.BillboardMatrix (ref modelView);
				GL.MatrixMode (MatrixMode.Modelview);
				GL.LoadMatrix (ref modelView);
			}

			instanceShader.Activate ();

            // prepare attribute arrays for draw
            GL.PushClientAttrib(ClientAttribMask.ClientAllAttribBits);
            prepareAttribute(_posBuffer, instanceShader.AttrInstancePos, 
				particleSystem.positions);
			prepareAttribute(_orientationXYBuffer, instanceShader.AttrInstanceOrientationXY,
				particleSystem.orientationsXY);
			prepareAttribute(_orientationZBuffer, instanceShader.AttrInstanceOrientationZ, 
				particleSystem.orientationsZ);
            prepareAttribute(_masterScaleBuffer, instanceShader.AttrInstanceMasterScale, 
				particleSystem.masterScales);
			prepareAttribute(_componentScaleXYBuffer, instanceShader.AttrInstanceComponentScaleXY, 
				particleSystem.componentScalesXY);
			prepareAttribute(_componentScaleZBuffer, instanceShader.AttrInstanceComponentScaleZ,
				particleSystem.componentScalesZ);
            prepareAttribute(_colorBuffer, instanceShader.AttrInstanceColor, 
				particleSystem.colors);

			//prepareAttribute(m_spriteIndexBuffer, instanceShader.AttrInstanceSpriteIndex, m_ps.SpriteIndices);
            prepareAttribute(_spriteOffsetUBuffer, instanceShader.AttrInstanceSpriteOffsetU, 
				particleSystem.spriteOffsetsU);
            prepareAttribute(_spriteOffsetVBuffer, instanceShader.AttrInstanceSpriteOffsetV, 
				particleSystem.SpriteOffsetsV);
            prepareAttribute(_spriteSizeUBuffer, instanceShader.AttrInstanceSpriteSizeU, 
				particleSystem.SpriteSizesU);
            prepareAttribute(_spriteSizeVBuffer, instanceShader.AttrInstanceSpriteSizeV, 
				particleSystem.SpriteSizesV);

            // do the draw
            mesh.RenderInstanced(ref renderConfig, particleSystem.activeBlockLength, PrimitiveType.Triangles);
             
            GL.PopClientAttrib();
            //this.boundingSphere.Render(ref renderConfig);
        }

        
		void prepareAttribute<AB, A>(AB attrBuff, int attrLoc, A[] array) 
            where A : struct, ISSAttributeLayout 
            where AB : SSAttributeBuffer<A>
        {
            int numActive = particleSystem.activeBlockLength;
            int numInstancesPerValue = array.Length < numActive ? numActive : 1;
			attrBuff.PrepareAttributeAndUpdate(attrLoc, numInstancesPerValue, array);
        }

        public override void Update (float fElapsedMS)
        {
            if (simulateOnUpdate) {
                particleSystem.simulate(fElapsedMS);
            }
        }

		#if true
		public override bool PreciseIntersect(ref SSRay worldSpaceRay, ref float distanceAlongRay) {
			// for now, particle systems don't intersect with anything
			// TODO: figure out how to do this.
			return false;
		}
        #endif
    }
}

