	using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using SimpleScene.Util;

namespace SimpleScene.Demos
{
	public class SLaserBeamMiddleObject : SSObject
	{
        // TODO restructure to use one texture?

		protected static readonly UInt16[] _middleIndices = {
			0,1,2, 1,3,2, // left cap
			2,3,4, 3,5,4, // middle
			4,5,6, 5,7,6  // right cap?
		};
		protected static readonly UInt16[] _interferenceIndices = {
			0,1,2, 1,3,2
		};

        /// <summary>
        /// Orientation presets for drawing the crossbeam with outward triangles on all surfaces
        /// </summary>
        public static readonly Matrix4[] xOrientationPresets = {
            Matrix4.Identity,
            Matrix4.CreateRotationX((float)Math.PI),
            Matrix4.CreateRotationX(+(float)Math.PI / 2f),
            Matrix4.CreateRotationX(-(float)Math.PI / 2f)
        };

		#region per-frame data sources
		protected readonly SLaser _laser;
		protected readonly int _beamId;
		protected readonly SSScene _cameraScene;
		#endregion

		#region stretched middle sprites
		public SSTexture middleBackgroundSprite = null;
		public SSTexture middleOverlayTexture = null;
		protected SSVertex_PosTex[] _middleVertices;
		protected SSIndexedMesh<SSVertex_PosTex> _middleMesh;
		#endregion

		#region interference sprite
		public SSTexture interferenceTexture = null;
		protected SSVertex_PosTex[] _interferenceVertices;
		protected SSIndexedMesh<SSVertex_PosTex> _interferenceMesh;
		#endregion

		// TODO cache these computations
		public override Vector3 localBoundingSphereCenter {
			get {
                var beam = _laser.beam(_beamId);
                Vector3 middleWorld = (beam.startPosWorld + beam.endPosWorld) / 2f;
                //return Vector3.Transform (middleWorld, this.worldMat.Inverted ());
                //some_name code start 24112019
                return (new Vector4(middleWorld, 1) * this.worldMat.Inverted()).Xyz;
                //some_name code end
            }
        }

		// TODO cache these computations
		public override float localBoundingSphereRadius {
			get {
                var beam = _laser.beam(_beamId);
                return beam.lengthFastWorld();
			}
		}

		public SLaserBeamMiddleObject (SLaser laser,
									   int beamId,
									   SSScene cameraScene,
								       SSTexture middleBackgroundTexture,
									   SSTexture middleOverlayTexture,
                                       SSTexture inteferenceTexture)
		{
			this._laser = laser;
			this._beamId = beamId;
			this._cameraScene = cameraScene;

            this.renderState.castsShadow = false;
            this.renderState.receivesShadows = false;
            this.renderState.depthTest = true;
            this.renderState.depthWrite = false;
            this.renderState.alphaBlendingOn = true;
            //this.renderState.alphaBlendingOn = false;
            
			renderState.blendFactorSrcRGB = renderState.blendFactorSrcAlpha = BlendingFactorSrc.SrcAlpha;
			renderState.blendFactorDestRGB = renderState.blendFactorDestAlpha = BlendingFactorDest.One;

            // reset all mat colors. emission will be controlled during rendering
            this.colorMaterial = new SSColorMaterial(Color4Helper.Zero);

            this.middleBackgroundSprite = middleBackgroundTexture;
            this.middleOverlayTexture = middleOverlayTexture;
            this.interferenceTexture = inteferenceTexture;

			// initialize non-changing vertex data
			_initMiddleMesh ();
			_initInterferenceVertices ();

			// force an update to make sure we are not rendering (very noticable) bogus
			Update(0f);
		}

		public override void Render(SSRenderConfig renderConfig)
		{
			var beam = _laser.beam(_beamId);
			if (beam == null) return; 

			base.Render (renderConfig);

			// step: setup render settings
			SSShaderProgram.DeactivateAll ();
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.Enable (EnableCap.Texture2D);

			var laserParams = _laser.parameters;

            // var startView = Vector3.Transform(beam.startPosWorld, renderConfig.invCameraViewMatrix);
            // var endView = Vector3.Transform (beam.endPosWorld, renderConfig.invCameraViewMatrix);
            //some_name code start 24112019
            Vector3 startView = (new Vector4(beam.startPosWorld, 1) * renderConfig.invCameraViewMatrix).Xyz;
            Vector3 endView = (new Vector4(beam.endPosWorld, 1) * renderConfig.invCameraViewMatrix).Xyz;
            //some_name code end

            var middleView = (startView + endView) / 2f;

			// step: draw middle section:
			Vector3 diff = endView - startView;
            float diff_xy = diff.Xy.Length;
			float phi = -(float)Math.Atan2 (diff.Z, diff_xy);
			float theta = (float)Math.Atan2 (diff.Y, diff.X);
			Matrix4 backgroundOrientMat = Matrix4.CreateRotationY (phi) * Matrix4.CreateRotationZ (theta);
			Matrix4 middlePlacementMat = backgroundOrientMat * Matrix4.CreateTranslation (middleView);
			//Matrix4 startPlacementMat = Matrix4.CreateTranslation (startView);

            float laserLength = diff.Length;
			float middleWidth = laserParams.middleBackgroundWidth * _laser.envelopeIntensity;
            /*
			Vector3 cameraDir = Vector3.Transform(
				-Vector3.UnitZ, _cameraScene.renderConfig.invCameraViewMatrix).Normalized();
            */

            //some_name code start 24112019
            Vector3 cameraDir = (new Vector4(-Vector3.UnitZ, 1) * _cameraScene.renderConfig.invCameraViewMatrix).Xyz.Normalized();
            //some_name code end

            float dot = Vector3.Dot (cameraDir, beam.directionWorld());
			dot = Math.Max (dot, 0f);
			float interferenceWidth = middleWidth * laserParams.middleInterferenceScale;

			GL.Color4 (1f, 1f, 1f, beam.periodicIntensity * beam.periodicIntensity);

            _updateMiddleMesh (laserLength, middleWidth);

			#if true
			// stretched middle background sprite
			if (middleBackgroundSprite != null) {

				GL.Material(MaterialFace.Front, MaterialParameter.Emission, laserParams.backgroundColor);
				GL.BindTexture (TextureTarget.Texture2D, middleBackgroundSprite.TextureID);

                foreach (var oriX in xOrientationPresets) 
                {
                    Matrix4 rotated = oriX * middlePlacementMat;
                    GL.LoadMatrix (ref rotated);
                    _middleMesh.renderMesh (renderConfig);
                }
			}
			#endif
			#if true
			// stretched middle overlay sprite
			if (middleOverlayTexture != null) {
				GL.Material(MaterialFace.Front, MaterialParameter.Emission, laserParams.overlayColor);
				GL.BindTexture (TextureTarget.Texture2D, middleOverlayTexture.TextureID);

				_middleMesh.renderMesh (renderConfig);

                foreach (var oriX in xOrientationPresets) 
                {
                    Matrix4 rotated = oriX * middlePlacementMat;
                    GL.LoadMatrix (ref rotated);
                    _middleMesh.renderMesh (renderConfig);
                }
			}
			#endif
			#if true
			// interference sprite with a moving U-coordinate offset
			if (laserParams.middleInterferenceScale > 0f && interferenceTexture != null)
			{
                _updateInterfernenceVertices(laserLength, interferenceWidth);

				GL.Material(MaterialFace.Front, MaterialParameter.Emission, laserParams.middleInterferenceColor);
				//GL.BindTexture(TextureTarget.Texture2D, interferenceSprite.TextureID);
				GL.BindTexture(TextureTarget.Texture2D, interferenceTexture.TextureID);
                var scaleMat = Matrix4.CreateScale(laserLength + middleWidth/2f, interferenceWidth, 1f);

                foreach (var oriX in xOrientationPresets) {
                    Matrix4 rotated = scaleMat * oriX * middlePlacementMat;
                    GL.LoadMatrix(ref rotated);
                    _interferenceMesh.renderMesh(renderConfig);
                }
			}
			#endif
		}

		protected void _initMiddleMesh()
		{
			float padding = _laser.parameters.middleSpritePadding;
			_middleVertices = new SSVertex_PosTex[8];
			_middleVertices [0].TexCoord = new Vector2 (padding, padding);
			_middleVertices [1].TexCoord = new Vector2 (padding, 1f-padding);

			_middleVertices [2].TexCoord = new Vector2 (1f-padding, padding);
			_middleVertices [3].TexCoord = new Vector2 (1f-padding, 1f-padding);

			_middleVertices [4].TexCoord = new Vector2 (1f-padding, padding);
			_middleVertices [5].TexCoord = new Vector2 (1f-padding, 1f-padding);

			_middleVertices [6].TexCoord = new Vector2 (1f, padding);
			_middleVertices [7].TexCoord = new Vector2 (1f, 1f-padding);

			_middleMesh = new SSIndexedMesh<SSVertex_PosTex>(null, _middleIndices);
		}

		protected void _updateMiddleMesh(float laserLength, float meshWidth)
		{
            //meshWidth = 1f;

			float halfWidth = meshWidth / 2f;
			float halfLength = laserLength / 2f;

			for (int i = 0; i < 8; i += 2) {
				_middleVertices [i].Position.Y = +halfWidth;
				_middleVertices [i + 1].Position.Y = -halfWidth;
			}

			_middleVertices [0].Position.X = _middleVertices[1].Position.X = -halfLength - halfWidth;
			_middleVertices [2].Position.X = _middleVertices[3].Position.X = -halfLength + halfWidth;
			_middleVertices [4].Position.X = _middleVertices[5].Position.X = +halfLength - halfWidth;
			_middleVertices [6].Position.X = _middleVertices[7].Position.X = +halfLength + halfWidth;

			_middleMesh.updateVertices (_middleVertices);
		}

		protected void _initInterferenceVertices()
		{
			_interferenceVertices = new SSVertex_PosTex[4];
			_interferenceMesh = new SSIndexedMesh<SSVertex_PosTex> (null, _interferenceIndices);
	        
			_interferenceVertices[0].Position = new Vector3(-0.5f, +0.5f, 0f);
			_interferenceVertices[1].Position = new Vector3(-0.5f, -0.5f, 0f);
			_interferenceVertices[2].Position = new Vector3(+0.5f, +0.5f, 0f);
			_interferenceVertices[3].Position = new Vector3(+0.5f, -0.5f, 0f);

			_interferenceVertices[0].TexCoord.Y = _interferenceVertices[2].TexCoord.Y = 1f;
			_interferenceVertices[1].TexCoord.Y = _interferenceVertices[3].TexCoord.Y = 0f;
		}

		protected void _updateInterfernenceVertices(float laserLength, float interferenceWidth)
		{
			var beam = _laser.beam (_beamId);
			if (beam == null) return;

			float vScale = (interferenceWidth != 0f) ? (laserLength / interferenceWidth) : 0f;

			_interferenceVertices [0].TexCoord.X = _interferenceVertices [1].TexCoord.X
				=  beam.interferenceOffset * vScale;
			_interferenceVertices [2].TexCoord.X = _interferenceVertices [3].TexCoord.X
				= (beam.interferenceOffset + 1f) * vScale;
			_interferenceMesh.updateVertices (_interferenceVertices);
		}
	}
}

