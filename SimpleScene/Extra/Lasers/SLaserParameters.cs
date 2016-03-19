using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using SimpleScene.Util;

namespace SimpleScene.Demos
{
    [Serializable]
    public class SLaserParameters
    {
        #region customizable and serializable expressions
        public static float evaluatePeriodicScalar(string expressionStr, float t, float fallback = 0f)
        {
            if (expressionStr == null || expressionStr.Length <= 0f) {
                return fallback;
            }
            // note that Expression caching is already done internally by NCalc:
            // https://ncalc.codeplex.com/wikipage?title=description&referringTitle=Home
            var expr = new NCalc.Expression(expressionStr); 
            expr.Parameters ["Pi"] = Math.PI;
            expr.Parameters ["t"] = t;
            return (float)((double)(expr.Evaluate()));
        }
        public static float evaluateBeamPlacement(
            string expressionStr, int beamId, int numBeams, float t)
        {
            if (expressionStr == null || expressionStr.Length <= 0) {
                return 0f;
            }

            // note that Expression caching is already done internally by NCalc:
            // https://ncalc.codeplex.com/wikipage?title=description&referringTitle=Home

            var expr = new NCalc.Expression (expressionStr);
            expr.Parameters ["Pi"] = Math.PI;
            expr.Parameters ["beamId"] = beamId;
            expr.Parameters ["numBeams"] = numBeams;
            expr.Parameters ["t"] = t;
            return (float)((double)(expr.Evaluate()));
        }
        #endregion

        #region shared color parameters
        public Color4 backgroundColor = Color4.Magenta;
        public Color4 overlayColor = Color4.White;
        #endregion

        #region periodic intensity      
        /// <summary>
        /// Intensity as a function of period fraction t (from 0 to 1) using NCalc expression
        /// </summary>
        public string intensityPeriodicFunctionStr =
            "0.8 + 0.1 * Sin(2.0 * Pi * 10.0 * t) * Sin(2.0 * Pi * t)";

        /// <summary>
        /// Further periodic modulation or scale, if needed
        /// </summary>
        public string intensityModulationFunctionStr = "1.0";
        #endregion

        #region intensity ADSR envelope
        /// <summary>
        /// Attack-decay-sustain-release envelope, with infinite sustain to simulate
        /// "engaged-until-released" lasers by default
        /// </summary>
        public LinearADSREnvelope intensityEnvelope 
            = new LinearADSREnvelope (0.15f, 0.15f, float.PositiveInfinity, 0.5f, 1f, 0.7f);
        //= new ADSREnvelope (0.20f, 0.20f, float.PositiveInfinity, 1f, 1f, 0.7f);
        #endregion

        #region periodic drift
        public string driftXFuncStr = "Cos(2.0 * Pi * 0.1 * t) * Cos(2.0 * Pi * 0.53 * t)";
        public string driftYFuncStr = "Sin(2.0 * Pi * 0.1 * t) * Sin(2.0 * Pi * 0.57 * t)";
        public string driftModulationFuncStr = "0.1";
        #endregion

        #region multi-beam settings
        /// <summary>
        /// Each "laser" entry can produce multiple rendered beams to model synchronized laser cannon arrays
        /// </summary>
        public int numBeams = 1;

        /// <summary>
        /// Beam placement functions positions beam origins for one or more laser beams. When implementing
        /// assume laser origin is at (0, 0, 0) and the target is in the +z direction. Default functions
        /// arrange beams in a circle around the origin.
        /// </summary>
        public string beamPlacementFuncXStr 
            = "numBeams <= 1 ? 0.0 : Cos(2.0 * Pi * beamId / numBeams)";
        public string beamPlacementFuncYStr 
            = "numBeams <= 1 ? 0.0 : Sin(2.0 * Pi * beamId / numBeams)";
        public string beamPlacementFuncZStr
            = "0.0";

        public Vector3 getBeamPlacementVector (int beamId, int numBeams, float t)
        {
            var ret = new Vector3 (
                evaluateBeamPlacement(beamPlacementFuncXStr, beamId, numBeams, t), 
                evaluateBeamPlacement(beamPlacementFuncYStr, beamId, numBeams, t),
                evaluateBeamPlacement(beamPlacementFuncZStr, beamId, numBeams, t)
            );
            return ret;
        }

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
        public string middleBackgroundTextureFilename = "lasers/middleBackground.png";
        public string middleOverlayTextureFilename="lasers/middleOverlay.png";
        public string middleInterferenceTextureFilename = "lasers/middleInterference.png";

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
        public string middleInterferenceUFuncStr = "-0.75 * t";
        public float middleInterferenceUFunc(float t)
            { return  evaluatePeriodicScalar(middleInterferenceUFuncStr, t, fallback:1f); }
        #endregion

        #region emission flare 
        public bool doEmissionFlare = true;

        public string emissionSpritesTextureFilename = "lasers/laserEmissionSprites.png";
        public SSTexture emissionSpritesTexture() 
            { return SSAssetManager.GetInstance<SSTextureWithAlpha>(emissionSpritesTextureFilename); }

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

        public string laserBurnParticlesFilename = "explosions/fig7.png";

        public RectangleF[] flameSmokeSpriteRects = new RectangleF[] {
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
        public float flameSmokeScaleMax = 2f;
        public float flameSmokeLifetime = 4f;
        public float spriteVelocityTowardsCameraMin = 1f;
        public float spriteVelocityTowardsCameraMax = 2f;

        /// <summary>
        /// Default locations of flash sprites in fig7.png
        /// </summary>
        public RectangleF[] flashSpriteRects = new RectangleF[] {
            new RectangleF(0.5f,  0f,    0.25f, 0.25f),
            new RectangleF(0.75f, 0f,    0.25f, 0.25f),
            new RectangleF(0.5f,  0.25f, 0.25f, 0.25f),
            new RectangleF(0.75f, 0.25f, 0.25f, 0.25f),
        };

        public int flashParticlesPerEmissionMin = 1;
        public int flashParticlesPerEmissionMax = 1;
        public float flashEmitFrequency = 10f;
        public float flashScaleMin = 1.5f;
        public float flashScaleMax = 1.5f;
        public float flashLifetime = 1.5f;
        #endregion

        #region runtime helpers
        public float intensityPeriodicFunction(float t)
            { return evaluatePeriodicScalar(intensityPeriodicFunctionStr, t, fallback: 1f); }
        public float intensityModulation(float t) 
            { return evaluatePeriodicScalar(intensityModulationFunctionStr, t, fallback: 1f); }
        public float driftXFunc(float t) 
            { return evaluatePeriodicScalar(driftXFuncStr, t); }
        public float driftYFunc(float t)
            { return evaluatePeriodicScalar(driftYFuncStr, t); }
        public float driftModulationFunc(float t)
            { return evaluatePeriodicScalar(driftModulationFuncStr, t); }
        public SSTexture middleBackgroundTexture()
            { return SSAssetManager.GetInstance<SSTextureWithAlpha>(middleBackgroundTextureFilename); }
        public SSTexture middleOverlayTexture() 
            { return SSAssetManager.GetInstance<SSTextureWithAlpha>(middleOverlayTextureFilename); }
        public SSTexture middleInterferenceTexture() 
            { return SSAssetManager.GetInstance<SSTextureWithAlpha> (middleInterferenceTextureFilename ); }
        public SSTexture laserBurnParticlesTexture() 
            { return SSAssetManager.GetInstance<SSTextureWithAlpha>(laserBurnParticlesFilename); }
        #endregion

        public SLaserParameters() 
        { 
        }

        public SLaserParameters shallowCopy()
        {
            return (SLaserParameters)this.MemberwiseClone();
        }
    }
}


