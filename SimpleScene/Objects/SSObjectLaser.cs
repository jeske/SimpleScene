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
		public Color4 backgroundColor = Color4.Purple;
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

	public class SSObjectLaser : SSObject
	{
		public SSLaser laser = null;
		public SSTexture middleBackgroundSprite = null;
		public SSTexture middleOverlaySprite = null;
		public SSTexture startBackgroundSprite = null;
		public SSTexture startOverlaySprite = null;


		public SSObjectLaser (SSLaser laser = null)
		{
			this.laser = laser;

			this.renderState.castsShadow = false;
			this.renderState.receivesShadows = false;
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

		public override void Render(SSRenderConfig renderConfig)
		{
			base.Render (renderConfig);

			// step: compute endpoints in view space
			var startView = Vector3.Transform(laser.start, renderConfig.invCameraViewMatrix);
			var endView = Vector3.Transform (laser.end, renderConfig.invCameraViewMatrix);
			var middleView = (startView + endView) / 2f;

			// step: setup render settings
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);

			GL.MatrixMode (MatrixMode.Modelview);
			SSShaderProgram.DeactivateAll ();

			// step: draw middle section:

			// theta and phi
			Vector3 diff = endView - startView;
			float diff_xy = diff.Xy.LengthFast;
			float phi = (float)Math.Atan2 (diff.Z, diff_xy);
			float theta = (float)Math.Atan2 (diff.Y, diff.X);
			Matrix4 middlePlacementMat = 
				Matrix4.CreateTranslation(middleView)
				* Matrix4.CreateRotationZ (theta) * Matrix4.CreateRotationY (phi);


			float laserLength = diff.LengthSquared;

			if (middleBackgroundSprite != null) {
				GL.BindTexture (TextureTarget.Texture2D, middleBackgroundSprite.TextureID);

				Matrix4 middlebackgroundScale = Matrix4.CreateScale (
					laserLength, laser.parameters.backgroundWidth, 1f);
				Matrix4 middleBackgroundMatrix = middlePlacementMat * middlebackgroundScale;
				GL.LoadMatrix (ref middleBackgroundMatrix);

				// TODO single fance instance
				SSTexturedQuad.DoubleFaceInstance.DrawArrays (renderConfig, PrimitiveType.Triangles);
			}
			if (middleOverlaySprite != null) {
				GL.BindTexture (TextureTarget.Texture2D, middleOverlaySprite.TextureID);

				Matrix4 middleOverlayScale = Matrix4.CreateScale (
                     laserLength, laser.parameters.backgroundWidth * laser.parameters.overlayWidthRatio, 1f);
				Matrix4 middleOverlayMatrix = middlePlacementMat * middleOverlayScale;
				GL.LoadMatrix (ref middleOverlayMatrix);

				// TODO single fance instance
				SSTexturedQuad.DoubleFaceInstance.DrawArrays (renderConfig, PrimitiveType.Triangles);
			}






		}
	}
}

