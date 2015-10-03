using System;
using System.Collections.Generic;
using OpenTK;

namespace SimpleScene.Demos
{
    /// <summary>
    /// Missile target provides target position, velocity, and acceleration
    /// </summary>
    public interface ISpaceMissileTarget
    {
        Vector3 position { get; }
        Vector3 velocity { get; }
        Vector3 acceleration { get; } // make optional?
    };

    public class SSpaceMissileVisualizer
    {
        public void launchCluster(
            Vector3 launchPos, Vector3 launchVel, 
            ISpaceMissileTarget target, float timeToHit,
            SSpaceMissileVisualizerParameters clusterParams)
        {

        }

        /// <summary>
        /// Update time to hit with latest from network/simulation
        /// </summary>
        public void updateClusterTimeToHit(float timeToHit);

        /// <summary>
        /// Pick missiles and terminate them before they reach target
        /// </summary>
        /// <returns>The missiles info so laser renderers could shoot at them, etc.</returns>
        public List<SSpaceMissileVisualizerMissile> interceptMissiles(
            SSpaceMissileVisualizerCluster cluster, int numMissiles, float delay)
        {
            return new List<SSpaceMissileVisualizerMissile> ();
        }


    }

    public class SSpaceMissileVisualizerCluster
    {

    }

    public class SSpaceMissileVisualizerMissile
    {
        // TODO render objects
        // TODO interface with guiding behavior
        Vector3 position;
        Vector3 velocity;
    }

    public class SSpaceMissileVisualizerParameters
    {
        // TODO min/max activation time?
        // TODO: max acceleration?
        // TODO throw/programmed maneuver pattern
        // TODO: homing behavior type
        // TODO: fuel strategy?
    }
}

