using System;

namespace SimpleScene
{
	public class SSTextureMaterial
	{
		public SSTexture diffuseTex;
		public SSTexture specularTex;
		public SSTexture ambientTex;
		public SSTexture bumpMapTex;

		public SSTextureMaterial (SSTexture diffuse = null, SSTexture specular = null, 
							      SSTexture ambient = null, SSTexture bumpMap = null)
		{
			diffuseTex = diffuse;
			specularTex = specular;
			ambientTex = ambient;
			bumpMapTex = bumpMap;
		}
	}
}

