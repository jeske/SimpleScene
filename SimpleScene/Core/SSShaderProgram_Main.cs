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
using System.Drawing;

namespace SimpleScene
{
	public class SSShaderProgram_Main : SSShaderProgram
	{
		SSShader vertexShader;  
		SSShader fragmentShader;
		SSShader geometryShader;

		#region Uniform Modifiers
		public bool DiffTexEnabled {
			set { GL.Uniform1 (u_diffTexEnabled, value ? 1 : 0); }
		}

		public bool SpecTexEnabled {
			set { GL.Uniform1 (u_specTexEnabled, value ? 1 : 0); }
		}

		public bool AmbTexEnabled {
			set { GL.Uniform1 (u_ambiTexEnabled, value ? 1 : 0); }
		}

		public bool BumpTexEnabled {
			set { GL.Uniform1 (u_bumpTexEnabled, value ? 1 : 0); }
		}

		public float AnimateSecondsOffset {
			set { GL.Uniform1 (u_animateSecondsOffset, value); }
		}

		public bool ShowWireframes {
			set { GL.Uniform1 (u_showWireframes, value ? 1 : 0); }
		}

		public Rectangle WinScale {
			set { GL.Uniform2 (u_winScale, (float)value.Width, (float)value.Height); }
		}
		#endregion

		#region Uniform Locations
		private int u_winScale;
		private int u_animateSecondsOffset;
		private int u_showWireframes;

		private int u_diffTexEnabled;
		private int u_specTexEnabled;
		private int u_ambiTexEnabled;
		private int u_bumpTexEnabled;
		#endregion

		public SSShaderProgram_Main ()
		{
			// open the shader asset context...
			const string ctx = "./Shaders/";

			m_programID = GL.CreateProgram();
			// we use this method of detecting the extension because we are in a GL2.2 context

			if (GL.GetString(StringName.Extensions).ToLower().Contains("gl_ext_gpu_shader4")) {

				this.vertexShader = SSAssetManager.GetInstance<SSVertexShader>(ctx, "ss4_vertex.glsl");
				GL.AttachShader (m_programID, vertexShader.ShaderID);

				this.fragmentShader = SSAssetManager.GetInstance<SSFragmentShader>(ctx, "ss4_fragment.glsl");
				GL.AttachShader (m_programID, fragmentShader.ShaderID);

				this.geometryShader = SSAssetManager.GetInstance<SSGeometryShader>(ctx, "ss4_geometry.glsl");		
				GL.Ext.ProgramParameter (m_programID, ExtGeometryShader4.GeometryInputTypeExt, (int)All.Triangles);
				GL.Ext.ProgramParameter (m_programID, ExtGeometryShader4.GeometryOutputTypeExt, (int)All.TriangleStrip);
				GL.Ext.ProgramParameter (m_programID, ExtGeometryShader4.GeometryVerticesOutExt, 3);
				GL.AttachShader (m_programID, geometryShader.ShaderID);

			} else {
				this.vertexShader = SSAssetManager.GetInstance<SSVertexShader>(ctx, "ss1_vertex.glsl");
				GL.AttachShader (m_programID, vertexShader.ShaderID);

				this.fragmentShader = SSAssetManager.GetInstance<SSFragmentShader>(ctx, "ss1_fragment.glsl");
				GL.AttachShader (m_programID, fragmentShader.ShaderID);
			}


			GL.LinkProgram(m_programID);
			Console.WriteLine(GL.GetProgramInfoLog(m_programID));

			// shader is initialized now...
		
			GL.UseProgram (m_programID);
			GL.Uniform1 (GL.GetUniformLocation (m_programID, "showWireframes"), (int)0);			
			GL.Uniform1 (GL.GetUniformLocation (m_programID, "animateSecondsOffset"), (float)0.0f);		

			// get shader uniform variable locations	
			int GLun_diffTex = GL.GetUniformLocation(m_programID, "diffTex");
			int GLun_specTex = GL.GetUniformLocation(m_programID, "specTex");
			int GLun_ambiTex = GL.GetUniformLocation(m_programID, "ambiTex");
			int GLun_bumpTex = GL.GetUniformLocation(m_programID, "bumpTex");

			// bind shader uniform variable handles to GL texture-unit numbers
			GL.Uniform1(GLun_diffTex,0); // Texture.Texture0
			GL.Uniform1(GLun_specTex,1); // Texture.Texture1
			GL.Uniform1(GLun_ambiTex,2); // Texture.Texture2
			GL.Uniform1(GLun_bumpTex,3); // Texture.Texture3


            u_animateSecondsOffset = GL.GetUniformLocation(m_programID, "animateSecondsOffset");
			u_winScale = GL.GetUniformLocation(m_programID, "WIN_SCALE");
			u_showWireframes = GL.GetUniformLocation(m_programID, "showWireframes");

			u_diffTexEnabled = GL.GetUniformLocation(m_programID, "diffTexEnabled");
			u_specTexEnabled = GL.GetUniformLocation(m_programID, "specTexEnabled");
			u_ambiTexEnabled = GL.GetUniformLocation(m_programID, "ambiTexEnabled");
			u_bumpTexEnabled = GL.GetUniformLocation(m_programID, "bumpTexEnabled");

			{
				ErrorCode glerr;
				if ((glerr = GL.GetError ()) != ErrorCode.NoError) {
					throw new Exception (String.Format ("GL Error: {0}", glerr));
				}
			}

		}
	}
}

