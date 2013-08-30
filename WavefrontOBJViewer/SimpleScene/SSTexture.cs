using System;

namespace WavefrontOBJViewer
{
	// TODO: add the ability to load form a stream, to support zip-archives and other non-file textures
	
	public class SSTexture
	{
		public readonly string texFilename;
		public SSTexture (string texFilename) {
			this.texFilename = texFilename;		
		}
	}
}

