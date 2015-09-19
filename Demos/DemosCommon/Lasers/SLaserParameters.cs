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

        #region middle sprites
        /// <summary>
        /// padding for the start+middle stretched sprite. Mid section vertices gets streched 
        /// beyond this padding.
        /// </summary>
        public float laserSpritePadding = 0.05f;

        /// <summary>
        /// width of the middle section sprite (in world units)
        /// </summary>
        public float backgroundWidth = 2f;
        #endregion

        #region interference sprite
        public Color4 interferenceColor = Color4.White;

        /// <summary>
        /// Interference sprite will be drawn X times thicker than the start+middle section width
        /// </summary>
        public float interferenceScale = 2.0f;

        /// <summary>
        /// Describes the value of the U-coordinate offset for the interference as a function of time
        /// </summary>
        public PeriodicFunction interferenceUFunc = (t) => -0.75f * t;
        #endregion

        #region start-only radial sprites
        /// <summary>
        /// start-only radial emission sprites will be drawn x times larger than the middle section width
        /// when at full intensity (looking straight into the laser)
        /// </summary>
        public float startPointScale = 1f;
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

        #region emission flare 
        public bool doEmissionFlare = true;
        public float emissionFlareSizeMaxPx = 500f;
        public float occDiskDirOffset = 0.5f;
        public float occDisk1RadiusPx = 15f;
        public float occDisk2RadiusWU = 0.5f;
        public float occDisksAlpha = 0.0001f;
        //public float occDisksAlpha = 0.3f;
        #endregion

        #region screen hit flare
        public bool doScreenHitFlare = true;
        public float hitFlareSizeMaxPx = 2000f;
        public float coronaBackgroundScale = 1f;
        public float coronaOverlayScale = 0.5f;
        public float ring1Scale = 0.275f;
        public float ring2Scale = 0.25f;
        #endregion

        #region particle system for burn effects
        /// <summary>
        /// Default locations of flame sprites in fig7.png
        /// </summary>
        public RectangleF[] flameSmokeSpriteRects = {
            new RectangleF(0f,    0f,    0.25f, 0.25f),
            new RectangleF(0f,    0.25f, 0.25f, 0.25f),
            new RectangleF(0.25f, 0.25f, 0.25f, 0.25f),
            new RectangleF(0.25f, 0f,    0.25f, 0.25f),
        };

        public int flameSmokeParticlesPerEmission = 5;
        public float flameSmokeEmitFrequency = 10f;
        public float flameSmokeScaleMin = 1f;
        public float flameSmokeScaleMax = 3f;
        public float flameSmokeLifetime = 1f;

        /// <summary>
        /// Default locations of flash sprites in fig7.png
        /// </summary>
        public RectangleF[] flashSpriteRects = {
            new RectangleF(0.5f,  0f,    0.25f, 0.25f),
            new RectangleF(0.75f, 0f,    0.25f, 0.25f),
            new RectangleF(0.5f,  0.25f, 0.25f, 0.25f),
            new RectangleF(0.75f, 0.25f, 0.25f, 0.25f),
        };

        public int flashParticlesPerEmission = 5;
        public float flashEmitFrequency = 10f;
        public float flashScaleMin = 1f;
        public float flashScaleMax = 3f;
        public float flashLifetime = 1f;
        #endregion

        // TODO add sprites from hitflare.png
        public SSTexture sprite2dEffectsTexture =
            SSAssetManager.GetInstance<SSTextureWithAlpha>("./lasers", "laser2dSprites.png"); 
        //SSAssetManager.GetInstance<SSTextureWithAlpha>("./", "uv_checker large.png");
    }
}

