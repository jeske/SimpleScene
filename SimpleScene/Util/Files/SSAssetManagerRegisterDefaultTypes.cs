using System;

namespace SimpleScene
{
	public static class SSAssetManagerRegisterDefaultTypes
	{
		public static void RegisterTypes ()
		{
			// Register SS types for loading by SSAssetManager
			SSAssetManager.RegisterLoadDelegate<SSTexture>(
				(ctx, filename) => { return new SSTexture(ctx, filename); }
			);
			SSAssetManager.RegisterLoadDelegate<SSTextureWithAlpha>(
				(ctx, filename) => { return new SSTextureWithAlpha(ctx, filename); }
			);
			SSAssetManager.RegisterLoadDelegate<SSMesh_wfOBJ>(
				(ctx, filename) => { return new SSMesh_wfOBJ(ctx, filename); }
			);
			SSAssetManager.RegisterLoadDelegate<SSVertexShader>(
				(ctx, filename) => { return new SSVertexShader(ctx, filename); }
			);
			SSAssetManager.RegisterLoadDelegate<SSFragmentShader>(
				(ctx, filename) => { return new SSFragmentShader(ctx, filename); }
			);
			SSAssetManager.RegisterLoadDelegate<SSGeometryShader>(
				(ctx, filename) => { return new SSGeometryShader(ctx, filename); }
			);
			SSAssetManager.RegisterLoadDelegate<SSSkeletalMeshMD5[]> (
				(ctx, filename) => { return SSMD5MeshParser.ReadMeshes(ctx, filename); }
			);
			SSAssetManager.RegisterLoadDelegate<SSSkeletalAnimationMD5> (
				(ctx, filename) => { return SSMD5AnimParser.ReadAnimation(ctx, filename); }
			);
		}
	}
}

