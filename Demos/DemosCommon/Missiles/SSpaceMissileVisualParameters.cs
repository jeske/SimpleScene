using System;
using System.Drawing; // RectangleF
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene.Demos
{
    // TODO: fuel strategy???

    public class SSpaceMissileVisualParameters
    {
        public delegate Matrix4 SpawnTxfmDelegate(ISSpaceMissileTarget target, Vector3 launcherPos, Vector3 launcherVel);
        public delegate ISSpaceMissileDriver 
            EjectionCreationDelegate(SSpaceMissileData missile, Vector3 clusterPos, Vector3 clusterVel);
        public delegate ISSpaceMissileDriver PursuitCreationDelegate(SSpaceMissileData missile);

        #region simulation parameters
        public float simulationStep = 0.05f;
        public float atTargetDistance = 1f;
        public bool terminateWhenAtTarget = true;
        public float explosionIntensity = 1f;
        #endregion

        #region ejection
        public float minActivationTime = 1f;
        public float ejectionVelocity = 10f;
        public float ejectionMaxRotationVel = 1f;

        public BodiesFieldGenerator spawnGenerator 
            = new BodiesFieldGenerator(new ParticlesSphereGenerator(Vector3.Zero, 1f));
        public SpawnTxfmDelegate spawnTxfm 
            = (target, launcherPos, launcherVel) => { return Matrix4.CreateTranslation(launcherPos); };
        public float spawnDistanceScale = 10f;

        public EjectionCreationDelegate createEjection = (missile, clusterPos, clusterVel) =>
            { return new SSimpleMissileEjectionDriver (missile, clusterPos, clusterVel); };
            
        #endregion

        #region pursuit
        public PursuitCreationDelegate createPursuit = (missile) => 
            { return new SProportionalNavigationPursuitDriver (missile); };
        public float navigationGain = 3f;
        #endregion


        #region render parameters
        /// <summary> Missile mesh must be facing into positive Z axis </summary>
        public SSAbstractMesh missileMesh
        = SSAssetManager.GetInstance<SSMesh_wfOBJ>("missiles", "missile.obj");
        //= SSAssetManager.GetInstance<SSMesh_wfOBJ> ("./drone2/", "Drone2.obj");
        public float missileScale = 0.3f;
        /// <summary> distance from the center of the mesh to the jet (before scale) </summary>
        public float jetPosition = 4.2f;
        public SSTexture particlesTexture
            = SSAssetManager.GetInstance<SSTextureWithAlpha>("explosions", "fig7.png");

        public RectangleF[] flameSmokeSpriteRects = {
            new RectangleF(0f,    0f,    0.25f, 0.25f),
            new RectangleF(0f,    0.25f, 0.25f, 0.25f),
            new RectangleF(0.25f, 0.25f, 0.25f, 0.25f),
            new RectangleF(0.25f, 0f,    0.25f, 0.25f),
        };
        public Color4 innerFlameColor = Color4.LightGoldenrodYellow;
        public Color4 outerFlameColor = Color4.DarkOrange;
        public Color4 smokeColor = Color4.LightGray;
        public float smokeDuration = 0.5f;
        public float smokeEmissionFrequencyMin = 10f;
        public float smokeEmissionFrequencyMax = 200f;
        public int smokeParticlesPerEmissionMin = 4;
        public int smokeParticlesPerEmissionMax = 5;
        public float fullSmokeEmissionAcc = 10f;
        #endregion
    }
}

