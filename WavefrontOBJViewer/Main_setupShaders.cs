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
				this.vertexShader = new SSShader (ShaderType.VertexShader, "bumpVertex", ctx.getAsset ("ss1_vertex.glsl"));
				GL.AttachShader (ProgramID, vertexShader.ShaderID);

				this.fragmentShader = new SSShader (ShaderType.FragmentShader, "bumpFragment", ctx.getAsset ("ss1_fragment.glsl"));
				GL.AttachShader (ProgramID, fragmentShader.ShaderID);

			}

			GL.LinkProgram(ProgramID);
			Console.WriteLine(GL.GetProgramInfoLog(ProgramID));

			GL.UseProgram (ProgramID);
			GL.Uniform1 (GL.GetUniformLocation (ProgramID, "showWireframes"), (int)0);			
			GL.Uniform1 (GL.GetUniformLocation (ProgramID, "animateSecondsOffset"), (float)0.0f);			

			this.shaderPgm = new SSShaderProgram(ProgramID);

			{
				ErrorCode glerr;
				if ((glerr = GL.GetError ()) != ErrorCode.NoError) {
					throw new Exception (String.Format ("GL Error: {0}", glerr));
				}
			}
		}

	}
}


			
						
												
			
