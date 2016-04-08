using System;
using System.Drawing; // RectangleF
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene.Demos
{
    [Serializable]
    public class SSpaceMissileParameters
    {
        #region simulation details
        public float simulationStep = 0.025f;

        /// <summary> when true missiles are terminated when at target. otherwise external logic has to clean them up </summary>
        public bool terminateWhenAtTarget = true;
        /// <summary> invoked a missile hits a target. for example to show explosions </summary>
        //public MissileEventDelegate targetHitHandlers = null;
        #endregion

        #region spawn and ejection
        /// <summary> default ejection driver: velocity at the moment of ejection</summary>
        public float ejectionVelocity = 10f;
        /// <summary> default ejection driver: acceleration during ejection</summary>
        public float ejectionAcc = 6f;
        /// <summary> used to plugin field generators. Can be set to null, in which case only spawn transform delegates are used </summary>
        public virtual ISSpaceMissileDriver createEjection(SSpaceMissileData missile)
            { return new SMissileEjectionDriver (missile); }
        #endregion

        #region pursuit parameters and hit details
        /// <summary> time after launch when we transition from ejection into pursuit phase </summary>
        public float pursuitActivationTime = 0.35f;
        /// <summary> basic proportional navigation's coefficient (N) </summary>
        public float pursuitNavigationGain = 3f;
        /// <summary> augment proportional navigation with target's lateral acceleration. needs more testing </summary>
        public bool pursuitAugmentedPN = false;
        /// <summary> throttles/accelerates the missile hit at the time specified by the hit time </summary>
        public bool pursuitHitTimeCorrection = false;
        /// <summary> maximum velocity of a pursuit. ignored when hit time correction is active. can be set to +infinity </summary>
        public float pursuitMaxVelocity = float.PositiveInfinity;
        /// <summary> maximum lateral acceleration that can be applied while in pursuit. ignored when hit time correction is active </summary>
        public float pursuitMaxAcc = 20f;

        /// <summary> delegate for creating pursuit phase missile drivers </summary>
        public virtual ISSpaceMissileDriver createPursuit(SSpaceMissileData missile)
            { return new SProportionalNavigationPursuitDriver (missile); }

        /// <summary> roughly distance from the mesh or target center where we are sure to be hitting the target</summary>
        public float atTargetDistance = 1f;
        #endregion

        /// <summary> show visual and stdout debugging helpers </summary>
        public bool debuggingAid = false;

        public SSpaceMissileParameters()
        { }

        /// <summary>
        /// can be used with derived classes to obtain a copy of simpler parameters
        /// of this class
        /// </summary>
        public SSpaceMissileParameters(SSpaceMissileParameters other)
        {
            this.simulationStep = other.simulationStep;
            this.terminateWhenAtTarget = other.terminateWhenAtTarget;
            this.debuggingAid = other.debuggingAid;
            this.atTargetDistance = other.atTargetDistance;
            this.ejectionAcc = other.ejectionAcc;
            this.ejectionVelocity = other.ejectionVelocity;
            this.pursuitActivationTime = other.pursuitActivationTime;
            this.pursuitAugmentedPN = other.pursuitAugmentedPN;
            this.pursuitHitTimeCorrection = other.pursuitHitTimeCorrection;
            this.pursuitMaxAcc = other.pursuitMaxAcc;
            this.pursuitMaxVelocity = other.pursuitMaxVelocity;
            this.pursuitNavigationGain = other.pursuitNavigationGain;
        }

        public SSpaceMissileParameters shallowClone()
        {
            return (SSpaceMissileParameters)this.MemberwiseClone();
        }
    }

    [Serializable]
    public class SSpaceMissileVisualParameters : SSpaceMissileParameters
    {
        #region visual missile drivers
        public override ISSpaceMissileDriver createEjection(SSpaceMissileData missile)
            { return new SMissileEjectionVisualDriver (missile as SSpaceMissileVisualData); }
        /// <summary> delegate for creating pursuit phase missile drivers </summary>
        public override ISSpaceMissileDriver createPursuit(SSpaceMissileData missile)
            { return new SProportionalNavigationPursuitVisualDriver (missile as SSpaceMissileVisualData); }
        public virtual SSpaceMissileVisualData createMissile(
            Vector3 pos, Vector3 dir, Vector3 vel, SSpaceMissileClusterVisualData cluster, int clusterId)
            { return new SSpaceMissileVisualData (pos, dir, vel, cluster, clusterId); }
        #endregion

        #region visual parameters for extending the basic physics model
        /// <summary> default ejection driver: angular velocity can be initialized to at most this </summary>
        public float ejectionMaxRotationVel = 7f;
        /// <summary> radians rate by which the missile's visual orientation leans into its velocity </summary>
        public float pursuitVisualRotationRate = 0.1f;
        /// <summary> missile body mesh must be facing towards +Z axis </summary>
        public string missileBodyMeshFilename = "missiles/missile.obj";
        /// <summary> distance from the center of the mesh to the jet (before scale) </summary>
        public float jetPosition = 4.2f;
        /// <summary> scale applied to rendering missile body</summary>
        public float missileBodyScale = 0.3f;
        #endregion

        #region smoke render parameters
        public string smokeParticlesTextureFilename = "explosions/fig7.png";
        public RectangleF[] smokeSpriteRects = {
            new RectangleF(0f,    0f,    0.25f, 0.25f),
            new RectangleF(0f,    0.25f, 0.25f, 0.25f),
            new RectangleF(0.25f, 0.25f, 0.25f, 0.25f),
            new RectangleF(0.25f, 0f,    0.25f, 0.25f),
        };
        public Color4 smokeColor = Color4.LightGray;
        public float smokeEmissionFrequencyMin = 10f;
        public float smokeEmissionFrequencyMax = 200f;
        public int smokePerEmissionMin = 1;
        public int smokePerEmissionMax = 1;

        public float ejectionSmokeSizeMin = 1f;
        public float ejectionSmokeSizeMax = 15f;           
        public float ejectionSmokeDuration = 1f;

        public Color4 innerFlameColor = Color4.LightGoldenrodYellow;
        public Color4 outerFlameColor = Color4.DarkOrange;
        public float flameSmokeSizeMin = 2f;
        public float flameSmokeSizeMax = 3f;
        public float flameSmokeDuration = 0.5f;
        #endregion

        public SSpaceMissileVisualParameters() 
        {
        }

        #region runtime helpers
        public SSAbstractMesh missileBodyMesh()
        {
            return SSAssetManager.GetInstance<SSMesh_wfOBJ>(missileBodyMeshFilename); 
        }

        public SSTexture smokeParticlesTexture()
        {
            return SSAssetManager.GetInstance<SSTextureWithAlpha>(smokeParticlesTextureFilename);
        }
        #endregion
    }
}

