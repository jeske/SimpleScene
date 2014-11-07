using System;
using OpenTK;
using System.Collections.Generic;
using SimpleScene;

namespace Util3d
{
    class Projections // Use namespace instead?
    {
        static Matrix4 SimpleShadowmapProjection(
            List<SSObject> objects, 
            SSLight light,
            FrustumCuller frustum = null
        )
        {
            var shadowCasters = new List<SSObject> ();

            // Step 1: AABB of the visible objects
            Vector3 min = new Vector3 (float.PositiveInfinity);
            Vector3 max = new Vector3 (float.NegativeInfinity);
            foreach (var obj in objects) {
                if (obj.renderState.toBeDeleted
                 || !obj.renderState.visible
                 || !obj.renderState.castsShadow) {
                    continue;
                }
                if (obj.boundingSphere != null
                && !frustum.isSphereInsideFrustum(obj.Pos, obj.ScaledRadius)) {
                    continue;
                }
                shadowCasters.Add(obj);
                // determine AABB of the objects so far
                for (int i = 0; i < 3; ++i) {
                    if (obj.Pos [i] < min [i]) {
                        min [i] = obj.Pos [i];
                    }
                    if (obj.Pos [i] > max [i]) {
                        max [i] = obj.Pos [i];
                    }
                }
            }

            // Step 2: find objects between AABB from before and the light
            if (light.Type == SSLight.LightType.PointSource) {
                throw new NotSupportedException();
            } else if (light.Type == SSLight.LightType.Directional) {
                // "project" AABB back onto light
            }

            return new Matrix4 ();
        }

    }
}


