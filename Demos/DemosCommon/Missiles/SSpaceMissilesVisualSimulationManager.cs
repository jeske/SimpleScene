using System;
using System.Collections.Generic;
using OpenTK;

namespace SimpleScene.Demos
{
    public class SSpaceMissilesVisualSimulationManager
    {
        protected readonly SSpaceMissilesVisualSimulation _simulation;
        protected readonly SSScene _objScene;

        protected readonly SSParticleSystemData _particlesData;
        protected readonly SSInstancedMeshRenderer _particleRenderer;

        protected readonly Dictionary<SSpaceMissileVisualizationData, SSpaceMissileRenderInfo>
            _missilesRuntimes = new Dictionary<SSpaceMissileVisualizationData, SSpaceMissileRenderInfo>();

        public SSpaceMissilesVisualSimulation simulation { get { return _simulation; } }

        public SSpaceMissilesVisualSimulationManager (SSScene objScene, SSScene particleScene, 
                                                      int particleCapacity = 2000)
        {
            _simulation = new SSpaceMissilesVisualSimulation ();

            _objScene = objScene;
            objScene.preRenderHooks += _preRenderUpdate;

            _particlesData = new SSParticleSystemData (particleCapacity);
            _particleRenderer = new SSInstancedMeshRenderer (
                _particlesData, SSTexturedQuad.DoubleFaceInstance);
            particleScene.AddObject(_particleRenderer);
        }

        ~SSpaceMissilesVisualSimulationManager()
        {
            foreach (var missile in _missilesRuntimes.Keys) {
                _removeMissile(missile);
            }
            _particleRenderer.renderState.toBeDeleted = true;
        }

        public SSpaceMissileVisualSimCluster launchCluster(
            Vector3 launchPos, Vector3 launchVel, int numMissiles,
            ISSpaceMissileTarget target, float timeToHit,
            SSpaceMissileVisualizerParameters clusterParams)
        {
            var cluster = _simulation.launchCluster(launchPos, launchVel, numMissiles,
              target, timeToHit, clusterParams);
            foreach (var missile in cluster.missiles) {
                _addMissile(missile);
            }
            return cluster;
        }

        // TODO: remove cluster??? or missile?

        protected void _addMissile(SSpaceMissileVisualizationData missile)
        {
            var missileRuntime = new SSpaceMissileRenderInfo (missile);
            _objScene.AddObject(missileRuntime.bodyObj);
            _particlesData.addEmitter(missileRuntime.smokeEmitter);
            _missilesRuntimes [missile] = missileRuntime;
        }

        protected void _removeMissile(SSpaceMissileVisualizationData missile)
        {
            var missileRuntime = _missilesRuntimes [missile];
            _missilesRuntimes.Remove(missile);

            missileRuntime.bodyObj.renderState.toBeDeleted = true;
            _particlesData.removeEmitter(missileRuntime.smokeEmitter);
            
        }

        protected void _preRenderUpdate(float timeElapsed)
        {
            foreach (var missile in _missilesRuntimes.Values) {
                missile.preRenderUpdate(timeElapsed);
            }
        }

        protected class SSpaceMissileRenderInfo
        {
            public readonly SSObjectMesh bodyObj;
            public readonly SSParticleEmitter smokeEmitter;
            protected SSpaceMissileVisualizationData _missile;

            public SSpaceMissileRenderInfo(SSpaceMissileVisualizationData missile)
            {
                _missile = missile;
                var mParams = missile.cluster.parameters;
                bodyObj = new SSObjectMesh(mParams.missileMesh);

                smokeEmitter = new SSRadialEmitter(); 
            }

            public void preRenderUpdate(float timeElapsed)
            {
                var mParams = _missile.cluster.parameters;

                bodyObj.Pos = _missile.position;

                Vector3 dir = _missile.direction;
                float dirTheta = (float)Math.Atan2(dir.Y, dir.X);
                float dirPhi = (float)Math.Atan2(dir.Z, dir.Xy.LengthFast);
                Quaternion dirQuat = Quaternion.FromAxisAngle(Vector3.UnitY, dirPhi)
                                   * Quaternion.FromAxisAngle(Vector3.UnitZ, dirTheta);
                bodyObj.Orient(mParams.missileMeshOrient * dirQuat);
            }

        }
    }
}



