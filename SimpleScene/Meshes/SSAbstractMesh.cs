// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene 
{
	public abstract class SSAbstractMesh 
	{
		public delegate void MeshPositionOrSizeChangedHandler();
		public event MeshPositionOrSizeChangedHandler OnMeshPositionOrSizeChanged = null;
		protected void NotifyMeshPositionOrSizeChanged() {
			if (OnMeshPositionOrSizeChanged != null) {
				OnMeshPositionOrSizeChanged ();
			}
		}

		public virtual bool alphaBlendingEnabled { get; set; }
		public virtual SSTextureMaterial textureMaterial { get; set; }

		public virtual float boundingSphereRadius { get; set; }
		public virtual Vector3 boundingSphereCenter	{ get; set; }

		/// <summary>
		/// Keeps track of where the Updates() are to be coming from to prevents redundant Update() calls 
		/// when the mesh is shared by multiple SSObjectMesh's
		/// </summary>
		public WeakReference updateSource = new WeakReference (null);

        public SSAbstractMesh()
		{
			alphaBlendingEnabled = false;
			boundingSphereRadius = 1f;
			boundingSphereCenter = Vector3.Zero;
		}

		public virtual void renderMesh (SSRenderConfig renderConfig)
		{
			if (!renderConfig.drawingShadowMap && textureMaterial != null) {
				if (renderConfig.ActiveDrawShader != null) {
					renderConfig.ActiveDrawShader.SetupTextures (textureMaterial);
				} else {
					GL.ActiveTexture(TextureUnit.Texture0);
					GL.Enable(EnableCap.Texture2D);
					if (textureMaterial.ambientTex != null || textureMaterial.diffuseTex != null) {
						// fall back onto the diffuse texture in the absence of ambient
						SSTexture tex = textureMaterial.ambientTex ?? textureMaterial.diffuseTex;
						GL.BindTexture(TextureTarget.Texture2D, tex.TextureID);
					} else {
						GL.BindTexture(TextureTarget.Texture2D, 0);
					}
				}
			}

            #if false
			if (alphaBlendingEnabled) {
				GL.Enable (EnableCap.Blend);
				GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			}
            #endif
		}

        public virtual bool preciseIntersect (ref SSRay localRay, out float localRayContact)
        {
            localRayContact = 0f;
            return true;
        }

		public virtual void update(float timeElapsed) { }
	}
}

