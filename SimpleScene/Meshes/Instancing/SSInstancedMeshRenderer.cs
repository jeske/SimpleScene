using System;
using System.Collections.Generic;
using System.Drawing; // for RectangleF
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

    /// <summary>
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
        public bool simulateOnRender = false;
        public bool useBVHForIntersections = false;
        public RenderMode renderMode = RenderMode.Auto;
        /// <summary>
        /// When in the "auto" render mode, how many particles before switching to
        /// the on-GPU instancing?
        /// </summary>
        public int autoRenderModeThreshold = 30;

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
			get { return instanceData.center; }
		}

		public override float localBoundingSphereRadius {
			get { return instanceData.radius; }
		}

        public SSInstancedMeshRenderer (SSInstancesData ps, 
										BufferUsageHint hint = BufferUsageHint.StreamDraw)
        {
            instanceData = ps;
            _initAttributeBuffers(hint);

            // Fixes flicker issues for particles with "fighting" view depth values
            this.renderState.depthFunc = DepthFunction.Lequal;

            // In many situations selecting instances is computationally intensive. 
            // Turn it off and let users customize
            this.selectable = false;

			this.preRenderHook += 
				(obj, rc) => { if (this.simulateOnRender) { instanceData.update (rc.timeElapsedS); } };
			// TODO: unhook safely
        }

		public SSInstancedMeshRenderer (SSInstancesData ps, 
			ISSInstancable mesh = null,
			BufferUsageHint hint = BufferUsageHint.StreamDraw)
			: this(ps, hint)
		{
			this.mesh = mesh;
		}

        // TODO interface or abstract classify
        protected virtual void _initAttributeBuffers(BufferUsageHint hint)
        {
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

        public override void Render (SSRenderConfig renderConfig)
        {
            // allow particle system to react to camera/world mat changes (even if there are no particles)
            instanceData.updateCamera (ref this.worldMat, 
                ref renderConfig.invCameraViewMatrix, ref renderConfig.projectionMatrix);

            // do we have anything to draw?
            if (instanceData.numElements <= 0) return;

            if (!renderConfig.drawingShadowMap && 
                !renderState.doBillboarding && base.alphaBlendingEnabled) {
                // Must be called before updating buffers
                instanceData.sortByDepth (ref renderConfig.invCameraViewMatrix);
            }

            base.Render(renderConfig);

            if (renderMode == RenderMode.GpuInstancing
            || (renderMode == RenderMode.Auto && instanceData.numElements > autoRenderModeThreshold)) {
                _renderWithGPUInstancing(renderConfig);
            } else {
                _renderWithCPUIterations(renderConfig); 
            }
        }

        public override void Update (float fElapsedSecs)
		{
			if (simulateOnUpdate) {
				float prevRadius = instanceData.radius;
				instanceData.update (fElapsedSecs);
				if (instanceData.radius != prevRadius) {
					NotifyPositionOrSizeChanged ();
				}
			} 
		}

        protected virtual void _prepareInstanceShader(SSRenderConfig renderConfig)
        {
            ISSInstancableShaderProgram instanceShader;

            if (renderConfig.drawingShadowMap && renderConfig.drawingPssm) {
                renderConfig.instancePssmShader.Activate ();
                renderConfig.instancePssmShader.UniObjectWorldTransform = this.worldMat;
                instanceShader = renderConfig.instancePssmShader;
            } else {
                // texture binding and world mat setup
                renderConfig.instanceShader.Activate ();
                base.setDefaultShaderState(renderConfig.instanceShader, renderConfig);
                instanceShader = renderConfig.instanceShader;
            }

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
        }

        protected void _renderWithGPUInstancing(SSRenderConfig renderConfig)
        {
            // prepare attribute arrays for draw
            GL.PushClientAttrib(ClientAttribMask.ClientAllAttribBits);

            _prepareInstanceShader(renderConfig);

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

        protected virtual void _renderWithCPUIterations(SSRenderConfig renderConfig)
        {
            var mainShader = renderConfig.mainShader;
            mainShader.Activate();
            var modelViewMat = this.worldMat * renderConfig.invCameraViewMatrix;
            var mvOrient = modelViewMat.ExtractRotation(false);
            mvOrient.Xyz *= -1f;
            var mvOrientInverseMat = Matrix4.CreateFromQuaternion(mvOrient);

            for (int i = 0; i < instanceData.activeBlockLength; i++) {
                if (!instanceData.isValid(i))
                    continue;

                RectangleF spriteRect = instanceData.readRect(i);
                mainShader.UniSpriteOffsetAndSize(spriteRect.X, spriteRect.Y, 
                    spriteRect.Width, spriteRect.Height);

                Matrix4 mat = _instanceMat(i, ref mvOrientInverseMat);
                mat *= modelViewMat;
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadMatrix(ref mat);

                var color = instanceData.readColor(i);
                GL.Color4(color);

                mesh.drawSingle(renderConfig, this.primType);
            }
        }

        protected Matrix4 _instanceMat(int i, ref Matrix4 mvInverseOrient)
        {
            var pos = instanceData.readPosition(i);
            var componentScale = instanceData.readComponentScale(i);
            var scale = componentScale * instanceData.readMasterScale(i);
            var ori = instanceData.readOrientation(i);

            var instanceMat = Matrix4.CreateScale(scale);
            if (float.IsNaN(ori.X) || float.IsNaN(ori.Y)) { 
                // per-instance billboarding
                instanceMat *= Matrix4.CreateRotationZ(ori.Z) 
                             * mvInverseOrient;
            } else { 
                // no billboarding
                instanceMat *= Matrix4.CreateRotationX(ori.X)
                             * Matrix4.CreateRotationY(ori.Y)
                             * Matrix4.CreateRotationZ(ori.Z);
            }               
            instanceMat *= Matrix4.CreateTranslation(pos);
            return instanceMat;
        }

		public override bool PreciseIntersect(ref SSRay worldSpaceRay, ref float distanceAlongRay) 
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
            var pos = instanceData.readPosition(i);
            var masterScale = instanceData.readMasterScale(i);
            var scale = instanceData.readComponentScale(i) * masterScale;
            if (abstrMesh == null) {
                // no way to be any more precise except hitting a generic sphere
                float radius = Math.Max(scale.Z, Math.Max(scale.X, scale.Y));
                var sphere = new SSSphere (pos, radius);
                return sphere.IntersectsRay(ref localRay, out localContact);
            } else {
                // When using SSAbstractMesh we can invoke its preciseIntersect()
                Matrix4 instanceMat = Matrix4.CreateScale(scale) * Matrix4.CreateTranslation(pos);

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
                return _instanceData.readPosition(i);
            }

            public float radius(int i)
            {
                var masterScale = _instanceData.readMasterScale(i);
                var componentScale = _instanceData.readComponentScale(i);
                return masterScale * Math.Max(componentScale.Z, 
                    Math.Max(componentScale.X, componentScale.Y));
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

