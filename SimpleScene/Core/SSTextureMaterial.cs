using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleScene
{
	public class SSTextureMaterial
	{
		public SSTexture diffuseTex;
		public SSTexture specularTex;
		public SSTexture ambientTex;
		public SSTexture bumpMapTex;

        public static SSTextureMaterial FromMaterialString(string basePath, string materialString)
		{
			string existingPath = null;

            string combined = Path.Combine(basePath, materialString);
			if (SSAssetManager.ResourceExists (combined)) {
                existingPath = combined;
			} else if (SSAssetManager.ResourceExists (materialString)) {
                existingPath = materialString;
			} else {
                string[] basePaths = { "", basePath }; // search in root as well as supplied base path
				var extensions = new List<string> (SSTexture.commonImageExtensions);
				extensions.Insert (0, ".mtl"); // check mtl first

                foreach (var bp in basePaths) {
					// for each context (current vs root directory)...
					foreach (string extension in extensions) {
						// for each extension of interest...
                        string fullPath = Path.Combine(bp, materialString + extension);
                        if (SSAssetManager.ResourceExists (fullPath)) {
                            existingPath = fullPath;
							break;
						}
					}
				}
			}

            if (existingPath != null) {
				// try loading a material
				try {
                    SSWavefrontMTLInfo[] mtls = SSWavefrontMTLInfo.ReadMTLs(existingPath);
					if (mtls.Length <= 0) {
						throw new Exception("No MTLs available in a file");
					}
                    string baseDir = Path.GetDirectoryName(existingPath);
                    return SSTextureMaterial.FromBlenderMtl(baseDir, mtls[0]);
				} catch {

					// try loading an image
					try {
                        SSTexture diffTex = SSAssetManager.GetInstance<SSTextureWithAlpha> (existingPath);
						return new SSTextureMaterial (diffTex);
					} catch {
					}
				}
			}

			string errMsg = "could not load texture material: " + materialString;
			System.Console.WriteLine (errMsg);
			throw new Exception (errMsg);
		}

        public static SSTextureMaterial FromBlenderMtl(string basePath, SSWavefrontMTLInfo mtl)
		{
			SSTexture diffuse = null, specular = null, ambient = null, bumpMap = null;
			if (mtl.diffuseTextureResourceName != null && mtl.diffuseTextureResourceName.Length > 0) {
                string path = Path.Combine(basePath, mtl.diffuseTextureResourceName);
				diffuse = SSAssetManager.GetInstance<SSTextureWithAlpha> (path);
			}
			if (mtl.specularTextureResourceName != null && mtl.specularTextureResourceName.Length > 0) {
                string path = Path.Combine(basePath, mtl.specularTextureResourceName);
                specular = SSAssetManager.GetInstance<SSTextureWithAlpha> (path);
			}
			if (mtl.ambientTextureResourceName != null && mtl.ambientTextureResourceName.Length > 0) {
                string path = Path.Combine(basePath, mtl.ambientTextureResourceName);
                ambient = SSAssetManager.GetInstance<SSTextureWithAlpha> (path);
			}
			if (mtl.bumpTextureResourceName != null && mtl.bumpTextureResourceName.Length > 0) {
                string path = Path.Combine(basePath, mtl.bumpTextureResourceName);
                bumpMap = SSAssetManager.GetInstance<SSTextureWithAlpha> (path);
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

