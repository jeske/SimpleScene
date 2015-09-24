using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using SimpleScene.Util.ssBVH;

namespace SimpleScene
{
    public interface ISSInstancable
    {
        /// <summary>
        /// Render a number of instances of the mesh. Attribute arrays must be prepared prior to use.
        /// </summary>
		void drawInstanced(SSRenderConfig renderConfig, int instanceCount, PrimitiveType primType);
        void drawSingle (SSRenderConfig renderConfig, PrimitiveType primType);
    }

	public abstract class SSInstancesData
	{
        public static Element readElement<Element>(Element[] array, int i)
        {
            return i >= array.Length ? array [0] : array [i];
        }

		public abstract int capacity { get; }
        public abstract int numElements { get; }
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
        public const BufferUsageHint _defaultUsageHint = BufferUsageHint.StreamDraw;

        public enum RenderMode { GpuInstancing, CpuFallback, Auto };

		public SSInstancesData instanceData;
        public ISSInstancable mesh;
        public PrimitiveType primType = PrimitiveType.Triangles;

        public bool simulateOnUpdate = true;
        public bool useBVHForIntersections = false;
        public RenderMode renderMode = RenderMode.Auto;
        /// <summary>
        /// When in the "auto" render mode, how many particles before switching to
        /// the on-GPU instancing?
        /// </summary>
        public int autoRenderModeThreshold = 100;

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

        protected SSInstanceBVH _bvh = null;

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

            // In many situations selecting instances is computationally intensive. 
            // Turn it off and let users customize
            this.selectable = false;
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

            if (!renderConfig.drawingShadowMap && 
                !renderState.doBillboarding && base.alphaBlendingEnabled) {
                // Must be called before updating buffers
                instanceData.sortByDepth (ref modelView);
            }

            if (renderMode == RenderMode.GpuInstancing
            || (renderMode == RenderMode.Auto && instanceData.numElements >= autoRenderModeThreshold)) {
                _renderWithGPUInstancing(renderConfig);
            } else {
                _renderWithCPUIterations(renderConfig); 
            }
        }

        protected void _renderWithCPUIterations(SSRenderConfig renderConfig)
        {
            var mainShader = renderConfig.mainShader;
            mainShader.Activate();

            for (int i = 0; i < instanceData.activeBlockLength; i++) {
                if (!instanceData.isValid(i))
                    continue;

                var spriteOffsetU = SSInstancesData.readElement(instanceData.spriteOffsetsU, i).Value;
                var spriteOffsetV = SSInstancesData.readElement(instanceData.spriteOffsetsV, i).Value;
                var spriteSizeU = SSInstancesData.readElement(instanceData.spriteSizesU, i).Value;
                var spriteSizeV = SSInstancesData.readElement(instanceData.spriteSizesV, i).Value;
                mainShader.UniSpriteOffsetAndSize(spriteOffsetU, spriteOffsetV, spriteSizeU, spriteSizeV);

                var color = SSInstancesData.readElement(instanceData.colors, i).Color;
                var oriXY = SSInstancesData.readElement(instanceData.orientationsXY, i).Value;

                Matrix4 mat = _instanceMat(i) * this.worldMat * renderConfig.invCameraViewMatrix;
                if (float.IsNaN(oriXY.X) || float.IsNaN(oriXY.Y)) { // per-instance billboarding
                    mat = OpenTKHelper.BillboardMatrix(ref mat);
                }
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadMatrix(ref mat);

                GL.Color4(Color4Helper.FromUInt32(color));

                mesh.drawSingle(renderConfig, this.primType);
            }
        }

        protected Matrix4 _instanceMat(int i)
        {
            var pos = SSInstancesData.readElement(instanceData.positions, i).Value;
            var componentScaleXY = SSInstancesData.readElement(instanceData.componentScalesXY, i).Value;
            var componentScaleZ = SSInstancesData.readElement(instanceData.componentScalesZ, i).Value;
            var scale = new Vector3 (componentScaleXY.X, componentScaleXY.Y, componentScaleZ)
                * SSInstancesData.readElement(instanceData.masterScales, i).Value;
            var oriXY = SSInstancesData.readElement(instanceData.orientationsXY, i).Value;
            var oriZ = SSInstancesData.readElement(instanceData.orientationsZ, i).Value;
            var instanceMat = Matrix4.CreateScale(scale);
            if (!float.IsNaN(oriXY.X) && !float.IsNaN(oriXY.Y)) { // Not NaN -> no billboarding
                instanceMat *= Matrix4.CreateRotationX(-oriXY.X) * Matrix4.CreateRotationY(-oriXY.Y);
            }
            instanceMat = instanceMat
                * Matrix4.CreateRotationZ(-oriZ)
                * Matrix4.CreateTranslation(pos);
            return instanceMat;

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
            mesh.drawInstanced(renderConfig, instanceData.activeBlockLength, this.primType);

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

		protected override bool PreciseIntersect(ref SSRay worldSpaceRay, out float distanceAlongRay) 
        {
            SSRay localRay = worldSpaceRay.Transformed(this.worldMat.Inverted());
            SSAbstractMesh abstrMesh = this.mesh as SSAbstractMesh;

            float nearestLocalRayContact = float.PositiveInfinity;

            if (useBVHForIntersections) {
                if (_bvh == null) {
                    _bvh = new SSInstanceBVH (this.instanceData);
                    for (int i = 0; i < instanceData.activeBlockLength; ++i) {
                        if (instanceData.isValid(i)) {
                            _bvh.addObject(i);
                        }
                    }
                }

                List<ssBVHNode<int>> nodesHit = _bvh.traverseRay(localRay);
                foreach (var node in nodesHit) {
                    if (!node.IsLeaf)
                        continue;
                    foreach (int i in node.gobjects) {
                        if (!instanceData.isValid(i)) continue;

                        float localContact;
                        if (_perInstanceIntersectionTest(abstrMesh, i, ref localRay, out localContact)) {
                            if (localContact < nearestLocalRayContact) {
                                nearestLocalRayContact = localContact;
                            }
                        }
                    }
                }
            } else {
                // no BVH is used
                for (int i = 0; i < instanceData.activeBlockLength; ++i) {
                    if (!instanceData.isValid(i)) continue;

                    float localContact;
                    if (_perInstanceIntersectionTest(abstrMesh, i, ref localRay, out localContact)) {
                        if (localContact < nearestLocalRayContact) {
                            nearestLocalRayContact = localContact;
                        }
                    }
                }
            }

            if (nearestLocalRayContact < float.PositiveInfinity) {
                Vector3 localContactPt = localRay.pos + nearestLocalRayContact * localRay.dir;
                Vector3 worldContactPt = Vector3.Transform(localContactPt, this.worldMat);
                distanceAlongRay = (worldContactPt - worldSpaceRay.pos).Length;
                return true;
            } else {
                distanceAlongRay = float.PositiveInfinity;
                return false;
            }
		}

        protected bool _perInstanceIntersectionTest(SSAbstractMesh abstrMesh, int i,
                                                    ref SSRay localRay, out float localContact)
        {
            var pos = SSInstancesData.readElement(instanceData.positions, i).Value;
            var masterScale = SSInstancesData.readElement(instanceData.masterScales, i).Value;
            var componentScaleXY = SSInstancesData.readElement(instanceData.componentScalesXY, i).Value;
            var componentScaleZ = SSInstancesData.readElement(instanceData.componentScalesZ, i).Value;
            if (abstrMesh == null) {
                // no way to be any more precise except hitting a generic sphere
                float radius = Math.Max(componentScaleZ, Math.Max(componentScaleXY.X, componentScaleXY.Y));
                var sphere = new SSSphere (pos, radius);
                return sphere.IntersectsRay(ref localRay, out localContact);
            } else {
                // When using SSAbstractMesh we can invoke its preciseIntersect()
                Matrix4 instanceMat = Matrix4.CreateScale(
                    masterScale * componentScaleXY.X,
                    masterScale * componentScaleXY.Y,
                    masterScale * componentScaleZ)
                    * Matrix4.CreateTranslation(pos);

                SSRay instanceRay = localRay.Transformed(instanceMat.Inverted());
                float instanceContact;
                if (abstrMesh.preciseIntersect(ref instanceRay, out instanceContact)) {
                    Vector3 instanceContactPt = instanceRay.pos + instanceContact * instanceRay.dir;
                    Vector3 localContactPt = Vector3.Transform(instanceContactPt, instanceMat);
                    localContact = (localContactPt - localRay.pos).Length;
                    return true;
                } else {
                    localContact = float.PositiveInfinity;
                    return false;
                }
            }
        }

        #if true
        public class SSInstanceBNHNodeAdaptor : SSBVHNodeAdaptor<int>
        {
            protected readonly SSInstancesData _instanceData;
            protected ssBVH<int> _bvh;
            protected Dictionary<int, ssBVHNode<int>> _indexToLeafMap
                = new Dictionary<int, ssBVHNode<int>> ();

            public SSInstanceBNHNodeAdaptor(SSInstancesData instanceData)
            {
                _instanceData = instanceData;
            }

            public void setBVH(ssBVH<int> bvh)
            {
                _bvh = bvh;
            }

            public ssBVH<int> BVH { get { return _bvh; } }

            public Vector3 objectpos(int i)
            {
                return SSInstancesData.readElement(_instanceData.positions, i).Value;
            }

            public float radius(int i)
            {
                var masterScale = SSInstancesData.readElement(_instanceData.masterScales, i).Value;
                var componentScaleXY = SSInstancesData.readElement(_instanceData.componentScalesXY, i).Value;
                var componentScaleZ = SSInstancesData.readElement(_instanceData.componentScalesZ, i).Value;
                return masterScale * Math.Max(componentScaleZ, Math.Max(componentScaleXY.X, componentScaleXY.Y));
            }

            public void checkMap(int i)
            {
                if (!_indexToLeafMap.ContainsKey(i)) {
                    throw new Exception ("missing map for a shuffled child");
                }
            }

            public void unmapObject(int i)
            {
                _indexToLeafMap.Remove(i);
            }

            public void mapObjectToBVHLeaf(int i, ssBVHNode<int> leaf)
            {
                _indexToLeafMap [i] = leaf;
            }

            public ssBVHNode<int> getLeaf(int i)
            {
                return _indexToLeafMap [i];
            }
        }

        public class SSInstanceBVH : ssBVH<int>
        {
            public SSInstanceBVH(SSInstancesData instanceData, int maxTrianglesPerLeaf=1)
                : base (new SSInstanceBNHNodeAdaptor(instanceData), new List<int>(), maxTrianglesPerLeaf)
            {
            }
        }
        #endif
    }
}

