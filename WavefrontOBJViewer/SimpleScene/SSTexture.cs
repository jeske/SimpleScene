using System;

using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace WavefrontOBJViewer
{
	// TODO: add the ability to load form a stream, to support zip-archives and other non-file textures
	
	public class SSTexture
	{
		public readonly string texFilename;
		private int Texture;
		public int TextureID { get { return Texture; } }
		public SSTexture (string texFilename) {
			this.texFilename = texFilename;		
			loadTexture();
		}
		
		private void loadTexture() {
			// http://www.opentk.com/node/259
		
			if (!System.IO.File.Exists(texFilename)) {
				throw new Exception("SSTexture: missing texture file : " + texFilename);
			}
			
		    //make a bitmap out of the file on the disk
		    Bitmap TextureBitmap = new Bitmap(texFilename);
		    
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
                PixelInternalFormat.Three,
                TextureBitmap.Width, TextureBitmap.Height,
                0, // border
                PixelFormat.Bgra,
                PixelType.UnsignedByte,
                TextureData.Scan0
                );
                
		 
		    //free the bitmap data (we dont need it anymore because it has been passed to the OpenGL driver
		    TextureBitmap.UnlockBits(TextureData);
		 
		}
		
	}
}

