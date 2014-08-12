// Copyright(C) David W. Jeske, 2014, All Rights Reserved.
// Released to the public domain. 


// shader doc references..
//
// http://fabiensanglard.net/bumpMapping/index.php
// http://www.geeks3d.com/20091013/shader-library-phong-shader-with-multiple-lights-glsl/
// http://en.wikibooks.org/wiki/GLSL_Programming/GLUT/Specular_Highlights

using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using SimpleScene;

namespace SimpleScene
{
	public class SSShaderProgram_Main : SSShaderProgram
	{
		SSShader vertexShader;  
		SSShader fragmentShader;
		SSShader geometryShader;

		// uniform locations
		public int u_WIN_SCALE;
		public int u_animateSecondsOffset;
		public int u_showWireframes;
		
		//public int u_diffTexEnabled;
		//public int u_specTexEnabled;
		//public int u_ambiTexEnabled;
		//public int u_bumpTexEnabled;

		public SSShaderProgram_Main ()
		{
			// open the shader asset context...
			var ctx = SSAssetManager.mgr.getContext ("./Shaders/");

			int ProgramID = GL.CreateProgram();
			// we use this method of detecting the extension because we are in a GL2.2 context

			if (GL.GetString(StringName.Extensions).ToLower().Contains("gl_ext_gpu_shader4")) {

				this.vertexShader = new SSShader (ShaderType.VertexShader, "bumpVertex", ctx.getAsset ("ss4_vertex.glsl"));
				GL.AttachShader (ProgramID, vertexShader.ShaderID);

				this.fragmentShader = new SSShader (ShaderType.FragmentShader, "bumpFragment", ctx.getAsset ("ss4_fragment.glsl"));
				GL.AttachShader (ProgramID, fragmentShader.ShaderID);

				this.geometryShader = new SSShader (ShaderType.GeometryShader, "bumpGeometry", ctx.getAsset ("ss4_geometry.glsl"));						
				GL.Ext.ProgramParameter (ProgramID, ExtGeometryShader4.GeometryInputTypeExt, (int)All.Triangles);
				GL.Ext.ProgramParameter (ProgramID, ExtGeometryShader4.GeometryOutputTypeExt, (int)All.TriangleStrip);
				GL.Ext.ProgramParameter (ProgramID, ExtGeometryShader4.GeometryVerticesOutExt, 3);
				GL.AttachShader (ProgramID, geometryShader.ShaderID);

			} else {
				this.vertexShader = new SSShader (ShaderType.VertexShader, "vertex", ctx.getAsset ("ss1_vertex.glsl"));
				GL.AttachShader (ProgramID, vertexShader.ShaderID);

				this.fragmentShader = new SSShader (ShaderType.FragmentShader, "fragment", ctx.getAsset ("ss1_fragment.glsl"));
				GL.AttachShader (ProgramID, fragmentShader.ShaderID);
			}


			GL.LinkProgram(ProgramID);
			Console.WriteLine(GL.GetProgramInfoLog(ProgramID));

			// shader is initialized now...
		
			GL.UseProgram (ProgramID);
			GL.Uniform1 (GL.GetUniformLocation (ProgramID, "showWireframes"), (int)0);			
			GL.Uniform1 (GL.GetUniformLocation (ProgramID, "animateSecondsOffset"), (float)0.0f);		

			// get shader uniform variable locations	
			int GLun_diffTex = GL.GetUniformLocation(ProgramID, "diffTex");
			int GLun_specTex = GL.GetUniformLocation(ProgramID, "specTex");
			int GLun_ambiTex = GL.GetUniformLocation(ProgramID, "ambiTex");
			int GLun_bumpTex = GL.GetUniformLocation(ProgramID, "bumpTex");

			// bind shader uniform variable handles to GL texture-unit numbers
			GL.Uniform1(GLun_diffTex,0); // Texture.Texture0
			GL.Uniform1(GLun_specTex,1); // Texture.Texture1
			GL.Uniform1(GLun_ambiTex,2); // Texture.Texture2
			GL.Uniform1(GLun_bumpTex,3); // Texture.Texture3


            u_animateSecondsOffset = GL.GetUniformLocation(ProgramID, "animateSecondsOffset");
			u_WIN_SCALE = GL.GetUniformLocation(ProgramID, "WIN_SCALE");
			u_showWireframes = GL.GetUniformLocation(ProgramID, "showWireframes");

			//u_diffTexEnabled = GL.GetUniformLocation(ProgramID, "diffTexEnabled");
			//u_specTexEnabled = GL.GetUniformLocation(ProgramID, "specTexEnabled");
			//u_ambiTexEnabled = GL.GetUniformLocation(ProgramID, "ambiTexEnabled");
			//u_bumpTexEnabled = GL.GetUniformLocation(ProgramID, "bumpTexEnabled");

			{
				ErrorCode glerr;
				if ((glerr = GL.GetError ()) != ErrorCode.NoError) {
					throw new Exception (String.Format ("GL Error: {0}", glerr));
				}
			}

		}
	}
}

