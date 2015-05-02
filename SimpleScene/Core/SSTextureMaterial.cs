using System;

namespace SimpleScene
{
	public class SSTextureMaterial
	{
		public SSTexture diffuseTex;
		public SSTexture specularTex;
		public SSTexture ambientTex;
		public SSTexture bumpMapTex;

		public static SSTextureMaterial FromMaterialString(SSAssetManager.Context ctx, string materialString)
		{
			string existingFilename = null;
			SSAssetManager.Context existingCtx = null;

			if (ctx != null && SSAssetManager.ResourceExists (ctx, materialString)) {
				existingCtx = ctx;
				existingFilename = materialString;
			} else if (SSAssetManager.ResourceExists (SSAssetManager.Context.Root, materialString)) {
				existingCtx = SSAssetManager.Context.Root;
				existingFilename = materialString;
			} else {
				// TODO try material first
				SSAssetManager.Context[] ctxs = { ctx, SSAssetManager.Context.Root };
				foreach (var context in ctxs) {
					foreach (string extension in SSTexture.commonImageExtensions) {
						string filename = materialString + extension;
						if (SSAssetManager.ResourceExists (context, filename)) {
							existingCtx = context;
							existingFilename = filename;
							break;
						}
					}
				}
			}

			if (existingFilename != null) {
				try {
					SSTexture diffTex = SSAssetManager.GetInstance<SSTextureWithAlpha>(existingCtx, existingFilename);
					return new SSTextureMaterial(diffTex);
				} catch { };
			}

			string errMsg = "could not load texture material: " + materialString;
			System.Console.WriteLine (errMsg);
			throw new Exception (errMsg);
		}

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

