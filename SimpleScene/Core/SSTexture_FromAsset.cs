using System;
using System.Drawing;

namespace SimpleScene
{


	// TODO: add the ability to load form a stream, to support zip-archives and other non-file textures


	public class SSTexture_FromAsset : SSTexture
	{

		public readonly SSAssetItem textureAsset;
		public SSTexture_FromAsset (SSAssetItem textureAsset) {
			this.textureAsset = textureAsset;	
			Bitmap textureBitmap = new Bitmap (textureAsset.Open());

			this.loadFromBitmap (textureBitmap, name: textureAsset.resourceName);

		}
	}
}

