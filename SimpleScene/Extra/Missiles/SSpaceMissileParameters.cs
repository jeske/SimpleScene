using System;
using System.Drawing; // RectangleF
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene.Demos
{
    // TODO: fuel strategy???
    [Serializable]
    public class SSpaceMissileParameters
    {
        #if false
        public delegate Matrix4 SpawnTxfmDelegate(ISSpaceMissileTarget target, 
                                                  Vector3 launcherPos, Vector3 launcherVel,
                                                  int missileId, int clusterSize);
        public delegate ISSpaceMissileDriver 
            EjectionCreationDelegate(SSpaceMissileData missile, Vector3 clusterPos, Vector3 clusterVel);
        public delegate ISSpaceMissileDriver PursuitCreationDelegate(SSpaceMissileData missile);
        public delegate void MissileEventDelegate (Vector3 position, SSpaceMissileParameters mParams);
        #endif

        #region simulation parameters
        public float simulationStep = 0.025f;
        #endregion

        #region spawn and ejection
        /// <summary> default ejection driver: velocity at the moment of ejection</summary>
        public float ejectionVelocity = 10f;
        /// <summary> default ejection driver: acceleration during ejection</summary>
        public float ejectionAcc = 6f;
        /// <summary> default ejection driver: angular velocity can be initialized to at most this </summary>
        public float ejectionMaxRotationVel = 7f;

        /// <summary> used to plugin field generators. Can be set to null, in which case only spawn transform delegates are used </summary>
        public virtual ISSpaceMissileDriver createEjection(SSpaceMissileData missile)
            { return new SSimpleMissileEjectionDriver (missile); }
        #if false
        public BodiesFieldGenerator spawnGenerator
            = new BodiesFieldGenerator(new ParticlesSphereGenerator(Vector3.Zero, 1f));
        /// <summary> on the presense of generator scales its output distance from the center </summary>
        public float spawnGeneratorScale = 10f;
        /// <summary> initial transform applied to every spawned missile. </summary>
        public SpawnTxfmDelegate spawnTxfm 
            = (target, launcherPos, launcherVel, id, num) 
                => { return Matrix4.CreateTranslation(launcherPos); };
        /// <summary> delegate for creating new ejection phase missile drivers </summary>
        public string [] spawnPosOffsetFuncs = {"0.0", "0.0", "0.0"}; // customizable ncalc expressions for x, y, z
        /// <summary> angle above z axis </summary>
        // public string spawnDirThetaFunc = "0.0"; 
        /// <summary> angle above xy plane </summary>
        // public string spawnDirPhiFunc = "0.0";

        protected static NCalc.Expression _setupSpawnExpr(string exprStr,
            Vector3 launcherPos, Vector3 launcherVel,
            int missileId, int numMissiles, Vector3 targetPosLocal) 
        {
            var ret = new NCalc.Expression(exprStr); 
            ret.Parameters ["Pi"] = Math.PI;
            ret.Parameters ["targetPosX"] = targetPos.X;
            ret.Parameters ["targetPosY"] = targetPos.Y;
            ret.Parameters ["targetPosZ"] = targetPos.Z;
            ret.Parameters ["launcherPosX"] = launcherPos.X;
            ret.Parameters ["launcherPosY"] = launcherPos.Y;
            ret.Parameters ["launcherPosZ"] = launcherPos.Z;
            ret.Parameters ["launcherVelX"] = launcherVel.X;
            ret.Parameters ["launcherVelY"] = launcherVel.Y;
            ret.Parameters ["launcherVelZ"] = launcherVel.Z;
            ret.Parameters ["i"] = missileId;
            ret.Parameters ["numMissiles"] = numMissiles;
            return ret;
        }

        public Vector3 evaluateSpawnPosOffset (int missileId, int numMissiles, Vector3 targetPosLocal)
        {
            Vector3 ret = Vector3.Zero;
            for (int i = 0; i < 3; ++i) {
                string expressionStr = spawnPosOffsetFuncs [i];
                if (expressionStr != null || expressionStr.Length <= 0f) {
                    var expr = _setupSpawnExpr(expressionStr, 
                        launcherPos, launcherVel, missileId, numMissiles, targetPos);
                    ret [i] += (float)((double)expr.Evaluate());
                }
            }
            return ret;


            // note that Expression caching is already done internally by NCalc:
            // https://ncalc.codeplex.com/wikipage?title=description&referringTitle=Home
            /return (float)((double)(expr.Evaluate()));
        }
        #endif

        #endregion

        #region pursuit
        /// <summary> time after launch when we transition from ejection into pursuit phase </summary>
        public float pursuitActivationTime = 0.35f;
        /// <summary> delegate for creating pursuit phase missile drivers </summary>
        public virtual ISSpaceMissileDriver createPursuit(SSpaceMissileData missile)
            { return new SProportionalNavigationPursuitDriver (missile); }
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
        #endregion

        #region target hit and termination
        /// <summary> roughly distance from the mesh or target center where we are sure to be hitting the target</summary>
        public float atTargetDistance = 1f;
        /// <summary> when true missiles are terminated when at target. otherwise external logic has to clean them up </summary>
        public bool terminateWhenAtTarget = true;
        /// <summary> invoked a missile hits a target. for example to show explosions </summary>
        //public MissileEventDelegate targetHitHandlers = null;
        #endregion

        #region body render parameters
        /// <summary> radians rate by which the missile's visual orientation leans into its velocity </summary>
        public float pursuitVisualRotationRate = 0.1f;
        /// <summary> missile body mesh must be facing into +Z axis </summary>
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

        /// <summary> show visual and stdout debugging helpers </summary>
        public bool debuggingAid = false;

        public SSpaceMissileParameters() 
        {
        }

        #region runtime helpers
        public SSAbstractMesh missileBodyMesh()
        {
            return SSAssetManager.GetInstance<SSMesh_wfOBJ>(".", missileBodyMeshFilename); 
        }

        public SSTexture smokeParticlesTexture()
        {
            return SSAssetManager.GetInstance<SSTextureWithAlpha>(".", smokeParticlesTextureFilename);
        }
        #endregion
    }
}

