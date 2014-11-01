// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;


namespace SimpleScene
{

	// abstract base class for "tangible" Renderable objects
	public abstract class SSObject : SSObjectBase {
	    public Color4 ambientMatColor = new Color4(0.001f,0.001f,0.001f,1.0f);
		public Color4 diffuseMatColor = new Color4(1.2f,1.2f,1.2f,1.2f);
		public Color4 specularMatColor = new Color4(0.8f,0.8f,0.8f,1.0f);
		public Color4 emissionMatColor = new Color4(1.0f,1.0f,1.0f,1.0f);
		public float shininessMatColor = 10.0f;

		public string Name = "";

		public SSObject() : base() {
			Name = String.Format("Unnamed:{0}",this.GetHashCode());	
		}
		public virtual void Render (ref SSRenderConfig renderConfig) {
			// compute and set the modelView matrix, by combining the cameraViewMat
			// with the object's world matrix
			//    ... http://www.songho.ca/opengl/gl_transform.html
			//    ... http://stackoverflow.com/questions/5798226/3d-graphics-processing-how-to-calculate-modelview-matrix

			Matrix4 modelViewMat = this.worldMat * renderConfig.invCameraViewMat;
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadMatrix(ref modelViewMat);

            if (renderConfig.drawingShadowMap) // nothing to setup
                return;

            if (renderConfig.BaseShader != null) {
                var shaderPgm = renderConfig.BaseShader;
                // turn off most GL features to start..
                shaderPgm.Activate();
                shaderPgm.u_DiffTexEnabled = false;
                shaderPgm.u_SpecTexEnabled = false;
                shaderPgm.u_AmbTexEnabled = false;
                shaderPgm.u_BumpTexEnabled = false;
                // pass world transform to the main shader for referencing the shadowmap
                shaderPgm.u_ObjectWorldTransform = this.worldMat;
            }

            GL.Disable(EnableCap.Blend);
            // reset things to a default state
            int maxtex = GL.GetInteger(GetPName.MaxTextureUnits); // this is the legacy fixed-function max
            for (int i = 0; i < maxtex; i++) {
                GL.ActiveTexture(TextureUnit.Texture0 + i);
                GL.Disable(EnableCap.Texture2D);
            }

            if (this.renderState.lighted) {
                GL.Enable(EnableCap.Lighting);
                GL.ShadeModel(ShadingModel.Flat);
            } else {
                GL.Disable(EnableCap.Lighting);
            }

            GL.Disable(EnableCap.ColorMaterial); // turn off per-vertex color
	
            // setup the base color values...
            GL.Material(MaterialFace.Front, MaterialParameter.Ambient, ambientMatColor);
            GL.Material(MaterialFace.Front, MaterialParameter.Diffuse, diffuseMatColor);
            GL.Material(MaterialFace.Front, MaterialParameter.Specular, specularMatColor);
            GL.Material(MaterialFace.Front, MaterialParameter.Emission, emissionMatColor);
            GL.Material(MaterialFace.Front, MaterialParameter.Shininess, shininessMatColor);
			
            // GL.Color4(diffuseMatColor);

            // ... subclasses will render the object itself..
		}

		public SSObjectSphere boundingSphere=null;  // TODO: fix this, it's object-space radius, world-space position
		public SSObject collisionShell=null;
		public virtual bool Intersect(ref SSRay worldSpaceRay, out float distanceAlongRay) {
			distanceAlongRay = 0.0f;
			if (boundingSphere != null) {
				if (boundingSphere.Intersect(ref worldSpaceRay, out distanceAlongRay)) {					
			        if (collisionShell != null) {			            
				        return collisionShell.Intersect(ref worldSpaceRay, out distanceAlongRay);
		            } else {
						return PreciseIntersect(ref worldSpaceRay, ref distanceAlongRay);
					}
				}
			}
			return false;
		}

		public virtual bool PreciseIntersect(ref SSRay worldSpaceRay, ref float distanceAlongRay) {
			return true;
		}

	    public delegate void ChangedEventHandler(SSObject sender);
		public event ChangedEventHandler OnChanged;
		public override void ObjectChanged ()
		{
			if (OnChanged != null) {
				OnChanged(this);
			}

		}
	}

	public class SSOBRenderState {
	    public bool lighted = true;
	    public bool visible = true;
        public bool castsShadow = true;
		public bool toBeDeleted = false;
	}

	// abstract base class for all transformable objects (objects, lights, ...)
	public abstract class SSObjectBase {

		// object orientation
		protected Vector3 _pos;
		public Vector3 Pos {  
			get { return _pos; } 
			set { _pos = value; this.calcMatFromState();}
		}
		protected Vector3 _scale = new Vector3 (1.0f);
		public Vector3 Scale { 
			get { return _scale; } 
			set { _scale = value; this.calcMatFromState (); }
		}
		public float Size {
		    set { Scale = new Vector3(value); }
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

		public SSOBRenderState renderState = new SSOBRenderState();

		// TODO: use these!
		private SSObject parent;
		private ICollection<SSObject> children;

		public void Orient(Quaternion orientation) {
			Matrix4 newOrientation = Matrix4.CreateFromQuaternion(orientation);
			this._dir = new Vector3(newOrientation.M31, newOrientation.M32, newOrientation.M33);
			this._up = new Vector3(newOrientation.M21, newOrientation.M22, newOrientation.M23);
			this._right = Vector3.Cross(this._up, this._dir).Normalized();
			this.calcMatFromState(); 
		}
		
		private float DegreeToRadian(float angleInDegrees) {
			return (float)Math.PI * angleInDegrees / 180.0f;
		}

		public void EulerDegAngleOrient(float XDelta, float YDelta) {
			Quaternion yaw_Rotation = Quaternion.FromAxisAngle(Vector3.UnitY,DegreeToRadian(-XDelta));
    		Quaternion pitch_Rotation = Quaternion.FromAxisAngle(Vector3.UnitX,DegreeToRadian(-YDelta));

			this.calcMatFromState(); // make sure our local matrix is current

			// openGL requires pre-multiplation of these matricies...
			Quaternion qResult = yaw_Rotation * pitch_Rotation * this.localMat.ExtractRotation();
						
			this.Orient(qResult);
		}

		protected void updateMat(ref Vector3 dir, ref Vector3 up, ref Vector3 right, ref Vector3 pos) {

			this._pos = pos;
			this._dir = dir;
			this._right = right;
			this._up = up;
			calcMatFromState();
		}

		protected void calcMatFromState() {
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

			newLocalMat *= Matrix4.CreateScale (this._scale);

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

			ObjectChanged();
		}

		public virtual void ObjectChanged() { }

		public virtual void Update (float fElapsedMS) {}

		// constructor
		public SSObjectBase() { 
			// position at the origin...
			this._pos = new Vector3(0.0f,0.0f,0.0f);
			
			// base-scale
			this._dir = new Vector3(0.0f,0.0f,1.0f);    // Z+  front
			this._up = new Vector3(0.0f,1.0f,0.0f);     // Y+  up
			this._right = new Vector3(1.0f,0.0f,0.0f);  // X+  right
			
			this.calcMatFromState();
			
			// rotate here if we want to.
		}
	}
}

