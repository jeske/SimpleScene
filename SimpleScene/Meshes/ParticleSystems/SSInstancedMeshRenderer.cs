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
        void Render (SSRenderConfig renderConfig);
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

        public bool isValid(int slotIdx)
        {
            if (slotIdx >= positions.Length) {
                slotIdx = 0;
            }
            var pos = positions [slotIdx].Value; 
            return !float.IsNaN(pos.X) && !float.IsNaN(pos.Y) && !float.IsNaN(pos.Y);
        }
	}

    /// <summary>5
    /// Renders particle system with attribute buffers and an ISSInstancable object
    /// </summary>
    public class SSInstancedMeshRenderer : SSObject
    {
        // TODO Draw any ibo/vbo mesh

        public const BufferUsageHint _defaultUsageHint = BufferUsageHint.StreamDraw;

		public SSInstancesData instanceData;

        public bool simulateOnUpdate = true;
        public bool fallbackToCpu = false; // when true, draw using iteration with CPU (no GPU instancing)
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

            ISSInstancableShaderProgram instanceShader = renderConfig.instanceShader;

            if (!renderConfig.drawingShadowMap && 
                !renderState.doBillboarding && base.alphaBlendingEnabled) {
                // Must be called before updating buffers
                instanceData.sortByDepth (ref modelView);
            }

            if (this.fallbackToCpu) {
                _renderWithCPUIterations(renderConfig); 
            } else {
                _renderWithGPUInstancing(renderConfig);
            }
        }

        protected void _renderWithCPUIterations(SSRenderConfig renderConfig)
        {
            var mainShader = renderConfig.mainShader;
            mainShader.Activate();

            for (int i = 0; i < instanceData.activeBlockLength; i++) {
                if (!instanceData.isValid(i))
                    continue;

                var spriteOffsetU = _readElement(instanceData.spriteOffsetsU, i).Value;
                var spriteOffsetV = _readElement(instanceData.spriteOffsetsV, i).Value;
                var spriteSizeU = _readElement(instanceData.spriteSizesU, i).Value;
                var spriteSizeV = _readElement(instanceData.spriteSizesV, i).Value;
                mainShader.UniSpriteOffsetAndSize(spriteOffsetU, spriteOffsetV, spriteSizeU, spriteSizeV);

                var pos = _readElement(instanceData.positions, i).Value;
                var componentScaleXY = _readElement(instanceData.componentScalesXY, i).Value;
                var componentScaleZ = _readElement(instanceData.componentScalesZ, i).Value;
                var scale = new Vector3 (componentScaleXY.X, componentScaleXY.Y, componentScaleZ)
                            * _readElement(instanceData.masterScales, i).Value;
                var oriXY = _readElement(instanceData.orientationsXY, i).Value;
                var oriZ = _readElement(instanceData.orientationsZ, i).Value;
                var color = _readElement(instanceData.colors, i).Color;
                

                // TODO check consistency of orientations with the shader implementation
                var instanceMat = Matrix4.CreateScale(scale);
                if (!float.IsNaN(oriXY.X) && !float.IsNaN(oriXY.Y)) { // Not NaN -> no billboarding
                    instanceMat *= Matrix4.CreateRotationX(-oriXY.X) * Matrix4.CreateRotationY(-oriXY.Y);
                }
                instanceMat = instanceMat
                    * Matrix4.CreateRotationZ(-oriZ)
                    * Matrix4.CreateTranslation(pos)
                    * this.worldMat
                    * renderConfig.invCameraViewMatrix;
                if (float.IsNaN(oriXY.X) || float.IsNaN(oriXY.Y)) { // per-instance billboarding
                    instanceMat = OpenTKHelper.BillboardMatrix(ref instanceMat);
                }
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadMatrix(ref instanceMat);

                GL.Color4(Color4Helper.FromUInt32(color));

                mesh.Render(renderConfig);
            }
        }

        protected static Element _readElement<Element>(Element[] array, int i)
        {
            return i >= array.Length ? array [0] : array [i];
        }

        protected void _renderWithGPUInstancing(SSRenderConfig renderConfig)
        {
            // select either instance shader or instance pssm shader
            ISSInstancableShaderProgram instanceShader = renderConfig.instanceShader;

            // texture binding setup
            if (renderConfig.drawingShadowMap) {
                renderConfig.instancePssmShader.Activate ();
                renderConfig.instancePssmShader.UniObjectWorldTransform = this.worldMat;
                instanceShader = renderConfig.instancePssmShader;

            } else {
                renderConfig.instanceShader.Activate();
                renderConfig.instanceShader.UniObjectWorldTransform = this.worldMat;
                if (base.textureMaterial != null) {
                    renderConfig.instanceShader.SetupTextures(base.textureMaterial);
                }
            }

            instanceShader.Activate ();

            // prepare attribute arrays for draw
            GL.PushClientAttrib(ClientAttribMask.ClientAllAttribBits);
            _prepareAttribute(_posBuffer, instanceShader.AttrInstancePos, 
                instanceData.positions);
            _prepareAttribute(_orientationXYBuffer, instanceShader.AttrInstanceOrientationXY,
                instanceData.orientationsXY);
            _prepareAttribute(_orientationZBuffer, instanceShader.AttrInstanceOrientationZ, 
                instanceData.orientationsZ);
            _prepareAttribute(_masterScaleBuffer, instanceShader.AttrInstanceMasterScale, 
                instanceData.masterScales);
            _prepareAttribute(_componentScaleXYBuffer, instanceShader.AttrInstanceComponentScaleXY, 
                instanceData.componentScalesXY);
            _prepareAttribute(_componentScaleZBuffer, instanceShader.AttrInstanceComponentScaleZ,
                instanceData.componentScalesZ);
            _prepareAttribute(_colorBuffer, instanceShader.AttrInstanceColor, 
                instanceData.colors);

            //prepareAttribute(m_spriteIndexBuffer, instanceShader.AttrInstanceSpriteIndex, m_ps.SpriteIndices);
            _prepareAttribute(_spriteOffsetUBuffer, instanceShader.AttrInstanceSpriteOffsetU, 
                instanceData.spriteOffsetsU);
            _prepareAttribute(_spriteOffsetVBuffer, instanceShader.AttrInstanceSpriteOffsetV, 
                instanceData.spriteOffsetsV);
            _prepareAttribute(_spriteSizeUBuffer, instanceShader.AttrInstanceSpriteSizeU, 
                instanceData.spriteSizesU);
            _prepareAttribute(_spriteSizeVBuffer, instanceShader.AttrInstanceSpriteSizeV, 
                instanceData.spriteSizesV);

            // do the draw
            mesh.renderInstanced(renderConfig, instanceData.activeBlockLength, PrimitiveType.Triangles);

            GL.PopClientAttrib();
            //this.boundingSphere.Render(ref renderConfig);
        }
        
		protected void _prepareAttribute<AB, A>(AB attrBuff, int attrLoc, A[] array) 
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

