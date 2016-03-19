using System;
using SimpleScene.Demos;
using System.Drawing;

namespace SimpleScene
{
	public static class SSAssetManagerRegisterDefaultTypes
	{
		public static void RegisterTypes ()
		{
			// Register SS types for loading by SSAssetManager
			SSAssetManager.RegisterLoadDelegate<SSTexture>(
				(path) => { return new SSTexture(path); }
			);
			SSAssetManager.RegisterLoadDelegate<SSTextureWithAlpha>(
				(path) => { return new SSTextureWithAlpha(path); }
			);
			SSAssetManager.RegisterLoadDelegate<SSMesh_wfOBJ>(
				(path) => { return new SSMesh_wfOBJ(path); }
			);
			SSAssetManager.RegisterLoadDelegate<SSVertexShader>(
				(path) => { return new SSVertexShader(path); }
			);
			SSAssetManager.RegisterLoadDelegate<SSFragmentShader>(
				(path) => { return new SSFragmentShader(path); }
			);
			SSAssetManager.RegisterLoadDelegate<SSGeometryShader>(
				(path) => { return new SSGeometryShader(path); }
			);
			SSAssetManager.RegisterLoadDelegate<SSSkeletalMeshMD5[]> (
				(path) => { return SSMD5MeshParser.ReadMeshes(path); }
			);
			SSAssetManager.RegisterLoadDelegate<SSSkeletalAnimationMD5> (
				(path) => { return SSMD5AnimParser.ReadAnimation(path); }
			);
            SSAssetManager.RegisterLoadDelegate<MatterHackers.Agg.Font.TypeFace>(
                (path) => { return SSFontLoader.loadTypeFace(path); }
            );
            #if false
            SSAssetManager.RegisterLoadDelegate<FontFamily[]>(
            (ctx, path) => { return SSFontLoader.loadFontFamilies(path); }
            );
            #endif
		}
	}
}

