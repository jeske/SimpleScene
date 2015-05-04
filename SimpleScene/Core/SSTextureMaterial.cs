using System;
using System.Collections.Generic;

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
				SSAssetManager.Context[] ctxs = { ctx, SSAssetManager.Context.Root };
				var extensions = new List<string> (SSTexture.commonImageExtensions);
				extensions.Insert (0, ".mtl"); // check mtl first

				foreach (var context in ctxs) {
					// for each context (current vs root directory)...
					foreach (string extension in extensions) {
						// for each extension of interest...
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
				// try loading a material
				try {
					SSBlenderMTLInfo[] mtls = SSBlenderMTLInfo.ReadMTLs(existingCtx, existingFilename);
					if (mtls.Length < 0) {
						throw new Exception("No MTLs available in a file");
					}
					return SSTextureMaterial.FromBlenderMtl(existingCtx, mtls[0]);
				} catch {

					// try loading an image
					try {
						SSTexture diffTex = SSAssetManager.GetInstance<SSTextureWithAlpha> (existingCtx, existingFilename);
						return new SSTextureMaterial (diffTex);
					} catch {
					}
				}
			}

			string errMsg = "could not load texture material: " + materialString;
			System.Console.WriteLine (errMsg);
			throw new Exception (errMsg);
		}

		public static SSTextureMaterial FromBlenderMtl(SSAssetManager.Context ctx, SSBlenderMTLInfo mtl)
		{
			SSTexture diffuse = null, specular = null, ambient = null, bumpMap = null;
			if (mtl.diffuseTextureResourceName != null && mtl.diffuseTextureResourceName.Length > 0) {
				diffuse = SSAssetManager.GetInstance<SSTextureWithAlpha> (ctx, mtl.diffuseTextureResourceName);
			}
			if (mtl.specularTextureResourceName != null && mtl.specularTextureResourceName.Length > 0) {
				specular = SSAssetManager.GetInstance<SSTextureWithAlpha> (ctx, mtl.specularTextureResourceName);
			}
			if (mtl.ambientTextureResourceName != null && mtl.ambientTextureResourceName.Length > 0) {
				ambient = SSAssetManager.GetInstance<SSTextureWithAlpha> (ctx, mtl.ambientTextureResourceName);
			}
			if (mtl.bumpTextureResourceName != null && mtl.bumpTextureResourceName.Length > 0) {
				bumpMap = SSAssetManager.GetInstance<SSTextureWithAlpha> (ctx, mtl.bumpTextureResourceName);
			}

			return new SSTextureMaterial (diffuse, specular, ambient, bumpMap);
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

