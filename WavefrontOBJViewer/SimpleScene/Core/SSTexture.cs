// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.IO;

using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace WavefrontOBJViewer
{
	// TODO: add delegate interface for providing the texture-surface (aka reloading)
	// TODO: add support for OpenGL texture eviction extension (i forget the name of the ext)

	public class SSTexture
	{	
		private int _glTextureID = 0;
		public int TextureID { get { return _glTextureID; } }
		public SSTexture () { }

		public void createFromBitmap(Bitmap TextureBitmap) {		    
		    //get the data out of the bitmap
		    System.Drawing.Imaging.BitmapData TextureData = 
			TextureBitmap.LockBits(
		            new System.Drawing.Rectangle(0,0,TextureBitmap.Width,TextureBitmap.Height),
		            System.Drawing.Imaging.ImageLockMode.ReadOnly,
					System.Drawing.Imaging.PixelFormat.Format24bppRgb
		        );
		 
		    //Code to get the data to the OpenGL Driver
		  
			GL.ActiveTexture(TextureUnit.Texture0);

		    //generate one texture and put its ID number into the "_glTextureID" variable
		    GL.GenTextures(1,out _glTextureID);
		    //tell OpenGL that this is a 2D texture
		    GL.BindTexture(TextureTarget.Texture2D,_glTextureID);
		 
		    //the following code sets certian parameters for the texture
			GL.TexEnv (TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float)TextureEnvMode.Modulate);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.LinearMipmapLinear);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);
		    
		    // tell OpenGL to build mipmaps out of the bitmap data
		    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, (float)1.0f);
		 
			// tell openGL the next line begins on a word boundary...
			GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);

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
			GL.GetError ();	

			Console.WriteLine("SSTexture: loaded texture size = {0} {1}",TextureBitmap.Width,TextureBitmap.Height);


		    //free the bitmap data (we dont need it anymore because it has been passed to the OpenGL driver
		    TextureBitmap.UnlockBits(TextureData);		 
		}
		
	}
}

