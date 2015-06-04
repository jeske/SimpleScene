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
		protected static SSMeshBoundingSphere _boundingSphereMesh = new SSMeshBoundingSphere (1f);

		public Color4 MainColor = Color4.White;
		public bool Selectable = true;

	    public Color4 AmbientMatColor = new Color4(0.0006f,0.0006f,0.0006f,1.0f);
		public Color4 DiffuseMatColor = new Color4(0.3f, 0.3f, 0.3f, 1f);
		public Color4 SpecularMatColor = new Color4(0.6f, 0.6f, 0.6f, 1f);
		public Color4 EmissionMatColor = new Color4(0.001f, 0.001f, 0.001f, 1f);
		public float ShininessMatColor = 10.0f;
        
		public virtual SSTextureMaterial textureMaterial { get; set; }
		public virtual bool alphaBlendingEnabled { get; set; }

		public virtual Vector3 localBoundingSphereCenter { get; set; }
		public virtual float localBoundingSphereRadius { get; set; }

		public Vector3 worldBoundingSphereCenter {
			get {
				return Vector3.Transform(localBoundingSphereCenter, this.worldMat);
			}
		}

		public float worldBoundingSphereRadius {
			get {
				return localBoundingSphereRadius * ScaleMax;
			}
		}

		public SSSphere worldBoundingSphere {
			get { 
				return new SSSphere (worldBoundingSphereCenter, worldBoundingSphereRadius); 
			}
		}

		public string Name = "";

		public SSObject() : base() {
			Name = String.Format("Unnamed:{0}",this.GetHashCode());	
			localBoundingSphereCenter = Vector3.Zero;
			localBoundingSphereRadius = 1f;
			alphaBlendingEnabled = false;
		}

        protected static void resetTexturingState()
        {
            GL.Disable(EnableCap.Texture2D);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            // don't turn off this stuff, because it might be used for the shadowmap.
            // GL.ActiveTexture(TextureUnit.Texture4);GL.BindTexture(TextureTarget.Texture2D, 0);
            // GL.ActiveTexture(TextureUnit.Texture5);GL.BindTexture(TextureTarget.Texture2D, 0);
            // GL.ActiveTexture(TextureUnit.Texture6);GL.BindTexture(TextureTarget.Texture2D, 0);
            // GL.ActiveTexture(TextureUnit.Texture7);GL.BindTexture(TextureTarget.Texture2D, 0);
            // GL.ActiveTexture(TextureUnit.Texture8);GL.BindTexture(TextureTarget.Texture2D, 0);
        }

		protected void setMaterialState(SSMainShaderProgram mainShader)
        {
            GL.Enable(EnableCap.ColorMaterial); // turn off per-vertex color
            GL.Color4(this.MainColor);

            // setup the base color values...
            GL.Material(MaterialFace.Front, MaterialParameter.Ambient, AmbientMatColor);
            GL.Material(MaterialFace.Front, MaterialParameter.Diffuse, DiffuseMatColor);
            GL.Material(MaterialFace.Front, MaterialParameter.Specular, SpecularMatColor);
            GL.Material(MaterialFace.Front, MaterialParameter.Emission, EmissionMatColor);
            GL.Material(MaterialFace.Front, MaterialParameter.Shininess, ShininessMatColor);

			if (textureMaterial != null) {
				if (mainShader != null) {
					mainShader.SetupTextures (textureMaterial);
				} else {
					GL.ActiveTexture(TextureUnit.Texture0);
					GL.Enable(EnableCap.Texture2D);
					if (textureMaterial.ambientTex != null || textureMaterial.diffuseTex != null) {
						// fall back onto the diffuse texture in the absence of ambient
						SSTexture tex = textureMaterial.ambientTex ?? textureMaterial.diffuseTex;
						GL.BindTexture(TextureTarget.Texture2D, tex.TextureID);
					} else {
						GL.BindTexture(TextureTarget.Texture2D, 0);
					}
				}
			}
        }

        protected void setDefaultShaderState(SSMainShaderProgram pgm) {
            if (pgm != null) {
                pgm.Activate();
                pgm.UniObjectWorldTransform = this.worldMat;
                pgm.SetupTextures();
            }
        }

		protected void renderBoundingSphereMesh(ref SSRenderConfig renderConfig)
		{
			if (worldBoundingSphereRadius > 0f 
			&& (renderConfig.renderBoundingSpheresLines || renderConfig.renderBoundingSpheresSolid)) {
				GL.Color4 (MainColor);
				Matrix4 modelViewMat 
					= Matrix4.CreateScale(worldBoundingSphereRadius) 
					* Matrix4.CreateTranslation(worldBoundingSphereCenter) 
					* renderConfig.invCameraViewMatrix;
				GL.MatrixMode(MatrixMode.Modelview);
				GL.LoadMatrix(ref modelViewMat);

				//GL.Translate (boundingSphere.center);
				//GL.Scale (new Vector3 (this.scaleMax * boundingSphere.radius));
				_boundingSphereMesh.renderMesh (ref renderConfig);
			}
		}

		public virtual void Render (ref SSRenderConfig renderConfig) {
			// compute and set the modelView matrix, by combining the cameraViewMat
			// with the object's world matrix
			//    ... http://www.songho.ca/opengl/gl_transform.html
			//    ... http://stackoverflow.com/questions/5798226/3d-graphics-processing-how-to-calculate-modelview-matrix
			renderBoundingSphereMesh (ref  renderConfig);

			Matrix4 modelViewMat = this.worldMat * renderConfig.invCameraViewMatrix;
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadMatrix(ref modelViewMat);

            resetTexturingState();

			if (renderConfig.drawingShadowMap) {
                if (renderConfig.drawingPssm && renderConfig.PssmShader != null) {
					renderConfig.PssmShader.Activate ();
					renderConfig.PssmShader.UniObjectWorldTransform = this.worldMat;
                } else {
                    SSShaderProgram.DeactivateAll();
                }
            } else {
                if (renderConfig.MainShader != null) {
                    setDefaultShaderState(renderConfig.MainShader);
                }
				setMaterialState(renderConfig.MainShader);

				if (alphaBlendingEnabled) {
					GL.Enable (EnableCap.Blend);
					GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
				} else {
					GL.Disable (EnableCap.Blend);
				}

                if (this.renderState.lighted) {
                    GL.Enable(EnableCap.Lighting);
                    GL.ShadeModel(ShadingModel.Flat);
                } else {
                    GL.Disable(EnableCap.Lighting);
                }
            }
		}

		public virtual bool Intersect(ref SSRay worldSpaceRay, out float scaledDistanceAlongRay) {
			var distanceAlongRay = 0.0f;
			if (localBoundingSphereRadius > 0f) {
				var objBoundingSphere = worldBoundingSphere;
				if (objBoundingSphere.IntersectsRay(ref worldSpaceRay, out distanceAlongRay)) {
					scaledDistanceAlongRay = ScaleMax * distanceAlongRay;
					return PreciseIntersect(ref worldSpaceRay, ref scaledDistanceAlongRay);
				}
			}
			scaledDistanceAlongRay = 0f;
			return false;
		}

		public virtual bool PreciseIntersect(ref SSRay worldSpaceRay, ref float distanceAlongRay) {
			return true;
		}

		public delegate void PositionOrSizeChangedHandler(SSObject sender);
		public event PositionOrSizeChangedHandler OnPositionOrSizeChanged;
		protected override void NotifyPositionOrSizeChanged ()
		{
			if (OnPositionOrSizeChanged != null) {
				OnPositionOrSizeChanged(this);
			}
		}
	}

	public class SSOBRenderState {
	    public bool lighted = true;
	    public bool visible = true;
        public bool castsShadow = false;
        public bool receivesShadows = false;
		public bool frustumCulling = true;
		public bool toBeDeleted = false;
	}

	// abstract base class for all transformable objects (objects, lights, ...)
	public abstract class SSObjectBase {

		// object orientation
		protected Vector3 _pos;
		public Vector3 Pos {  
			get { return _pos; } 
			set { _pos = value; this.calcMatFromState(); }
		}
		protected Vector3 _scale = new Vector3 (1.0f);
		public Vector3 Scale { 
			get { return _scale; } 
			set { _scale = value; this.calcMatFromState (); this.calcScaleMax (); }
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

		private float _scaleMax = 1f;
		public float ScaleMax { get { return _scaleMax; } }

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

		public void updateMat(ref Vector3 pos, ref Quaternion orient) {
			this._pos = pos;
			Matrix4 mat = Matrix4.CreateFromQuaternion(orient);
			this._right = new Vector3(mat.M11,mat.M12,mat.M13);
			this._up = new Vector3(mat.M21,mat.M22,mat.M23);
			this._dir = new Vector3(mat.M31,mat.M32,mat.M33);
			calcMatFromState();
		}

		public void updateMat(ref Matrix4 mat) {
			this._right = new Vector3(mat.M11,mat.M12,mat.M13);
			this._up = new Vector3(mat.M21,mat.M22,mat.M23);
			this._dir = new Vector3(mat.M31,mat.M32,mat.M33);
			this._pos = new Vector3(mat.M41,mat.M42,mat.M43);
			calcMatFromState();
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

			NotifyPositionOrSizeChanged();
		}

		protected void calcScaleMax()
		{
			float scaleMax = float.NegativeInfinity;
			for (int i = 0; i < 3; ++i) {
				scaleMax = Math.Max (scaleMax, _scale [i]);
			}
			_scaleMax = scaleMax;
		}

		protected virtual void NotifyPositionOrSizeChanged() { }

		public virtual void Update (float fElapsedS) {}

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

