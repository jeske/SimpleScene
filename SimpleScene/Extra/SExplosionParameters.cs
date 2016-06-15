using System;
using System.Drawing;
using OpenTK.Graphics;

namespace SimpleScene.Demos
{
    public class SExplosionParameters
    {
        #region global settings
        public float timeScale = 1f;
        public string textureFilename = "explosions/fig7.png";
        #endregion

        #region flame smoke parameters
        public bool doFlameSmoke = true;
        public RectangleF[] flameSmokeSprites= {
            new RectangleF(0f,    0f,    0.25f, 0.25f),
            new RectangleF(0f,    0.25f, 0.25f, 0.25f),
            new RectangleF(0.25f, 0.25f, 0.25f, 0.25f),
            new RectangleF(0.25f, 0f,    0.25f, 0.25f),
        };
        public Color4 flameColor = Color4.DarkOrange;
        public float flameSmokeDuration = 1.25f;
        #endregion

        #region flash parameters
        public bool doFlash = true;
        public RectangleF[] flashSprites = {
            new RectangleF(0.5f,  0f,    0.25f, 0.25f),
            new RectangleF(0.75f, 0f,    0.25f, 0.25f),
            new RectangleF(0.5f,  0.25f, 0.25f, 0.25f),
            new RectangleF(0.75f, 0.25f, 0.25f, 0.25f),
        };
        public Color4 flashColor = Color4.Yellow;
        public float flashDuration = 0.25f;
        #endregion

        #region flying sparks parameters
        public bool doFlyingSparks = true;
        public RectangleF[] flyingSparksSprites = {
            new RectangleF(0.75f, 0.85f, 0.25f, 0.05f)
        };
        public Color4 flyingSparksColor = Color4.DarkGoldenrod;
        public float flyingSparksDuration = 1.25f;
        #endregion

        #region smoke trails parameters
        public bool doSmokeTrails = true;
        public RectangleF[] smokeTrailsSprites= {
            new RectangleF(0f, 0.5f,   0.5f, 0.125f),
            new RectangleF(0f, 0.625f, 0.5f, 0.125f),
            new RectangleF(0f, 0.75f,  0.5f, 0.125f),
        };
        public Color4 smokeTrailsColor = Color4.Orange;
        public float smokeTrailsDuration = 0.75f;
        #endregion

        #region round sparks parameters
        public bool doRoundSparks = true;
        public RectangleF[] roundSparksSprites= {
            new RectangleF(0.5f, 0.75f, 0.25f, 0.25f)
        };
        public Color4 roundSparksColor = Color4.OrangeRed;
        public float roundSparksDuration = 1.25f;
        #endregion

        #region debris parameters
        public bool doDebris = true;
        public RectangleF[] debrisSprites= {
            new RectangleF(0.5f, 0.5f, 0.083333f, 0.083333f),
            new RectangleF(0.583333f, 0.5f, 0.083333f, 0.083333f),
            new RectangleF(0.66667f, 0.5f, 0.083333f, 0.083333f),

            new RectangleF(0.5f, 0.583333f, 0.083333f, 0.083333f),
            new RectangleF(0.583333f, 0.583333f, 0.083333f, 0.083333f),
            new RectangleF(0.66667f, 0.583333f, 0.083333f, 0.083333f),

            new RectangleF(0.5f, 0.66667f, 0.083333f, 0.083333f),
            new RectangleF(0.583333f, 0.66667f, 0.083333f, 0.083333f),
            new RectangleF(0.66667f, 0.66667f, 0.083333f, 0.083333f),
        };
        public Color4 debrisColorStart = Color4.Orange;
        public Color4 debrisColorEnd = Color4.Silver;
        public float debrisDuration = 2f;
        #endregion

        #region shockwave parameters
        public bool doShockwave = true;
        public RectangleF[] shockwaveSprites= {
            new RectangleF (0.75f, 0.5f, 0.25f, 0.25f)
        };
        public Color4 shockwaveColor = Color4.Orange;
        public float shockwaveDuration = 1f;
        #endregion

        #region runtime helpers
        public SSTexture getTexture()
            { return SSAssetManager.GetInstance<SSTextureWithAlpha> (textureFilename); }
        #endregion
    }

    public class SChainExplosionParameters : SExplosionParameters
    {
        public int numExplosionMin = 3;
        public int numExplosionsMax = 6;
        public int maxNumConcurrent = 2;
        public float initDelay = 0f;
        public float minDelay = 0.3f;
        public float maxDelay = 0.5f;
        public float minIntensity = 10f;
        public float maxIntensity = 20f;
        public float radiusMin = 0f;
        public float radiusMax = 30f;
        public float spreadVelocityMin = 10f;
        public float spreadVelocityMax = 20f;
    }
}

