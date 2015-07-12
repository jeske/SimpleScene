	using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene.Demos
{
	public class SimpleLaserObject : SSObject
	{
		static readonly protected UInt16[] _middleIndices = {
			0,1,2, 1,3,2, // left cap
			2,3,4, 3,5,4, // middle
			//4,5,6, 5,7,6  // right cap?
		};
		static readonly protected UInt16[] _interferenceIndices = {
			0,1,2, 1,3,2
		};

		public SimpleLaser laser = null;
		public SSScene cameraScene = null;

		#region intensity (alpha component strength)
		protected float _localT = 0f;
		protected float _periodicIntensity = 1f;
		protected float _envelopeIntensity = 1f;
		#endregion

		#region periodic world-coordinate drift
		// TODO tie to target surface mesh instead of world coordinates?
		protected float _driftX = 0f;
		protected float _driftY = 0f;
		#endregion

		#region stretched middle sprites
		public SSTexture middleBackgroundSprite = null;
		public SSTexture middleOverlaySprite = null;
		protected SSVertex_PosTex[] _middleVertices;
		protected SSIndexedMesh<SSVertex_PosTex> _middleMesh;
		#endregion

		#region start-only radial sprites
		public SSTexture startBackgroundSprite = null;
		public SSTexture startOverlaySprite = null;
		#endregion

		#region interference sprite
		public SSTexture interferenceSprite = null;
		protected SSVertex_PosTex[] _interferenceVertices;
		protected SSIndexedMesh<SSVertex_PosTex> _interferenceMesh;
		protected float _interferenceOffset = 0f; // TODO randomize?
		#endregion

		// TODO cache these computations
		public override Vector3 localBoundingSphereCenter {
			get {
				if (laser == null) {
					return Vector3.Zero;
				}
				Vector3 middleWorld = (laser.sourcePos() + laser.destPos()) / 2f;
				return Vector3.Transform (middleWorld, this.worldMat.Inverted ());
			}
		}

		// TODO cache these computations
		public override float localBoundingSphereRadius {
			get {
				if (laser == null) {
					return 0f;
				}
				Vector3 diff = (laser.destPos() - laser.sourcePos());
				return diff.LengthFast/2f;
			}
		}

		public SimpleLaserObject (SimpleLaser laser = null,
								  SSScene cameraScene = null,
							      SSTexture middleBackgroundSprite = null,
								  SSTexture middleOverlaySprite = null,
								  SSTexture startBackgroundSprite = null,
								  SSTexture startOverlaySprite = null,
								  SSTexture inteferenceSprite = null)
		{
			this.laser = laser;
			this.cameraScene = cameraScene;

			this.renderState.castsShadow = false;
			this.renderState.receivesShadows = false;

			var ctx = new SSAssetManager.Context ("./lasers");
			this.middleBackgroundSprite = middleBackgroundSprite 
				?? SSAssetManager.GetInstance<SSTextureWithAlpha>(ctx, "start2.png");
			this.middleOverlaySprite = middleOverlaySprite
				?? SSAssetManager.GetInstance<SSTextureWithAlpha>(ctx, "start2Over.png");
			this.startBackgroundSprite = startBackgroundSprite
				?? SSAssetManager.GetInstance<SSTextureWithAlpha>(ctx, "background.png");
			this.startOverlaySprite = startOverlaySprite
				?? SSAssetManager.GetInstance<SSTextureWithAlpha>(ctx, "start_overlay.png");
			this.interferenceSprite = interferenceSprite
				?? SSAssetManager.GetInstance<SSTextureWithAlpha> (ctx, "laseroverlay01.png");

			// reset all mat colors. emission will be controlled during rendering
			this.AmbientMatColor = new Color4(0f, 0f, 0f, 0f);
			this.DiffuseMatColor = new Color4(0f, 0f, 0f, 0f);
			this.SpecularMatColor = new Color4(0f, 0f, 0f, 0f);
			this.EmissionMatColor = new Color4(0f, 0f, 0f, 0f);

			// initialize non-changing vertex data
			_initMiddleMesh ();
			_initInterferenceVertices ();
		}

		public override void Render(SSRenderConfig renderConfig)
		{
			base.Render (renderConfig);

			// step: setup render settings
			SSShaderProgram.DeactivateAll ();
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.Enable (EnableCap.Texture2D);

			GL.Enable(EnableCap.Blend);
			GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);

			Vector3 laserStart = laser.sourcePos ();
			Vector3 laserEnd = laser.destPos ();
			Vector3 targetDir = (laserEnd - laserStart);
			Vector3 driftXAxis, driftYAxis;
			OpenTKHelper.TwoPerpAxes (targetDir, out driftXAxis, out driftYAxis);

			Vector3 driftedEnd = laserEnd + _driftX * driftXAxis + _driftY * driftYAxis;

			// step: compute endpoints in view space
			var startView = Vector3.Transform(laserStart, renderConfig.invCameraViewMatrix);
			var endView = Vector3.Transform (driftedEnd, renderConfig.invCameraViewMatrix);
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
			float middleWidth = laser.parameters.backgroundWidth;

			Vector3 laserDir = (laserEnd - laserStart).Normalized ();
			Vector3 cameraDir = Vector3.Transform(
				-Vector3.UnitZ, cameraScene.renderConfig.invCameraViewMatrix).Normalized();
			float dot = Vector3.Dot (cameraDir, laserDir);
			dot = Math.Max (dot, 0f);
			float startWidth = middleWidth * laser.parameters.startPointScale * (1f-dot);

			float interferenceWidth = middleWidth * laser.parameters.interferenceScale;

			GL.Color4 (1f, 1f, 1f, _periodicIntensity * _envelopeIntensity);

			#if true
			if (middleBackgroundSprite != null) {
				GL.Material(MaterialFace.Front, MaterialParameter.Emission, laser.parameters.backgroundColor);
				GL.BindTexture (TextureTarget.Texture2D, middleBackgroundSprite.TextureID);
				GL.LoadMatrix (ref middlePlacementMat);

				_updateMiddleMesh (laserLength, middleWidth);
				_middleMesh.renderMesh (renderConfig);

				#if true
				if (startBackgroundSprite != null) {
					GL.BindTexture (TextureTarget.Texture2D, startBackgroundSprite.TextureID);
					var mat = Matrix4.CreateScale (startWidth, startWidth, 1f)
					               * startPlacementMat;
					GL.LoadMatrix (ref mat);
					SSTexturedQuad.SingleFaceInstance.DrawArrays (renderConfig, PrimitiveType.Triangles);
				}
				#endif
			}
			#endif
			#if true
			if (middleOverlaySprite != null) {
				GL.Material(MaterialFace.Front, MaterialParameter.Emission, laser.parameters.overlayColor);
				GL.BindTexture (TextureTarget.Texture2D, middleOverlaySprite.TextureID);
				GL.LoadMatrix (ref middlePlacementMat);

				_updateMiddleMesh (laserLength, middleWidth);
				_middleMesh.renderMesh (renderConfig);
				
				#if true
				if (startOverlaySprite != null) {
					GL.BindTexture (TextureTarget.Texture2D, startOverlaySprite.TextureID);
					var mat = Matrix4.CreateScale (startWidth, startWidth, 1f)
						* startPlacementMat;
					GL.LoadMatrix (ref mat);
					SSTexturedQuad.SingleFaceInstance.DrawArrays (renderConfig, PrimitiveType.Triangles);
				}
				#endif
			}
			#endif
			#if true
			if (laser.parameters.interferenceScale > 0f && interferenceSprite != null)
			{
				GL.Material(MaterialFace.Front, MaterialParameter.Emission, laser.parameters.interferenceColor);
				//GL.BindTexture(TextureTarget.Texture2D, interferenceSprite.TextureID);
				GL.BindTexture(TextureTarget.Texture2D, interferenceSprite.TextureID);
				var mat = Matrix4.CreateScale(laserLength + startWidth, interferenceWidth, 1f) * middlePlacementMat;
				GL.LoadMatrix(ref mat);

				_updateInterfernenceVertices(laserLength, interferenceWidth);
				_interferenceMesh.renderMesh(renderConfig);
			}
			#endif
		}

		public override void Update (float fElapsedS)
		{
			_interferenceOffset -= laser.parameters.interferenceVelocity * fElapsedS;
			if (_interferenceOffset >= 1f || _interferenceOffset < 0f) {
				_interferenceOffset %= 1f;
			}

			// compute local time
			_localT += fElapsedS;

			// envelope intensity
			if (laser.parameters.intensityEnvelope != null) {
				var envelope = laser.localIntensityEnvelope;
				if (laser.releaseDirty == true) {
					// hacky way to have on-demand release and still use ADSR envelope
					envelope.attackDuration = 0f;
					envelope.decayDuration = 0f;
					envelope.sustainDuration = _localT;
					laser.releaseDirty = false;
				}
				_envelopeIntensity = envelope.computeLevel (_localT);
				if (_localT > envelope.totalDuration && laser.postReleaseFunc != null) {
					laser.postReleaseFunc (laser);
				}
			} else {
				_envelopeIntensity = 1f;
			}

			// periodic intensity
			float periodicT = _localT * laser.parameters.intensityFrequency;
			if (laser.parameters.intensityPeriodicFunction != null) {
				_periodicIntensity = laser.parameters.intensityPeriodicFunction (periodicT);
			} else {
				_periodicIntensity = 1f;
			}

			if (laser.parameters.intensityModulation != null) {
				_periodicIntensity *= laser.parameters.intensityModulation (periodicT);
			}

			// periodic world-coordinate drift
			if (laser.parameters.driftXFunc != null) {
				_driftX = laser.parameters.driftXFunc (_localT);
			} else {
				_driftX = 0f;
			}

			if (laser.parameters.driftYFunc != null) {
				_driftY = laser.parameters.driftYFunc (_localT);
			} else {
				_driftY = 0f;
			}

			if (laser.parameters.driftModulationFunc != null) {
				var driftMod = laser.parameters.driftModulationFunc (_localT);
				_driftX *= driftMod;
				_driftY *= driftMod;
			}
		}

		protected void _initMiddleMesh()
		{
			float padding = laser.parameters.laserSpritePadding;
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
			float vScale = (interferenceWidth != 0f) ? (laserLength / interferenceWidth) : 0f;

			_interferenceVertices [0].TexCoord.X = _interferenceVertices [1].TexCoord.X
				= _interferenceOffset * vScale;
			_interferenceVertices [2].TexCoord.X = _interferenceVertices [3].TexCoord.X
				= (_interferenceOffset + 1f) * vScale;
			_interferenceMesh.UpdateVertices (_interferenceVertices);
		}

		#if false
		/// <summary>
		/// Adds the laser to a list of lasers. Laser start, ends, fade and other effects
		/// are to be updated from somewhere else.
		/// </summary>
		/// <returns>The handle to LaserInfo which can be used for updating start and 
		/// end.</returns>
		public SSLaser addLaser(SSLaserParameters parameters)
		{
		var li = new SSLaser();
		li.start = Vector3.Zero;
		li.end = Vector3.Zero;
		li.parameters = parameters;
		_lasers.Add (li);
		return li;
		}

		public void removeLaser(SSLaser laser)
		{
		_lasers.Remove (laser);
		}
		#endif
	}
}

