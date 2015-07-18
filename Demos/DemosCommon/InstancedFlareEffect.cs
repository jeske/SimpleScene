using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using SimpleScene;

namespace SimpleScene.Demos
{
	public class InstancedFlareEffect : SSInstancedMeshRenderer
	{
		#region Source of Per-Frame Input
		private SSScene bbScene;
		private SSObjectOcclusionQueuery bbObj;
		#endregion

		public InstancedFlareEffect (SSScene bbScene,
									 SSObjectOcclusionQueuery occObj,
									 SSTexture texture,
									 RectangleF[] spriteRects,
									 Vector2[] spriteScales = null,
                                     Color4[] colors = null
        )
			: base(new FlareSpriteData(spriteRects, spriteScales, colors),
				   SSTexturedQuad.DoubleFaceInstance,
				   BufferUsageHint.StreamDraw)
		{
			_init(bbScene, occObj, texture);
		}

		public InstancedFlareEffect (
			SSScene bbScene,
			SSObjectOcclusionQueuery bbObj,
			SSTexture texture,
			RectangleF[] spriteRects,
			float[] spriteScales,
            Color4[] colors = null
        )
			: base(new FlareSpriteData(spriteRects, spriteScales, colors),
				SSTexturedQuad.DoubleFaceInstance,
				BufferUsageHint.StreamDraw)
		{
			_init(bbScene, bbObj, texture);
		}

		protected new FlareSpriteData instanceData {
			get { return base.instanceData as FlareSpriteData; }
		}

		public override void Render (SSRenderConfig renderConfig)
		{
			int queryResult = bbObj.OcclusionQueueryResult;
			if (queryResult <= 0) return;

			// Begin the quest to update VBO vertices
			Matrix4 viewInverted = bbScene.renderConfig.invCameraViewMatrix.Inverted();
			Vector3 viewRight = Vector3.Transform(new Vector3 (1f, 0f, 0f), viewInverted);
			Vector3 viewUp = Vector3.Transform(new Vector3 (0f, 1f, 0f), viewInverted);
			Vector3 bbRightMost = bbObj.Pos + viewRight.Normalized() * bbObj.Scale.X;
			Vector3 bbTopMost = bbObj.Pos + viewUp.Normalized() * bbObj.Scale.Y;

			Matrix4 bbSceneViewProj = bbScene.renderConfig.invCameraViewMatrix * bbScene.renderConfig.projectionMatrix;
			int[] viewport = new int[4];
			GL.GetInteger(GetPName.Viewport, viewport);
			Vector2 screenOrig = new Vector2 (viewport [0], viewport [1]);
			Vector2 screenRect = new Vector2 (viewport [2], viewport [3]);
			Vector2 screenCenter = screenOrig + screenRect / 2f;
			Vector2 bbPos = worldToScreen(bbObj.Pos, screenCenter, screenRect, bbSceneViewProj);
			Vector2 bbRightMostPt = worldToScreen(bbRightMost, screenCenter, screenRect, bbSceneViewProj);
			Vector2 bbTopMostPt = worldToScreen(bbTopMost, screenCenter, screenRect, bbSceneViewProj);
			Vector2 bbRect = 2f * new Vector2 (bbRightMostPt.X - bbPos.X, bbPos.Y - bbTopMostPt.Y);

			float bbFullEstimate = (float)Math.PI * (float)bbRect.X * (float)bbRect.Y / 4f;
			float intensityFraction = Math.Min((float)queryResult / bbFullEstimate, 1f);

			var color4 = bbObj.MainColor;
			color4.A = intensityFraction;
			instanceData.colors [0].Color = Color4Helper.ToUInt32 (color4);

			instanceData.masterScales [0].Value 
				= Math.Max (bbRect.X, bbRect.Y) * Math.Min (1.5f, 1f / (1f - intensityFraction));

			_updateScreenInstanceData (screenCenter, screenRect, bbPos, bbRect);

			base.Render (renderConfig);
		}

        protected virtual void _updateScreenInstanceData(Vector2 screenCenter, Vector2 screenRect, Vector2 bbPos, Vector2 bbRect)
        {
            Vector2 towardsCenter = screenCenter - bbPos;
            int numElements = instanceData.activeBlockLength;
            for (int i = 0; i < numElements; ++i) {
                Vector2 center = bbPos + towardsCenter * 2.5f / (float)numElements * (float)i;
                instanceData.positions [i].Value.Xy = center;
            }
        }

		private Vector2 worldToScreen(Vector3 worldPos, Vector2 screenCenter, Vector2 screenRect, Matrix4 bbSceneViewProj) 
		{
			Vector4 pos = Vector4.Transform(new Vector4(worldPos, 1f), bbSceneViewProj);
			pos /= pos.W;
			pos.Y = -pos.Y;
			return screenCenter + pos.Xy * screenRect/2f;
		}

		protected void _init(SSScene bbScene,
			SSObjectOcclusionQueuery bbObj,
			SSTexture texture)
		{
			base.AmbientMatColor = new Color4 (1f, 1f, 1f, 1f);
			base.DiffuseMatColor = new Color4 (0f, 0f, 0f, 0f);
			base.EmissionMatColor = new Color4(0f, 0f, 0f, 0f);
			base.SpecularMatColor = new Color4 (0f, 0f, 0f, 0f);
			base.ShininessMatColor = 0f;

			base.renderState.castsShadow = false;
			base.renderState.receivesShadows = false;
            base.renderState.depthTest = false;
            base.renderState.depthWrite = false;
			base.renderState.lighted = false;

			base.textureMaterial = new SSTextureMaterial (texture);
			base.alphaBlendingEnabled = true;
			this.bbObj = bbObj;
			this.bbScene = bbScene;
		}

		protected class FlareSpriteData : SSInstancesData
		{
			protected int _numElements;

			#region sprite data sent to the GPU
			protected SSAttributeVec3[] _positions;
			protected SSAttributeVec2[] _orientationsXY = { new SSAttributeVec2(Vector2.Zero) };
			protected SSAttributeFloat[] _orientationsZ = { new SSAttributeFloat(0f) };
			protected SSAttributeColor[] _colors = { new SSAttributeColor (Color4Helper.ToUInt32 (Color4.White)) };
			protected SSAttributeFloat[] _masterScales = { new SSAttributeFloat(1f) };
			protected SSAttributeVec2[] _componentScalesXY = { new SSAttributeVec2(Vector2.One) };
			protected SSAttributeFloat[] _componentScalesZ = { new SSAttributeFloat(1f) };

			//protected SSAttributeByte[] m_spriteIndices;
			protected SSAttributeFloat[] _spriteOffsetsU;
			protected SSAttributeFloat[] _spriteOffsetsV;
			protected SSAttributeFloat[] _spriteSizesU;
			protected SSAttributeFloat[] _spriteSizesV;
			#endregion

			#region implement SSInstancesData
			public override int capacity { get { return _numElements; } }
			public override int activeBlockLength { get { return _numElements; } }
			public override float radius { get { return 100f; } } // TODO
			public override SSAttributeVec3[] positions { get { return _positions; } }
			public override SSAttributeVec2[] orientationsXY { get { return _orientationsXY; } }
			public override SSAttributeFloat[] orientationsZ { get { return _orientationsZ; } }
			public override SSAttributeColor[] colors { get { return _colors; } }
			public override SSAttributeFloat[] masterScales { get { return _masterScales; } }
			public override SSAttributeVec2[] componentScalesXY { get { return _componentScalesXY; } }
			public override SSAttributeFloat[] componentScalesZ { get { return _componentScalesZ; } }
			//public SSAttributeByte[] SpriteIndices { get { return m_spriteIndices; } }
			public override SSAttributeFloat[] spriteOffsetsU { get { return _spriteOffsetsU; ; } }
			public override SSAttributeFloat[] spriteOffsetsV { get { return _spriteOffsetsV; } }
			public override SSAttributeFloat[] spriteSizesU { get { return _spriteSizesU; } }
			public override SSAttributeFloat[] spriteSizesV { get { return _spriteSizesV; } }
			#endregion

            public FlareSpriteData(RectangleF[] spriteRects, Vector2[] spriteScales, Color4[] colors)
			{
				_init(spriteRects, spriteScales, colors);
			}

            public FlareSpriteData(RectangleF[] spriteRects, float[] spriteScales, Color4[] colors)
			{
				Vector2[] scales2d = new Vector2[spriteScales.Length];
				for (int i = 0; i < spriteScales.Length; ++i) {
					scales2d[i] = new Vector2(spriteScales[i]);
				}
				_init(spriteRects, scales2d, colors);
			}

            private void _init(RectangleF[] spriteRects, Vector2[] spriteScales, Color4[] colors)
			{
				_numElements = spriteRects.Length;
				if (spriteScales == null) {
					spriteScales = new Vector2[1];
					spriteScales [0] = Vector2.One;
				}

				_positions = new SSAttributeVec3[_numElements];
				_componentScalesXY = new SSAttributeVec2[_numElements];
				_spriteOffsetsU = new SSAttributeFloat[_numElements];
				_spriteOffsetsV = new SSAttributeFloat[_numElements];
				_spriteSizesU = new SSAttributeFloat[_numElements];
				_spriteSizesV = new SSAttributeFloat[_numElements];
                if (colors != null) {
                    _colors = new SSAttributeColor[colors.Length];
                    for (int i = 0; i < colors.Length; ++i) {
                        _colors [i] = new SSAttributeColor(Color4Helper.ToUInt32(colors [i]));
                    }
                }

				for (int i = 0; i < _numElements; ++i) {
					var spriteRect = spriteRects[i];
					var spriteScale = spriteScales[i];
					_positions[i] = new SSAttributeVec3(Vector3.Zero);
					_spriteSizesU[i] = new SSAttributeFloat(spriteRect.Width);
					_spriteSizesV[i] = new SSAttributeFloat(spriteRect.Height);
					_spriteOffsetsU[i] = new SSAttributeFloat(spriteRect.Left);
					_spriteOffsetsV[i] = new SSAttributeFloat(spriteRect.Top);
					_componentScalesXY[i] = new SSAttributeVec2(spriteScale);
				}
			}
		}
	}
}

