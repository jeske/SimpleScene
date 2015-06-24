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
		public SSTexture middleBackgroundSprite = null;
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
								  SSTexture middleOverlaySprite = null)
		{
			this.laser = laser;

			this.renderState.castsShadow = false;
			this.renderState.receivesShadows = false;

			this.middleBackgroundSprite = middleBackgroundSprite 
				?? SSAssetManager.GetInstance<SSTextureWithAlpha>("./lasers", "laser.png");
				//?? SSAssetManager.GetInstance<SSTextureWithAlpha>("./boneman", "skin.png");
			this.middleOverlaySprite = middleOverlaySprite
				?? SSAssetManager.GetInstance<SSTextureWithAlpha>("./lasers", "laserOverlayStatic.png");
		}

		public override void Render(SSRenderConfig renderConfig)
		{

			// step: compute endpoints in view space
			var startView = Vector3.Transform(laser.start, renderConfig.invCameraViewMatrix);
			var endView = Vector3.Transform (laser.end, renderConfig.invCameraViewMatrix);
			var middleView = (startView + endView) / 2f;

			// theta and phi
			Vector3 diff = endView - startView;
			float diff_xy = diff.Xy.LengthFast;
			float phi = -(float)Math.Atan2 (diff.Z, diff_xy);
			float theta = (float)Math.Atan2 (diff.Y, diff.X);
			Matrix4 middlePlacementMat = 
				Matrix4.CreateRotationY (phi) * Matrix4.CreateRotationZ (theta)
				* Matrix4.CreateTranslation (middleView);

			float laserLength = diff.LengthFast;

			base.Render (renderConfig);

			// step: setup render settings
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
			//GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			SSShaderProgram.DeactivateAll ();
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.Enable (EnableCap.Texture2D);

			GL.DepthMask (false);

			// step: draw middle section:

			if (middleBackgroundSprite != null) {
				
				GL.BindTexture (TextureTarget.Texture2D, middleBackgroundSprite.TextureID);
				GL.Color4 (laser.parameters.backgroundColor);

				Matrix4 middlebackgroundScale = Matrix4.CreateScale (
					laserLength, laser.parameters.backgroundWidth, 1f);
				Matrix4 middleBackgroundMatrix = middlebackgroundScale * middlePlacementMat;
				GL.LoadMatrix (ref middleBackgroundMatrix);

				SSTexturedQuad.SingleFaceInstance.DrawArrays (renderConfig, PrimitiveType.Triangles);
			}
			#if true
			if (middleOverlaySprite != null) {
				GL.BindTexture (TextureTarget.Texture2D, middleOverlaySprite.TextureID);
				GL.Color4 (laser.parameters.overlayColor);

				Matrix4 middleOverlayScale = Matrix4.CreateScale (
                     laserLength, laser.parameters.backgroundWidth * laser.parameters.overlayWidthRatio, 1f);
				Matrix4 middleOverlayMatrix = middleOverlayScale * middlePlacementMat
					* Matrix4.CreateTranslation(0f, 0f, 0.1f);
				GL.LoadMatrix (ref middleOverlayMatrix);

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

