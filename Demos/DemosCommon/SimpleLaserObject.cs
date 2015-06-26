using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using SimpleScene.Util;

namespace SimpleScene
{
	// TODO have an easy way to update laser start and finish
	// TODO ability to "hold" laser active

	// TODO pulse, interference effects
	// TODO laser "drift"

	public class SSLaserParameters
	{
		public Color4 backgroundColor = Color4.White;
		public Color4 overlayColor = Color4.White;
		public float backgroundWidth = 0.1f;    // width in world units
		public float overlayWidthRatio = 0.3f;  // ratio, 0-1, of how much of the background is covered by overlay
	}

	public class SSLaser
	{
		public Vector3 start;
		public Vector3 end;
		public SSLaserParameters parameters;

		public float laserOpacity;
	}

	public class SimpleLaserObject : SSObject
	{
		public SSLaser laser = null;
		public SSTexture backgroundMiddleSprite = null;
		public SSTexture backgroundCapSprite = null;
		public SSTexture middleOverlaySprite = null;
		public SSTexture startBackgroundSprite = null;
		public SSTexture startOverlaySprite = null;

		// TODO cache these computations
		public override Vector3 localBoundingSphereCenter {
			get {
				if (laser == null) {
					return Vector3.Zero;
				}
				Vector3 middleWorld = (laser.start + laser.end) / 2f;
				return Vector3.Transform (middleWorld, this.worldMat.Inverted ());
			}
		}

		// TODO cache these computations
		public override float localBoundingSphereRadius {
			get {
				if (laser == null) {
					return 0f;
				}
				Vector3 diff = (laser.end - laser.start);
				return diff.LengthFast/2f;
			}
		}

		public SimpleLaserObject (SSLaser laser = null, 
							      SSTexture middleBackgroundSprite = null,
								  SSTexture middleOverlaySprite = null,
								  SSTexture startBackgroundSprite = null,
								  SSTexture startOverlaySprite = null)
		{
			this.laser = laser;

			this.renderState.castsShadow = false;
			this.renderState.receivesShadows = false;

			var ctx = new SSAssetManager.Context ("./lasers");
			this.backgroundMiddleSprite = middleBackgroundSprite 
				?? SSAssetManager.GetInstance<SSTextureWithAlpha>(ctx, "background_middle.png");
			this.backgroundCapSprite =
				SSAssetManager.GetInstance<SSTextureWithAlpha> (ctx, "background_cap.png");

			this.middleOverlaySprite = middleOverlaySprite
				?? SSAssetManager.GetInstance<SSTextureWithAlpha>(ctx, "laserOverlayStatic.png");
			this.startBackgroundSprite = startBackgroundSprite
				?? SSAssetManager.GetInstance<SSTextureWithAlpha> (ctx, "start_background.png");
			this.startOverlaySprite = startOverlaySprite
				?? SSAssetManager.GetInstance<SSTextureWithAlpha> (ctx, "start_overlay.png");

			this.AmbientMatColor = new Color4(0f, 0f, 0f, 0f);
			this.DiffuseMatColor = new Color4(0f, 0f, 0f, 0f);
			this.SpecularMatColor = new Color4(0f, 0f, 0f, 0f);
			this.EmissionMatColor = new Color4(0f, 0f, 0f, 0f);
		}

		public override void Render(SSRenderConfig renderConfig)
		{

			base.Render (renderConfig);

			// step: setup render settings
			GL.Enable(EnableCap.Blend);

			SSShaderProgram.DeactivateAll ();
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.Enable (EnableCap.Texture2D);

			// step: compute endpoints in view space
			var startView = Vector3.Transform(laser.start, renderConfig.invCameraViewMatrix);
			var endView = Vector3.Transform (laser.end, renderConfig.invCameraViewMatrix);
			var middleView = (startView + endView) / 2f;

			// step: draw middle section:
			Vector3 diff = endView - startView;
			float laserLength = diff.LengthFast;
			float diff_xy = diff.Xy.LengthFast;
			float phi = -(float)Math.Atan2 (diff.Z, diff_xy);
			float theta = (float)Math.Atan2 (diff.Y, diff.X);
			Matrix4 backgroundOrientMat = Matrix4.CreateRotationY (phi) * Matrix4.CreateRotationZ (theta);
			Matrix4 middlePlacementMat = backgroundOrientMat * Matrix4.CreateTranslation (middleView);
			Matrix4 startCapPlacement = backgroundOrientMat * Matrix4.CreateTranslation (startView + new Vector3(+0.075f, 0f, 0f));

			float backgroundWidth = laser.parameters.backgroundWidth;
			float overlayWidth = laser.parameters.backgroundWidth * laser.parameters.overlayWidthRatio;

			GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
			//GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			//GL.BlendEquationSeparate(BlendEquationMode.FuncAdd, BlendEquationMode.FuncAdd);
			//GL.BlendFuncSeparate (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One,
			//					  BlendingFactorSrc.One, BlendingFactorDest.Zero);

			if (backgroundMiddleSprite != null) {
				
				GL.BindTexture (TextureTarget.Texture2D, backgroundMiddleSprite.TextureID);
				//GL.Color4 (laser.parameters.backgroundColor);
				GL.Material(MaterialFace.Front, MaterialParameter.Emission, laser.parameters.backgroundColor);

				Matrix4 middlebackgroundScale = Matrix4.CreateScale (laserLength, backgroundWidth, 1f);
				Matrix4 middleBackgroundMatrix = middlebackgroundScale * middlePlacementMat;
				GL.LoadMatrix (ref middleBackgroundMatrix);

				SSTexturedQuad.SingleFaceInstance.DrawArrays (renderConfig, PrimitiveType.Triangles);

				// caps
				Matrix4 capBackgroundScale = Matrix4.CreateScale (backgroundWidth, backgroundWidth, 1f);
				if (backgroundCapSprite != null) {
					GL.BindTexture (TextureTarget.Texture2D, backgroundCapSprite.TextureID);
					Matrix4 startCapBackgroundMatrix = capBackgroundScale * startCapPlacement;
					GL.LoadMatrix (ref startCapBackgroundMatrix);
					SSTexturedQuad.SingleFaceInstance.DrawArrays (renderConfig, PrimitiveType.Triangles);
				}
			}
			#if false
			if (middleOverlaySprite != null) {
				GL.BindTexture (TextureTarget.Texture2D, middleOverlaySprite.TextureID);
				//GL.Color4 (laser.parameters.overlayColor);
				GL.Material(MaterialFace.Front, MaterialParameter.Emission, laser.parameters.overlayColor);

				Matrix4 middleOverlayScale = Matrix4.CreateScale (
					laserLength, overlayWidth, 1f);
				Matrix4 middleOverlayMatrix = middleOverlayScale * middlePlacementMat
					* Matrix4.CreateTranslation(0f, 0f, 0.1f);
				GL.LoadMatrix (ref middleOverlayMatrix);

				SSTexturedQuad.SingleFaceInstance.DrawArrays (renderConfig, PrimitiveType.Triangles);
			}
			#endif

			// step: start section
			Matrix4 startPlacementMatrix = 
				//Matrix4.CreateRotationY(phi) * Matrix4.CreateRotationZ(theta) *
				Matrix4.CreateTranslation (startView);
			//GL.BlendEquationSeparate (BlendEquationMode.FuncAdd, BlendEquationMode.FuncAdd);
			//GL.BlendFuncSeparate (BlendingFactorSrc.Zero, BlendingFactorDest.DstColor,
			//	BlendingFactorSrc.Zero, BlendingFactorDest.Zero);
			//GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			//GL.BlendEquation (BlendEquationMode.Max);

			//GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.Zero);
			//GL.BlendEquation (BlendEquationMode.Max);


			//GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);

			#if false
			if (startBackgroundSprite != null) {
				GL.BindTexture (TextureTarget.Texture2D, startBackgroundSprite.TextureID);
				//GL.Color4 (laser.parameters.backgroundColor);
				GL.Material(MaterialFace.Front, MaterialParameter.Emission, laser.parameters.backgroundColor);

				Matrix4 startBackgroundScale = Matrix4.CreateScale (
					backgroundWidth*1.5f, backgroundWidth*1.5f, 1f);
				Matrix4 startBackgroundMatrix = startBackgroundScale * startPlacementMatrix;
				GL.LoadMatrix (ref startBackgroundMatrix);

				SSTexturedQuad.SingleFaceInstance.DrawArrays (renderConfig, PrimitiveType.Triangles);
			}
			#endif
			#if false
			if (startOverlaySprite != null) {
				GL.BindTexture (TextureTarget.Texture2D, startOverlaySprite.TextureID);
				//GL.Color4 (laser.parameters.backgroundColor);
				GL.Material(MaterialFace.Front, MaterialParameter.Emission, laser.parameters.overlayColor);

				Matrix4 startOverlayScale = Matrix4.CreateScale (
					overlayWidth, overlayWidth, 1f);
				Matrix4 startOverlayMatrix = startOverlayScale * startPlacementMatrix;
				GL.LoadMatrix (ref startOverlayMatrix);

				SSTexturedQuad.SingleFaceInstance.DrawArrays (renderConfig, PrimitiveType.Triangles);
			}
			#endif



			#if false
			if (startBackgroundSprite != null) {
			GL.BindTexture (TextureTarget.Texture2D, startBackgroundSprite.TextureID);
			//GL.Color4 (laser.parameters.backgroundColor);
			GL.Material(MaterialFace.Front, MaterialParameter.Emission, laser.parameters.backgroundColor);

			Matrix4 startBackgroundScale = Matrix4.CreateScale (
			laser.parameters.backgroundWidth, laser.parameters.backgroundWidth, 1f);
			Matrix4 startBackgroundMatrix = startBackgroundScale * startPlacementMatrix;
			GL.LoadMatrix (ref startBackgroundMatrix);

			SSTexturedQuad.SingleFaceInstance.DrawArrays (renderConfig, PrimitiveType.Triangles);
			}
			#endif
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

