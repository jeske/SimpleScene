// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.IO;

using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace WavefrontOBJViewer
{
	// TODO: add the ability to load form a stream, to support zip-archives and other non-file textures
	
	public class SSTexture
	{	
		public readonly SSAssetItem textureAsset;

		private int Texture;
		public int TextureID { get { return Texture; } }
		public SSTexture (SSAssetItem textureAsset) {
			this.textureAsset = textureAsset;		
			loadTexture();
		}
		
		private void loadTexture() {
			// http://www.opentk.com/node/259

		    //make a bitmap out of the stream data...
			Bitmap TextureBitmap = new Bitmap (textureAsset.Open());
		    
		    //get the data out of the bitmap
		    System.Drawing.Imaging.BitmapData TextureData = 
			TextureBitmap.LockBits(
		            new System.Drawing.Rectangle(0,0,TextureBitmap.Width,TextureBitmap.Height),
		            System.Drawing.Imaging.ImageLockMode.ReadOnly,
		            System.Drawing.Imaging.PixelFormat.Format24bppRgb
		        );
		 
		    //Code to get the data to the OpenGL Driver
		  
		    //generate one texture and put its ID number into the "Texture" variable
		    GL.GenTextures(1,out Texture);
		    //tell OpenGL that this is a 2D texture
		    GL.BindTexture(TextureTarget.Texture2D,Texture);
		 
		    //the following code sets certian parameters for the texture
		    GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float)TextureEnvMode.Modulate);
		    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.LinearMipmapLinear);
		    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);
		    
		    // tell OpenGL to build mipmaps out of the bitmap data
		    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, (float)1.0f);
		 
		    // load the texture
		    
		    GL.TexImage2D(
                TextureTarget.Texture2D,
                0, // level
                PixelInternalFormat.CompressedRgb,
                TextureBitmap.Width, TextureBitmap.Height,
                0, // border
                PixelFormat.Bgr,     // why is this Bgr when the lockbits is rgb!?
                PixelType.UnsignedByte,
                TextureData.Scan0
                );

		 
		    //free the bitmap data (we dont need it anymore because it has been passed to the OpenGL driver
		    TextureBitmap.UnlockBits(TextureData);
		 
		}
		
	}
}

