using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	/// <summary>
	/// Manages skeletal runtime hierarchy and animation channels.
	/// Invokes draw on render submeshes.
	/// </summary>
	public class SSSkeletalRenderMesh : SSAbstractMesh, ISSInstancable
	{
		// TODO Fix multiple objects updating a single mesh twice

		public float TimeScale = 1f;

		protected readonly List<RenderSubMesh> _renderSubMeshes = new List<RenderSubMesh> ();
		protected readonly Dictionary<int, SSSkeletalAnimationChannelRuntime> _chanRuntimes
			= new Dictionary<int, SSSkeletalAnimationChannelRuntime>();
		protected readonly List<SSSkeletalAnimationStateMachineRuntime> _animStateMachines
			= new List<SSSkeletalAnimationStateMachineRuntime> ();
		protected readonly SSSkeletalHierarchyRuntime _hierarchy;

		protected SSSphere _boundingSphere;

		public override bool alphaBlendingEnabled {
			get { return base.alphaBlendingEnabled; }
			set {
				base.alphaBlendingEnabled = value;
				if (_renderSubMeshes != null) {
					foreach (var child in _renderSubMeshes) {
						child.alphaBlendingEnabled = value;
					}
				}
			}
		}

		public SSSkeletalRenderMesh(SSSkeletalMesh[] subMeshArray)
		{
			_hierarchy = new SSSkeletalHierarchyRuntime (subMeshArray [0].Joints);
			foreach (var mesh in subMeshArray) {
				AttachMesh (mesh);
			}
		}

		public SSSkeletalRenderMesh(SSSkeletalMesh mesh) 
		{
			_hierarchy = new SSSkeletalHierarchyRuntime (mesh.Joints);
			AttachMesh (mesh);
		}


		public void AddChannel(int channelId, params int[] topLevelActiveJointIds)
		{
			if (channelId < -1) {
				string errMsg = "channel id must be >= 0";
				System.Console.WriteLine (channelId);
				throw new Exception (errMsg);
			}

			foreach (int cid in topLevelActiveJointIds) {
				if (cid == -1) { // means include all
					topLevelActiveJointIds = _hierarchy.TopLevelJoints;
					break;
				}
			}
			var channel = new SSSkeletalAnimationChannelRuntime (channelId, topLevelActiveJointIds);
			_chanRuntimes.Add (channelId, channel);
		}

		public void AddChannel(int channelId, params string[] topLevelActiveJointNames)
		{
			int[] topLevelActiveJointsIds = new int[topLevelActiveJointNames.Length];
			for (int n = 0; n < topLevelActiveJointsIds.Length; ++n) {
				topLevelActiveJointsIds [n] = _hierarchy.JointIndex (topLevelActiveJointNames [n]);
			}
			AddChannel (channelId, topLevelActiveJointsIds);
		}

		/// <summary>
		/// This can be used to play once or loop an animation.
		/// For a more sophisticated control use AddStateMachine()
		/// </summary>
		public void PlayAnimation(int channelId, SSSkeletalAnimation anim,
								  bool repeat = true, float fadeInTime = 0f, bool interChannelFade = false)
		{
			if (_animStateMachines.Count > 0) {
				var errMsg = "do not use PlayAnimation() when the mesh is already controlled by state machines";
				System.Console.WriteLine (errMsg);
				throw new Exception (errMsg);
			}
			_hierarchy.VerifyAnimation (anim);
			var channel = _chanRuntimes [channelId];
			channel.PlayAnimation (anim, repeat, fadeInTime, interChannelFade);
		}

		/// <summary>
		/// Adds the state machine that will control the mesh.
		/// </summary>
		/// <param name="stateMachine">
		/// A runtime state machine component that can be used to trigger animation state transitions on demand,
		/// from keyboard etc.
		/// </param>
		public SSSkeletalAnimationStateMachineRuntime AddStateMachine(
			SSSkeletalAnimationStateMachine stateMachine)
		{
			var newSMRuntime = new SSSkeletalAnimationStateMachineRuntime (stateMachine, _chanRuntimes);
			_animStateMachines.Add (newSMRuntime);
			return newSMRuntime;
		}

		/// <summary>
		/// Adds a sumbesh that will share the existing joint hierarchy
		/// </summary>
		public void AttachMesh(SSSkeletalMesh mesh)
		{
			var newRender = new RenderSubMesh (mesh, _hierarchy);
			newRender.alphaBlendingEnabled = this.alphaBlendingEnabled;
			_renderSubMeshes.Add (newRender);
		}

		public override void Update(float elapsedS)
		{
			elapsedS *= TimeScale;
			foreach (var channel in _chanRuntimes.Values) {
				channel.Update (elapsedS);
			}
			// do automated state transitions immediately after channel updates to avoid
			// "gaps" (periods of time with undefined animation) when using instant transitions
			foreach (var sm in _animStateMachines) {
				sm.TriggerAutomaticTransitions ();
			}
		}

		public override void RenderMesh (ref SSRenderConfig renderConfig)
		{
			// apply animation channels
			var channels = new List<SSSkeletalAnimationChannelRuntime> ();
			channels.AddRange (_chanRuntimes.Values);
			_hierarchy.ApplyAnimationChannels (channels);

			SSAABB totalAABB = new SSAABB (float.PositiveInfinity, float.NegativeInfinity);
			foreach (var sub in _renderSubMeshes) {
				SSAABB aabb = sub.ComputeVertices ();
				sub.RenderMesh (ref renderConfig);
				totalAABB.ExpandBy (aabb);
			}
			_boundingSphere = totalAABB.ToSphere ();
			MeshChanged ();
		}

		public void RenderInstanced(ref SSRenderConfig cfg, int instanceCount, PrimitiveType primType) 
		{
			foreach (var sub in _renderSubMeshes) {
				sub.RenderInstanced (ref cfg, instanceCount, primType);
			}
		}

		public override bool TraverseTriangles<T> (T state, traverseFn<T> fn)
		{
			foreach (var s in _renderSubMeshes) {
				bool finished = s.TraverseTriangles (state, fn);
				if (finished) {
					return true;
				}
			}
			return false;
		}

		public override Vector3 Center ()
		{
			return _boundingSphere.center;
		}

		public override float Radius ()
		{
			return _boundingSphere.radius;
		}

		// *****************************

		/// <summary>
		/// Draws the submeshes
		/// </summary>
		protected class RenderSubMesh : SSIndexedMesh<SSVertex_PosNormTex>
		{
			// TODO Detach mesh
			protected readonly SSSkeletalMeshRuntime m_runtimeMesh;
			protected readonly SSVertex_PosNormTex[] m_vertices;

			public RenderSubMesh (SSSkeletalMesh skeletalMesh, SSSkeletalHierarchyRuntime hierarchy=null)
				: base(null, skeletalMesh.TriangleIndices)
			{
				m_runtimeMesh = new SSSkeletalMeshRuntime(skeletalMesh, hierarchy);

				m_vertices = new SSVertex_PosNormTex[m_runtimeMesh.NumVertices];
				for (int v = 0; v < m_runtimeMesh.NumVertices; ++v) {
					m_vertices [v].TexCoord = m_runtimeMesh.TextureCoords (v);
					m_vertices [v].Normal = m_runtimeMesh.BindPoseNormal(v);
				}
				ComputeVertices ();

				string matString = skeletalMesh.MaterialShaderString;
				if (matString != null && matString.Length > 0) {
					base.textureMaterial 
					= SSTextureMaterial.FromMaterialString (skeletalMesh.AssetContext, matString);
				}
			}

			public override void RenderMesh(ref SSRenderConfig renderConfig)
			{
				base.RenderMesh (ref renderConfig);

				// debugging vertex normals... 
				#if false
				{
				// do not change the order!!
				renderFaceNormals();               // these are correct..
				renderFaceAveragedVertexNormals(); // these are correct..                
				// renderBindPoseVertexNormals ();
				renderAnimatedVertexNormals();     // these are currently WRONG
				}
				#endif

				#if false
				SSShaderProgram.DeactivateAll ();
				// bounding box debugging
				GL.PolygonMode(MaterialFace.Front, PolygonMode.Line);
				GL.Disable (EnableCap.Texture2D);
				GL.Translate (aabb.Center ());
				GL.Scale (aabb.Diff ());
				GL.Color4 (1f, 0f, 0f, 0.1f);
				SSTexturedCube.Instance.DrawArrays (ref renderConfig, PrimitiveType.Triangles);
				GL.PolygonMode(MaterialFace.Front, PolygonMode.Fill);
				#endif
			}

			public override bool TraverseTriangles<T>(T state, traverseFn<T> fn) {
				for(int idx=0; idx < m_runtimeMesh.Indices.Length; idx+=3) {
					var v1 = m_runtimeMesh.ComputeVertexPos (m_runtimeMesh.Indices[idx]);
					var v2 = m_runtimeMesh.ComputeVertexPos (m_runtimeMesh.Indices[idx+1]);
					var v3 = m_runtimeMesh.ComputeVertexPos (m_runtimeMesh.Indices[idx+2]);
					bool finished = fn(state, v1, v2, v3);
					if (finished) { 
						return true;
					}
				}
				return false;
			}

			/// <summary>
			/// Computes vertex positions and normals (based on the state of runtime joint hierarchy).
			/// Updates VBO with the result.
			/// </summary>
			/// <returns>AABB of the vertices.</returns>
			public SSAABB ComputeVertices()
			{
				SSAABB aabb= new SSAABB (float.PositiveInfinity, float.NegativeInfinity);
				for (int v = 0; v < m_runtimeMesh.NumVertices; ++v) {
					// position
					Vector3 pos = m_runtimeMesh.ComputeVertexPos (v);
					m_vertices [v].Position = pos;
					aabb.UpdateMin (pos);
					aabb.UpdateMax (pos);
					// normal
					m_vertices [v].Normal = m_runtimeMesh.ComputeVertexNormal (v);
				}
				m_vbo.UpdateBufferData (m_vertices);
				return aabb;
			}


			private void renderFaceNormals() 
			{
				SSShaderProgram.DeactivateAll();
				GL.Color4(Color4.Green);
				for (int i=0;i<m_runtimeMesh.NumTriangles;i++) {
					int baseIdx = i * 3;                
					Vector3 p0 = m_runtimeMesh.ComputeVertexPosFromTriIndex(baseIdx);
					Vector3 p1 = m_runtimeMesh.ComputeVertexPosFromTriIndex(baseIdx + 1);
					Vector3 p2 = m_runtimeMesh.ComputeVertexPosFromTriIndex(baseIdx + 2);

					Vector3 face_center = (p0 + p1 + p2) / 3.0f;
					Vector3 face_normal = Vector3.Cross(p1 - p0, p2 - p0).Normalized();

					GL.Begin(PrimitiveType.Lines);
					GL.Vertex3(face_center);
					GL.Vertex3(face_center + face_normal * 0.2f);
					GL.End();                
				}
			}

			private void renderAnimatedVertexNormals() {
				SSShaderProgram.DeactivateAll();
				GL.Color4(Color4.Magenta);
				for (int v = 0; v < m_vertices.Length; ++v) {                

					GL.Begin(PrimitiveType.Lines);
					GL.Vertex3(m_vertices[v].Position);
					GL.Vertex3(m_vertices[v].Position + m_vertices[v].Normal * 0.2f);
					GL.End();
				}
			}

			private void renderBindPoseVertexNormals()
			{
				SSShaderProgram.DeactivateAll ();
				GL.Color4 (Color4.White);
				for (int v = 0; v < m_vertices.Length; ++v) {                
					GL.Begin (PrimitiveType.Lines);
					GL.Vertex3 (m_vertices [v].Position);
					GL.Vertex3 (m_vertices [v].Position + m_runtimeMesh.BindPoseNormal(v) * 0.3f); 
					GL.End ();
				}
			}

			public void renderFaceAveragedVertexNormals() {
				Vector3[] perVertexNormals = new Vector3[m_runtimeMesh.NumVertices];

				for (int i = 0; i < m_runtimeMesh.NumTriangles; i++) {
					int baseIdx = i * 3;
					Vector3 p0 = m_runtimeMesh.ComputeVertexPosFromTriIndex(baseIdx);
					Vector3 p1 = m_runtimeMesh.ComputeVertexPosFromTriIndex(baseIdx + 1);
					Vector3 p2 = m_runtimeMesh.ComputeVertexPosFromTriIndex(baseIdx + 2);

					Vector3 face_normal = Vector3.Cross(p1 - p0, p2 - p0).Normalized();

					int v0 = m_runtimeMesh.Indices[baseIdx];
					int v1 = m_runtimeMesh.Indices[baseIdx + 1];
					int v2 = m_runtimeMesh.Indices[baseIdx + 2];

					perVertexNormals[v0] += face_normal;
					perVertexNormals[v1] += face_normal;
					perVertexNormals[v2] += face_normal;

				}

				// render face averaged vertex normals

				SSShaderProgram.DeactivateAll();
				GL.Color4(Color4.Yellow);
				for (int v=0;v<perVertexNormals.Length;v++) {
					GL.Begin(PrimitiveType.Lines);
					GL.Vertex3(m_runtimeMesh.ComputeVertexPos(v));
					GL.Vertex3(m_runtimeMesh.ComputeVertexPos(v) + perVertexNormals[v].Normalized() * 0.5f);
					GL.End();
				}
			}
		}
	};


}

