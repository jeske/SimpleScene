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
		void renderInstanced(SSRenderConfig renderConfig, int instanceCount, PrimitiveType primType);
    }

	public abstract class SSInstancesData
	{
		public abstract int capacity { get; }
		public abstract int activeBlockLength { get; }
		public abstract float radius { get; }

		public abstract SSAttributeVec3[] positions { get; }
		public abstract SSAttributeVec2[] orientationsXY { get; }
		public abstract SSAttributeFloat[] orientationsZ { get; }
		public abstract SSAttributeColor[] colors { get; }
		public abstract SSAttributeFloat[] masterScales { get; }
		public abstract SSAttributeVec2[] componentScalesXY { get; }
		public abstract SSAttributeFloat[] componentScalesZ { get; }
		public abstract SSAttributeFloat[] spriteOffsetsU { get; }
		public abstract SSAttributeFloat[] spriteOffsetsV { get; }
		public abstract SSAttributeFloat[] spriteSizesU { get; }
		public abstract SSAttributeFloat[] spriteSizesV { get; }

		public virtual void sortByDepth(ref Matrix4 viewMatrix) { }
		public virtual void update(float elapsedS) { }
        public virtual void updateCamera(ref Matrix4 model, ref Matrix4 view, 
                                         ref Matrix4 projection) { }
	}

    /// <summary>
    /// Renders particle system with attribute buffers and an ISSInstancable object
    /// </summary>
    public class SSInstancedMeshRenderer : SSObject
    {
        // TODO Draw any ibo/vbo mesh

        public const BufferUsageHint _defaultUsageHint = BufferUsageHint.StreamDraw;

		public SSInstancesData instanceData;

        public bool simulateOnUpdate = true;
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
			get { return instanceData.radius; }
		}

        public SSInstancedMeshRenderer (SSInstancesData ps, 
										BufferUsageHint hint = BufferUsageHint.StreamDraw)
        {
            instanceData = ps;
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

            // Fixes flicker issues for particles with "fighting" view depth values
            this.renderState.depthFunc = DepthFunction.Lequal;
        }

		public SSInstancedMeshRenderer (SSInstancesData ps, 
			ISSInstancable mesh = null,
			BufferUsageHint hint = BufferUsageHint.StreamDraw)
			: this(ps, hint)
		{
			this.mesh = mesh;
		}

        public override void Render (SSRenderConfig renderConfig)
        {
			Matrix4 modelView = this.worldMat * renderConfig.invCameraViewMatrix;

			// allow particle system to react to camera/worldview
            instanceData.updateCamera (ref this.worldMat, ref renderConfig.invCameraViewMatrix,
                                       ref renderConfig.projectionMatrix);

			// do we have anything to draw?
			if (instanceData.activeBlockLength <= 0) return;

            base.Render(renderConfig);

			// select either instance shader or instance pssm shader
			ISSInstancableShaderProgram instanceShader = renderConfig.instanceShader;

			if (renderConfig.drawingShadowMap) {
				if (renderConfig.drawingPssm) {
					renderConfig.instancePssmShader.Activate ();
					renderConfig.instancePssmShader.UniObjectWorldTransform = this.worldMat;
					instanceShader = renderConfig.instancePssmShader;
				}
			} else {
                if (!renderState.doBillboarding && base.alphaBlendingEnabled) {
					// Must be called before updating buffers
					instanceData.sortByDepth (ref modelView);
				}

				// texture binding setup
                renderConfig.instanceShader.Activate();
				renderConfig.instanceShader.UniObjectWorldTransform = this.worldMat;
				if (base.textureMaterial != null) {
					renderConfig.instanceShader.SetupTextures (base.textureMaterial);
				}
			}

			instanceShader.Activate ();

            // prepare attribute arrays for draw
            GL.PushClientAttrib(ClientAttribMask.ClientAllAttribBits);
            prepareAttribute(_posBuffer, instanceShader.AttrInstancePos, 
				instanceData.positions);
			prepareAttribute(_orientationXYBuffer, instanceShader.AttrInstanceOrientationXY,
				instanceData.orientationsXY);
			prepareAttribute(_orientationZBuffer, instanceShader.AttrInstanceOrientationZ, 
				instanceData.orientationsZ);
            prepareAttribute(_masterScaleBuffer, instanceShader.AttrInstanceMasterScale, 
				instanceData.masterScales);
			prepareAttribute(_componentScaleXYBuffer, instanceShader.AttrInstanceComponentScaleXY, 
				instanceData.componentScalesXY);
			prepareAttribute(_componentScaleZBuffer, instanceShader.AttrInstanceComponentScaleZ,
				instanceData.componentScalesZ);
            prepareAttribute(_colorBuffer, instanceShader.AttrInstanceColor, 
				instanceData.colors);

			//prepareAttribute(m_spriteIndexBuffer, instanceShader.AttrInstanceSpriteIndex, m_ps.SpriteIndices);
            prepareAttribute(_spriteOffsetUBuffer, instanceShader.AttrInstanceSpriteOffsetU, 
				instanceData.spriteOffsetsU);
            prepareAttribute(_spriteOffsetVBuffer, instanceShader.AttrInstanceSpriteOffsetV, 
				instanceData.spriteOffsetsV);
            prepareAttribute(_spriteSizeUBuffer, instanceShader.AttrInstanceSpriteSizeU, 
				instanceData.spriteSizesU);
            prepareAttribute(_spriteSizeVBuffer, instanceShader.AttrInstanceSpriteSizeV, 
				instanceData.spriteSizesV);

            // do the draw
            mesh.renderInstanced(renderConfig, instanceData.activeBlockLength, PrimitiveType.Triangles);
             
            GL.PopClientAttrib();
            //this.boundingSphere.Render(ref renderConfig);
        }

        
		void prepareAttribute<AB, A>(AB attrBuff, int attrLoc, A[] array) 
            where A : struct, ISSAttributeLayout 
            where AB : SSAttributeBuffer<A>
        {
            int numActive = instanceData.activeBlockLength;
            int numInstancesPerValue = array.Length < numActive ? numActive : 1;
			attrBuff.PrepareAttributeAndUpdate(attrLoc, numInstancesPerValue, array);
        }

        public override void Update (float fElapsedMS)
        {
            if (simulateOnUpdate) {
				float prevRadius = instanceData.radius;
                instanceData.update(fElapsedMS);
				if (instanceData.radius != prevRadius) {
					NotifyPositionOrSizeChanged ();
				}
            }
        }

		#if true
		protected override bool PreciseIntersect(ref SSRay worldSpaceRay, ref float distanceAlongRay) {
			// for now, particle systems don't intersect with anything
			// TODO: figure out how to do this.
			return false;
		}
        #endif
    }
}

