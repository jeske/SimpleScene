	using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene.Demos
{
	public class SLaserBeamMiddleObject : SSObject
	{
        // TODO restructure to use one texture?

		protected static readonly UInt16[] _middleIndices = {
			0,1,2, 1,3,2, // left cap
			2,3,4, 3,5,4, // middle
			//4,5,6, 5,7,6  // right cap?
		};
		protected static readonly UInt16[] _interferenceIndices = {
			0,1,2, 1,3,2
		};

		#region per-frame data sources
		protected readonly SLaser _laser;
		protected readonly int _beamId;
		protected readonly SSScene _cameraScene;
		#endregion

		#region stretched middle sprites
		public SSTexture middleBackgroundSprite = null;
		public SSTexture middleOverlaySprite = null;
		protected SSVertex_PosTex[] _middleVertices;
		protected SSIndexedMesh<SSVertex_PosTex> _middleMesh;
		#endregion

		#region start-only radial sprites
		public SSTexture[] flareBackgroundSprites = null;
		public SSTexture[] flareOverlaySprites = null;
		#endregion

		#region interference sprite
		public SSTexture interferenceSprite = null;
		protected SSVertex_PosTex[] _interferenceVertices;
		protected SSIndexedMesh<SSVertex_PosTex> _interferenceMesh;
		#endregion

		// TODO cache these computations
		public override Vector3 localBoundingSphereCenter {
			get {
				Vector3 middleWorld = (_laser.sourcePos() + _laser.destPos()) / 2f;
				return Vector3.Transform (middleWorld, this.worldMat.Inverted ());
			}
		}

		// TODO cache these computations
		public override float localBoundingSphereRadius {
			get {
				Vector3 diff = (_laser.destPos() - _laser.sourcePos());
				return diff.LengthFast/2f;
			}
		}

		public SLaserBeamMiddleObject (SLaser laser,
									   int beamId,
									   SSScene cameraScene = null,
								       SSTexture middleBackgroundSprite = null,
									   SSTexture middleOverlaySprite = null,
									   SSTexture[] flareBackgroundSprites = null,
									   SSTexture[] flareOverlaySprites = null,
									   SSTexture inteferenceSprite = null)
		{
			this._laser = laser;
			this._beamId = beamId;
			this._cameraScene = cameraScene;

            this.renderState.castsShadow = false;
            this.renderState.receivesShadows = false;
            this.renderState.depthTest = true;
            this.renderState.depthWrite = false;
            this.renderState.alphaBlendingOn = true;
            this.renderState.blendFactorSrc = BlendingFactorSrc.SrcAlpha;
            this.renderState.blendFactorDest = BlendingFactorDest.One;

            // reset all mat colors. emission will be controlled during rendering
            this.AmbientMatColor = new Color4(0f, 0f, 0f, 0f);
            this.DiffuseMatColor = new Color4(0f, 0f, 0f, 0f);
            this.SpecularMatColor = new Color4(0f, 0f, 0f, 0f);
            this.EmissionMatColor = new Color4(0f, 0f, 0f, 0f);


			var ctx = new SSAssetManager.Context ("./lasers");
			this.middleBackgroundSprite = middleBackgroundSprite 
				?? SSAssetManager.GetInstance<SSTextureWithAlpha>(ctx, "middleBackground.png");
			this.middleOverlaySprite = middleOverlaySprite
				?? SSAssetManager.GetInstance<SSTextureWithAlpha>(ctx, "middleOverlay.png");
			this.interferenceSprite = interferenceSprite
				?? SSAssetManager.GetInstance<SSTextureWithAlpha> (ctx, "interference.png");

			if (flareBackgroundSprites != null && flareBackgroundSprites.Length > 0) { 
				this.flareBackgroundSprites = flareBackgroundSprites;
			} else {
				this.flareBackgroundSprites = new SSTexture[1];
				this.flareBackgroundSprites[0]
					= SSAssetManager.GetInstance<SSTextureWithAlpha>(ctx, "flareBackground.png");
			}

			if (flareOverlaySprites != null && flareOverlaySprites.Length > 0) { 
				this.flareOverlaySprites = flareOverlaySprites;
			} else {
				this.flareOverlaySprites = new SSTexture[1];
				this.flareOverlaySprites[0]
					= SSAssetManager.GetInstance<SSTextureWithAlpha>(ctx, "flareOverlay.png");
			}

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

			var startView = Vector3.Transform(beam.startPos, renderConfig.invCameraViewMatrix);
			var endView = Vector3.Transform (beam.endPos, renderConfig.invCameraViewMatrix);
			var middleView = (startView + endView) / 2f;

			// step: draw middle section:
			Vector3 diff = endView - startView;
			float diff_xy = diff.Xy.LengthFast;
			float phi = -(float)Math.Atan2 (diff.Z, diff_xy);
			float theta = (float)Math.Atan2 (diff.Y, diff.X);
			Matrix4 backgroundOrientMat = Matrix4.CreateRotationY (phi) * Matrix4.CreateRotationZ (theta);
			Matrix4 middlePlacementMat = backgroundOrientMat * Matrix4.CreateTranslation (middleView);
			Matrix4 startPlacementMat = Matrix4.CreateTranslation (startView);

			float laserLength = diff.LengthFast;
			float middleWidth = laserParams.backgroundWidth * _laser.envelopeIntensity;

			Vector3 cameraDir = Vector3.Transform(
				-Vector3.UnitZ, _cameraScene.renderConfig.invCameraViewMatrix).Normalized();
			float dot = Vector3.Dot (cameraDir, _laser.direction());
			dot = Math.Max (dot, 0f);
			float interferenceWidth = middleWidth * laserParams.interferenceScale;

			GL.Color4 (1f, 1f, 1f, beam.periodicIntensity * beam.periodicIntensity);

			#if true
			// stretched middle background sprite
			if (middleBackgroundSprite != null) {
				GL.Material(MaterialFace.Front, MaterialParameter.Emission, laserParams.backgroundColor);
				GL.BindTexture (TextureTarget.Texture2D, middleBackgroundSprite.TextureID);
				GL.LoadMatrix (ref middlePlacementMat);

				_updateMiddleMesh (laserLength, middleWidth);
				_middleMesh.renderMesh (renderConfig);
			}
			#endif
			#if true
			// stretched middle overlay sprite
			if (middleOverlaySprite != null) {
				GL.Material(MaterialFace.Front, MaterialParameter.Emission, laserParams.overlayColor);
				GL.BindTexture (TextureTarget.Texture2D, middleOverlaySprite.TextureID);
				GL.LoadMatrix (ref middlePlacementMat);

				_updateMiddleMesh (laserLength, middleWidth);
				_middleMesh.renderMesh (renderConfig);			
			}
			#endif
			#if false
			// start radial flare background sprites
			if (flareBackgroundSprites != null) {
				GL.Material(MaterialFace.Front, MaterialParameter.Emission, _laser.parameters.backgroundColor);
				var mat = Matrix4.CreateScale (flareSpriteWidth, flareSpriteWidth, 1f) * startPlacementMat;
				GL.LoadMatrix (ref mat);
				foreach (var tex in flareBackgroundSprites) {
					GL.BindTexture (TextureTarget.Texture2D, tex.TextureID);
					SSTexturedQuad.SingleFaceInstance.DrawArrays (renderConfig, PrimitiveType.Triangles);
				}
			}
			#endif
			#if false
			// start radial flare overlay sprites
			if (flareOverlaySprites != null) {
				GL.Material(MaterialFace.Front, MaterialParameter.Emission, _laser.parameters.overlayColor);
				var mat = Matrix4.CreateScale (flareSpriteWidth, flareSpriteWidth, 1f) * startPlacementMat;
				GL.LoadMatrix (ref mat);
				foreach (var tex in flareOverlaySprites) {
					GL.BindTexture (TextureTarget.Texture2D, tex.TextureID);
					SSTexturedQuad.SingleFaceInstance.DrawArrays (renderConfig, PrimitiveType.Triangles);
				}
			}
			#endif
			#if true
			// interference sprite with a moving U-coordinate offset
			if (laserParams.interferenceScale > 0f && interferenceSprite != null)
			{
				GL.Material(MaterialFace.Front, MaterialParameter.Emission, laserParams.interferenceColor);
				//GL.BindTexture(TextureTarget.Texture2D, interferenceSprite.TextureID);
				GL.BindTexture(TextureTarget.Texture2D, interferenceSprite.TextureID);
				var mat = Matrix4.CreateScale(laserLength + middleWidth/2f, interferenceWidth, 1f) * middlePlacementMat;
				GL.LoadMatrix(ref mat);

				_updateInterfernenceVertices(laserLength, interferenceWidth);
				_interferenceMesh.renderMesh(renderConfig);
			}
			#endif
		}

		protected void _initMiddleMesh()
		{
			float padding = _laser.parameters.laserSpritePadding;
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

			_middleMesh.UpdateVertices (_middleVertices);
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
			_interferenceMesh.UpdateVertices (_interferenceVertices);
		}
	}
}

