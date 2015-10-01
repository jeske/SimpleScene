using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using SimpleScene.Util;

namespace SimpleScene.Demos
{
    public class SLaserParameters
    {
        #region auxiliary function types
        /// <summary>
        /// Intensity as a function of period fraction t (from 0 to 1)
        /// </summary>
        public delegate float PeriodicFunction(float t);

        /// <summary>
        /// Beam placement functions positions beam origins for one or more laser beams. When implementing
        /// assume laser origin is at (0, 0, 0) and the target is in the +z direction
        /// </summary>
        public delegate Vector3 BeamPlacementFunction(int beamID, int numBeams, float t);
        #endregion

        #region shared color parameters
        public Color4 backgroundColor = Color4.Magenta;
        public Color4 overlayColor = Color4.White;
        #endregion

        #region periodic intensity      
        /// <summary>
        /// Intensity as a function of period fraction t (from 0 to 1)
        /// </summary>
        public PeriodicFunction intensityPeriodicFunction = 
            t => 0.8f + 0.1f * (float)Math.Sin(2.0f * (float)Math.PI * 10f * t) 
            * (float)Math.Sin(2.0f * (float)Math.PI * 2f * t) ;

        /// <summary>
        /// Further periodic modulation or scale, if needed
        /// </summary>
        public PeriodicFunction intensityModulation =
            t => 1f;
        #endregion

        #region intensity ADSR envelope
        /// <summary>
        /// Attack-decay-sustain-release envelope, with infinite sustain to simulate
        /// "engaged-until-released" lasers by default
        /// </summary>
        public ADSREnvelope intensityEnvelope 
        = new ADSREnvelope (0.15f, 0.15f, float.PositiveInfinity, 0.5f, 1f, 0.7f);
        //= new ADSREnvelope (0.20f, 0.20f, float.PositiveInfinity, 1f, 1f, 0.7f);
        #endregion

        #region periodic drift
        public PeriodicFunction driftXFunc = 
            t => (float)Math.Cos (2.0f * (float)Math.PI * 0.1f * t) 
            * (float)Math.Cos (2.0f * (float)Math.PI * 0.53f * t);

        public PeriodicFunction driftYFunc =
            t => (float)Math.Sin (2.0f * (float)Math.PI * 0.1f * t) 
            * (float)Math.Sin (2.0f * (float)Math.PI * 0.57f * t);

        public PeriodicFunction driftModulationFunc =
            t => 0.1f;
        #endregion

        #region multi-beam settings
        /// <summary>
        /// Each "laser" entry can produce multiple rendered beams to model synchronized laser cannon arrays
        /// </summary>
        public int numBeams = 1;

        /// <summary>
        /// Beam placement functions positions beam origins for one or more laser beams. When implementing
        /// assume laser origin is at (0, 0, 0) and the target is in the +z direction. Default function
        /// arranges beams in a circle around the origin.
        /// </summary>
        public BeamPlacementFunction beamStartPlacementFunc = (beamID, numBeams, t) => {
            if (numBeams <= 1) {
                return Vector3.Zero;
            } else {
                float a = 2f * (float)Math.PI / (float)numBeams * beamID;
                return new Vector3((float)Math.Cos(a), (float)Math.Sin(a), 0f);
            }
        };

        /// <summary>
        /// Beam-placement function output will be scaled by this much to produce the final beam start
        /// positions in world coordinates
        /// </summary>
        public float beamStartPlacementScale = 1f;

        /// <summary>
        /// Multiple beams ends will be this far apart from the laser endpoint. (in world coordinates)
        /// </summary>
        public float beamDestSpread = 0f;
        #endregion

        #region middle cross-beam section
        public SSTexture middleBackgroundTexture
            = SSAssetManager.GetInstance<SSTextureWithAlpha>("lasers", "middleBackground.png");
        public SSTexture middleOverlayTexture 
            = SSAssetManager.GetInstance<SSTextureWithAlpha>("lasers", "middleOverlay.png");
        public SSTexture middleInterferenceTexture
            = SSAssetManager.GetInstance<SSTextureWithAlpha> ("lasers", "middleInterference.png");

        /// <summary>
        /// padding for the start+middle stretched sprite. Mid section vertices gets streched 
        /// beyond this padding.
        /// </summary>
        public float middleSpritePadding = 0.05f;

        /// <summary>
        /// width of the middle section sprite (in world units)
        /// </summary>
        public float middleBackgroundWidth = 2f;
        #endregion

        #region interference sprite
        public Color4 middleInterferenceColor = Color4.White;

        /// <summary>
        /// Interference sprite will be drawn X times thicker than the start+middle section width
        /// </summary>
        public float middleInterferenceScale = 2.0f;

        /// <summary>
        /// Describes the value of the U-coordinate offset for the interference as a function of time
        /// </summary>
        public PeriodicFunction middleInterferenceUFunc = (t) => -0.75f * t;
        #endregion

        #region emission flare 
        public bool doEmissionFlare = true;

        public SSTexture emissionSpritesTexture =
            SSAssetManager.GetInstance<SSTextureWithAlpha>("lasers", "laserEmissionSprites.png"); 
        public RectangleF emissionBackgroundRect = new RectangleF(0f, 0.5f, 0.5f, 0.5f);
        public RectangleF emissionOverlayRect = new RectangleF(0f, 0.5f, 0.5f, 0.5f);

        public float emissionFlareScreenSizeMin = 25f;
        public float emissionOccDiskDirOffset = 0.25f;
        public float emissionOccDiskRadiusPx = 15f;
        public float emissionOccDisksAlpha = 0.001f; 
        //public float occDisksAlpha = 0.3f; // for debugging set to a higher fraction
        #endregion

        #region screen hit flare
        public bool doScreenHitFlare = true;
        public float hitFlareSizeMaxPx = 1000f;
        public float hitFlareCoronaBackgroundScale = 1f;
        public float hitFlareCoronaOverlayScale = 0.5f;
        public float hitFlareRing1Scale = 0.275f;
        public float hitFlareRing2Scale = 0.25f;
        #endregion

        #region particle system for burn effects
        public bool doLaserBurn = true;

        public static SSTexture laserBurnParticlesDefaultTexture() 
        {
            return SSAssetManager.GetInstance<SSTextureWithAlpha>("explosions", "fig7.png");
        }

        public RectangleF[] flameSmokeSpriteRects = {
            new RectangleF(0f,    0f,    0.25f, 0.25f),
            new RectangleF(0f,    0.25f, 0.25f, 0.25f),
            new RectangleF(0.25f, 0.25f, 0.25f, 0.25f),
            new RectangleF(0.25f, 0f,    0.25f, 0.25f),
        };

        //public int flameSmokeParticlesPerEmission = 5;
        public int flameSmokeParticlesPerEmissionMin = 1;
        public int flameSmokeParticlesPerEmissionMax = 1;
        public float flameSmokeEmitFrequency = 10f;
        public float flameSmokeScaleMin = 1.5f;
        public float flameSmokeScaleMax = 2.5f;
        public float flameSmokeLifetime = 2.5f;
        public float flameSmokeRadialVelocityMin = 0.25f;
        public float flameSmokeRadialVelocityMax = 0.5f;

        /// <summary>
        /// Default locations of flash sprites in fig7.png
        /// </summary>
        public RectangleF[] flashSpriteRects = {
            new RectangleF(0.5f,  0f,    0.25f, 0.25f),
            new RectangleF(0.75f, 0f,    0.25f, 0.25f),
            new RectangleF(0.5f,  0.25f, 0.25f, 0.25f),
            new RectangleF(0.75f, 0.25f, 0.25f, 0.25f),
        };

        public int flashParticlesPerEmissionMin = 1;
        public int flashParticlesPerEmissionMax = 1;
        public float flashEmitFrequency = 10f;
        public float flashScaleMin = 1f;
        public float flashScaleMax = 2f;
        public float flashLifetime = 1f;
        #endregion

        //SSAssetManager.GetInstance<SSTextureWithAlpha>("./", "uv_checker large.png");
    }
}

