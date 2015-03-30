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
	public class SSInstancePssmShaderProgram : SSPssmShaderProgram, ISSInstancableShaderProgram 
	{
		#region attribute locations
		private readonly int a_instancePos;
		private readonly int a_instanceOrientationXY;
		private readonly int a_instanceOrientationZ;
		private readonly int a_instanceMasterScale;
		private readonly int a_instanceComponentScaleXY;
		private readonly int a_instanceComponentScaleZ;
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

		public int AttrTexCoord {
			get { return -1; } // not used
		}

		public int AttrNormal {
			get { return -1; } // not used
		}
		#endregion

		public SSInstancePssmShaderProgram ()
			: base ("#define INSTANCE_DRAW\n")
		{
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

