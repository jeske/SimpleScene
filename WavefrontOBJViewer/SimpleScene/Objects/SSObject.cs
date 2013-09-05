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
		protected Vector3 _pos;
		public Vector3 Pos {  
			get { return _pos; } 
			set { _pos = value; }
		}
		protected Vector3 _dir;
		public Vector3 Dir { get { return _dir; } }
		protected Vector3 _up;
		public Vector3 Up { get { return _up; } }
		protected Vector3 _right;
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
		}
		protected void updateMat() {
			this.updateMat (ref this._dir, ref this._up, ref this._right, ref this._pos);
		}

		protected void updateMat(ref Vector3 dir, ref Vector3 up, ref Vector3 right, ref Vector3 pos) {
			Matrix4 newLocalMat = Matrix4.Identity;

			// rotation..
			newLocalMat.M11 = right.X;
			newLocalMat.M12 = right.Y;
			newLocalMat.M13 = right.Z;

			newLocalMat.M21 = up.X;
			newLocalMat.M22 = up.Y;
			newLocalMat.M23 = up.Z;

			newLocalMat.M31 = dir.X;
			newLocalMat.M32 = dir.Y;
			newLocalMat.M33 = dir.Z;

			// position
			newLocalMat.M41 = pos.X;
			newLocalMat.M42 = pos.Y;
			newLocalMat.M43 = pos.Z;

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
			
			// base-scale
			this._dir = new Vector3(0.0f,0.0f,1.0f);    // Z+  front
			this._up = new Vector3(0.0f,1.0f,0.0f);     // Y+  up
			this._right = new Vector3(1.0f,0.0f,0.0f);  // X+  right
			
			this.updateMat();
			
			// rotate here if we want to.
		}
	}
}

