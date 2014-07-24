// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK.Graphics.OpenGL;

using SimpleScene;

// shader doc references..
//
// http://fabiensanglard.net/bumpMapping/index.php
// http://www.geeks3d.com/20091013/shader-library-phong-shader-with-multiple-lights-glsl/
// http://en.wikibooks.org/wiki/GLSL_Programming/GLUT/Specular_Highlights


namespace WavefrontOBJViewer
{
	partial class WavefrontOBJViewer : OpenTK.GameWindow
	{
		SSShader vertexShader;
		SSShader fragmentShader;
		SSShader geometryShader;

		SSShaderProgram shaderPgm;
		
		// uniform locations
		public int u_WIN_SCALE;
		public int u_animateSecondsOffset;

		public void setupShaders() {
			// open the shader asset context...
			var ctx = SSAssetManager.mgr.getContext ("./shaders/");

			int ProgramID = GL.CreateProgram();

			// These shaders implement GLSL single-pass wireframes as described here...
			// http://strattonbrazil.blogspot.com/2011/09/single-pass-wireframe-rendering_10.html

			// another method of single-pass wireframes is this GLSL wireframe geometry shader
			// which outputs additional GL-Line geometry for every triangle. 
			// We don't use this method because GLSL120 is not allowed to output additional
			// primitives, and because it still suffers from z-fighting. 
			// http://www.lighthouse3d.com/tutorials/glsl-core-tutorial/geometry-shader/

			// https://wiki.engr.illinois.edu/display/graphics/Geometry+Shader+Hello+World
			// GLSL fragment shader and bump mapping tutorial
			// http://fabiensanglard.net/bumpMapping/index.php

			// we use this method of detecting the extension because we are in a GL2.2 context

			if (GL.GetString(StringName.Extensions).ToLower().Contains("gl_ext_gpu_shader4")) {

				this.vertexShader = new SSShader (ShaderType.VertexShader, "ss4 bumpVertex", ctx.getAsset ("ss4_vertex.glsl"));
				GL.AttachShader (ProgramID, vertexShader.ShaderID);
						
				this.fragmentShader = new SSShader (ShaderType.FragmentShader, "ss4 bumpFragment", ctx.getAsset ("ss4_fragment.glsl"));
				GL.AttachShader (ProgramID, fragmentShader.ShaderID);

				this.geometryShader = new SSShader (ShaderType.GeometryShader, "ss4 bumpGeometry", ctx.getAsset ("ss4_geometry.glsl"));						
				GL.Ext.ProgramParameter (ProgramID, ExtGeometryShader4.GeometryInputTypeExt, (int)All.Triangles);
				GL.Ext.ProgramParameter (ProgramID, ExtGeometryShader4.GeometryOutputTypeExt, (int)All.TriangleStrip);
				GL.Ext.ProgramParameter (ProgramID, ExtGeometryShader4.GeometryVerticesOutExt, 3);
				GL.AttachShader (ProgramID, geometryShader.ShaderID);

			} else {
				this.vertexShader = new SSShader (ShaderType.VertexShader, "ss1 bumpVertex", ctx.getAsset ("ss1_vertex.glsl"));
				GL.AttachShader (ProgramID, vertexShader.ShaderID);

				this.fragmentShader = new SSShader (ShaderType.FragmentShader, "ss1 bumpFragment", ctx.getAsset ("ss1_fragment.glsl"));
				GL.AttachShader (ProgramID, fragmentShader.ShaderID);

			}

			GL.LinkProgram(ProgramID);
			Console.WriteLine(GL.GetProgramInfoLog(ProgramID));

			// since the shader is initilized, make the shader program object
			this.shaderPgm = new SSShaderProgram(ProgramID);

			GL.UseProgram (ProgramID);
			GL.Uniform1 (GL.GetUniformLocation (ProgramID, "showWireframes"), (int)0);			
			GL.Uniform1 (GL.GetUniformLocation (ProgramID, "animateSecondsOffset"), (float)0.0f);			

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

			{
				ErrorCode glerr;
				if ((glerr = GL.GetError ()) != ErrorCode.NoError) {
					throw new Exception (String.Format ("GL Error: {0}", glerr));
				}
			}
		}

	}
}


			
						
												
			
