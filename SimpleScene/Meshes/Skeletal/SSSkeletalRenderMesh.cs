using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	public class SSSkeletalRenderMesh : SSIndexedMesh<SSVertex_PosNormTex>
	{
		// TODO Textures per render mesh, extracted from SSSkeletalMesh
		// TODO Detach mesh

		protected readonly SSSkeletalMeshRuntime m_skeletalMesh;
		protected readonly SSVertex_PosNormTex[] m_vertices;
		protected Dictionary<int, SSSkeletalAnimationChannel> m_animChannels
			= new Dictionary<int, SSSkeletalAnimationChannel>();
		protected SSSphere m_boundingSphere;
		protected List<SSSkeletalRenderMesh> m_attachedSkeletalMeshes;

		public SSSkeletalRenderMesh (SSSkeletalMesh skeletalMesh, 
									 SSSkeletalJointRuntime[] sharedJoints = null)
			: base(null, skeletalMesh.TriangleIndices)
		{
			m_skeletalMesh = new SSSkeletalMeshRuntime(skeletalMesh, sharedJoints);
			m_vertices = new SSVertex_PosNormTex[m_skeletalMesh.NumVertices];
			for (int v = 0; v < m_skeletalMesh.NumVertices; ++v) {
				m_vertices [v].TexCoord = m_skeletalMesh.TextureCoords (v);
			}
			m_attachedSkeletalMeshes = new List<SSSkeletalRenderMesh> ();
			computeVertices ();
		}

		public SSSkeletalRenderMesh(SSSkeletalMesh[] skeletalMeshes) 
			: this(skeletalMeshes[0], null)
		{
			for (int i = 1; i < skeletalMeshes.Length; ++i) {
				AttachMesh (skeletalMeshes [i]);
			}
		}

		public void AddChannel(int channelId, params int[] topLevelActiveJointIds)
		{
			foreach (int cid in topLevelActiveJointIds) {
				if (cid == -1) { // means include all
					topLevelActiveJointIds = m_skeletalMesh.TopLevelJoints;
					break;
				}
			}
			var channel = new SSSkeletalAnimationChannel (topLevelActiveJointIds);
			m_animChannels.Add (channelId, channel);
		}

		public void AddChannel(int channelId, params string[] topLevelActiveJointNames)
		{
			int[] topLevelActiveJointsIds = new int[topLevelActiveJointNames.Length];
			for (int n = 0; n < topLevelActiveJointsIds.Length; ++n) {
				topLevelActiveJointsIds [n] = m_skeletalMesh.JointIndex (topLevelActiveJointNames [n]);
			}
			AddChannel (channelId, topLevelActiveJointsIds);
		}

		public void PlayAnimation(int channelId, SSSkeletalAnimation anim,
								  bool repeat, float transitionTime)
		{
			m_skeletalMesh.VerifyAnimation (anim);
			var channel = m_animChannels [channelId];
			channel.PlayAnimation (anim, repeat, transitionTime);
		}

		public void AttachMesh(SSSkeletalMesh mesh)
		{
			var newRender = new SSSkeletalRenderMesh (mesh, m_skeletalMesh.Joints);
			m_attachedSkeletalMeshes.Add (newRender);
		}

		public override void RenderMesh(ref SSRenderConfig renderConfig)
		{
			// apply animation channels
			var channels = new List<SSSkeletalAnimationChannel> ();
			channels.AddRange (m_animChannels.Values);
			m_skeletalMesh.ApplyAnimationChannels (channels);

			// compute vertex positions, normals
			computeVertices ();

			// render vertices + indices
			base.RenderMesh (ref renderConfig);
			foreach (SSSkeletalRenderMesh rm in m_attachedSkeletalMeshes) {
				rm.RenderMesh (ref renderConfig);
			}

			//renderNormals ();
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

		public override void Update(float elapsedS)
		{
			foreach (var channel in m_animChannels.Values) {
				channel.Update (elapsedS);
			}
		}

		private void computeVertices()
		{
			SSAABB aabb= new SSAABB (float.PositiveInfinity, float.NegativeInfinity);
			for (int v = 0; v < m_skeletalMesh.NumVertices; ++v) {
				// position
				Vector3 pos = m_skeletalMesh.ComputeVertexPos (v);
				m_vertices [v].Position = pos;
				aabb.UpdateMin (pos);
				aabb.UpdateMax (pos);
				// normal
				m_vertices [v].Normal = m_skeletalMesh.ComputeVertexNormal (v);
			}
			m_vbo.UpdateBufferData (m_vertices);
			m_boundingSphere = aabb.ToSphere ();
			MeshChanged ();
		}


		private void renderNormals()
		{
			SSShaderProgram.DeactivateAll ();
			GL.Color4 (Color4.Magenta);
			for (int v = 0; v < m_vertices.Length; ++v) {
				GL.Begin (PrimitiveType.Lines);
				GL.Vertex3 (m_vertices [v].Position);
				GL.Vertex3 (m_vertices [v].Position + m_vertices [v].Normal * 0.5f); 
				GL.End ();
			}
		}

		public override Vector3 Center ()
		{
			return m_boundingSphere.center;
		}

		public override float Radius ()
		{
			return m_boundingSphere.radius;
		}

		protected class AttachedObjectInfo
		{
			public SSObject AttachedObject;
			public int JointIdx;
			public Vector3 PositionOffset;
			public Quaternion OrientOffset;
			public bool ControlOrient;
		}
	}
}

