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
		public event SSObject.ChangedEventHandler OnMeshChanged;

		public virtual bool alphaBlendingEnabled { get; set; }
		public virtual SSTextureMaterial textureMaterial { get; set; }

		public delegate bool traverseFn<T>(T state, Vector3 V1, Vector3 V2, Vector3 V3);

		public virtual void RenderMesh (ref SSRenderConfig renderConfig)
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

			if (alphaBlendingEnabled) {
				GL.Enable (EnableCap.Blend);
				GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			}
		}

        public virtual float Radius()
        {
            return 1f;
        }

		public virtual Vector3 Center()
		{
			return Vector3.Zero;
		}

        public virtual bool TraverseTriangles<T>(T state, traverseFn<T> fn) 
        {
            return true;
        }

		public bool TraverseTriangles(traverseFn<Object> fn) 
        {
			return this.TraverseTriangles<Object>(new Object(), fn);
		}

		public virtual void Update(float timeElapsed) { }

		protected void MeshChanged()
		{
			if (OnMeshChanged != null) {
				OnMeshChanged (null);
			}
		}
	}
}

