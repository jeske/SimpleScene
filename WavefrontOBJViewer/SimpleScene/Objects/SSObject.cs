// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.


using System;

using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;


namespace WavefrontOBJViewer
{
	
	public abstract class SSObject {
		// object orientation
		private Vector3 _pos;
		public Vector3 Pos {  
			get { return _pos; } 
			set { _pos = value; updateMat (); }
		}
		private Vector3 _dir;
		public Vector3 Dir { get { return _dir; } }
		private Vector3 _up;
		public Vector3 Up { get { return _up; } }
		private Vector3 _right;
		public Vector3 Right { get { return _right; } }

		// transform matricies
		public Matrix4 localMat;
		public Matrix4 worldMat;

		// TODO: use these!
		private SSObject parent;
		private ICollection<SSObject> children;

		public void Orient(Matrix4 newOrientation) {
			this._dir = new Vector3(newOrientation.M31, newOrientation.M32, newOrientation.M33);
			this._up = new Vector3(newOrientation.M21, newOrientation.M22, newOrientation.M23);
			this._right = Vector3.Cross(this._up, this._dir);
			this._right.Normalize();
			
			this.updateMat();
		}

		public void updateMat() {
			Matrix4 newLocalMat = Matrix4.Identity;

			// rotation..
			newLocalMat.M11 = _right.X;
			newLocalMat.M12 = _right.Y;
			newLocalMat.M13 = _right.Z;

			newLocalMat.M21 = _up.X;
			newLocalMat.M22 = _up.Y;
			newLocalMat.M23 = _up.Z;

			newLocalMat.M31 = _dir.X;
			newLocalMat.M32 = _dir.Y;
			newLocalMat.M33 = _dir.Z;

			// position
			newLocalMat.M41 = _pos.X;
			newLocalMat.M42 = _pos.Y;
			newLocalMat.M43 = _pos.Z;

			// compute world transformation
			Matrix4 newWorldMat;

			if (this.parent == null) {
				newWorldMat = newLocalMat;
			} else {
				newWorldMat = newLocalMat * this.parent.worldMat;
			}

			// apply the transformations
			this.localMat = newLocalMat;
			this.worldMat = newWorldMat;
		}

		public virtual void Render () {}
		public virtual void Update () {}

		// constructor
		public SSObject() { 
			// position at the origin...
			this._pos = new Vector3(0.0f,0.0f,0.0f);
			
			// rotation
			this._dir = new Vector3(0.0f,0.0f,1.0f);    // face Z+
			this._up = new Vector3(0.0f,1.0f,0.0f);     // Y+ up
			this._right = new Vector3(1.0f,0.0f,0.0f);  // X+ right
			
			this.updateMat ();
		}
	}
}

