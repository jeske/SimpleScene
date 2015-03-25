// Copyright(C) David W. Jeske, 2014, All Rights Reserved.
// Released to the public domain. 

using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SimpleScene;
using System.Drawing;
using System.Collections.Generic;

namespace SimpleScene
{
	public class SSInstancePssmShaderProgram : SSShaderProgram
		, ISSPssmShaderProgram, ISSInstancableShaderProgram 
	{
		private static string c_ctx = "./Shaders/Shadowmap";

		#region shaders
		private readonly SSShader m_vertexShader;  
		private readonly SSShader m_fragmentShader;
		private readonly SSShader m_geometryShader;
		#endregion

		#region uniform locations
		private readonly int u_numShadowMaps;
		private readonly int u_objectWorldTransform;
		private readonly int[] u_shadowMapVPs = new int[SSParallelSplitShadowMap.c_numberOfSplits];
		#endregion

		#region attribute locations
		private readonly int a_instancePos;
		private readonly int a_instanceOrientationXY;
		private readonly int a_instanceOrientationZ;
		private readonly int a_instanceMasterScale;
		private readonly int a_instanceComponentScaleXY;
		private readonly int a_instanceComponentScaleZ;
		#endregion

		#region uniform modifiers
		public Matrix4 UniObjectWorldTransform {
			// pass object world transform matrix for use in shadowmap lookup
			set { assertActive(); GL.UniformMatrix4(u_objectWorldTransform, false, ref value); }
		}

		public void UpdateShadowMapVPs(Matrix4[] mvps) {
			// pass update mvp matrices for shadowmap lookup
			for (int s = 0; s < SSParallelSplitShadowMap.c_numberOfSplits; ++s) {
				//GL.UniformMatrix4(u_shadowMapVPs + s, false, ref mvps[s]);
				GL.UniformMatrix4(u_shadowMapVPs [s], false, ref mvps [s]);
			}
		}
		#endregion

		#region attribute accessors
		public int AttrInstancePos {
			get { return a_instancePos; }
		}

		public int AttrInstanceOrientationXY {
			get { return a_instanceOrientationXY; }
		}

		public int AttrInstanceOrientationZ {
			get { return a_instanceOrientationZ; }
		}

		public int AttrInstanceMasterScale {
			get { return a_instanceMasterScale; }
		}

		public int AttrInstanceComponentScaleXY {
			get { return a_instanceComponentScaleXY; }
		}

		public int AttrInstanceComponentScaleZ {
			get { return a_instanceComponentScaleZ; }
		}

		public int AttrInstanceColor {
			get { return -1; } // not used
		}

		public int AttrInstanceSpriteIndex {
			get { return -1; } // not used
		}

		public int AttrInstanceSpriteOffsetU {
			get { return -1; } // not used
		}

		public int AttrInstanceSpriteOffsetV {
			get { return -1; } // not used
		}

		public int AttrInstanceSpriteSizeU {
			get { return -1; } // not used
		}

		public int AttrInstanceSpriteSizeV {
			get { return -1; } // not used
		}
		#endregion

		public SSInstancePssmShaderProgram ()
		{
			string glExtStr = GL.GetString (StringName.Extensions).ToLower ();
			if (!glExtStr.Contains ("gl_ext_gpu_shader4")
		     || !glExtStr.Contains ("gl_ext_draw_instanced")) {
				Console.WriteLine ("Instance PSSM shader not supported");
				m_isValid = false;
				return;
			}
			m_vertexShader = SSAssetManager.GetInstance<SSVertexShader>(c_ctx, 
				"instance_pssm_vertex.glsl");
			m_vertexShader.LoadShader();
			attach(m_vertexShader);

			m_fragmentShader = SSAssetManager.GetInstance<SSFragmentShader>(c_ctx,
				"pssm_fragment.glsl");
			m_fragmentShader.LoadShader();
			attach(m_fragmentShader);

			m_geometryShader = SSAssetManager.GetInstance<SSGeometryShader>(c_ctx,
				"pssm_geometry.glsl");
			m_geometryShader.LoadShader();
			GL.Ext.ProgramParameter (m_programID, ExtGeometryShader4.GeometryInputTypeExt, (int)All.Triangles);
			GL.Ext.ProgramParameter (m_programID, ExtGeometryShader4.GeometryOutputTypeExt, (int)All.TriangleStrip);
			GL.Ext.ProgramParameter (m_programID, ExtGeometryShader4.GeometryVerticesOutExt, 3 * SSParallelSplitShadowMap.c_numberOfSplits);
			attach(m_geometryShader);

			link();
			Activate();

			// TODO: debug passing things through arrays
			for (int i = 0; i < SSParallelSplitShadowMap.c_numberOfSplits; ++i) {
				var str = "shadowMapVPs" + i;
				u_shadowMapVPs[i] = getUniLoc(str);
			}
			//u_shadowMapVPs = getUniLoc("shadowMapVPs");

			//u_shadowMapSplits = getUniLoc("shadowMapSplits");
			u_objectWorldTransform = getUniLoc("objWorldTransform");
			u_numShadowMaps = getUniLoc("numShadowMaps");

			GL.Uniform1(u_numShadowMaps, SSParallelSplitShadowMap.c_numberOfSplits);

			// attributes
			a_instancePos = getAttrLoc("instancePos");
			a_instanceOrientationXY = getAttrLoc("instanceOrientationXY");
			a_instanceOrientationZ = getAttrLoc ("instanceOrientationZ");
			a_instanceMasterScale = getAttrLoc("instanceMasterScale");
			a_instanceComponentScaleXY = getAttrLoc("instanceComponentScaleXY");
			a_instanceComponentScaleZ = getAttrLoc("instanceComponentScaleZ");

			m_isValid = checkGlValid();
		}
	}
}

