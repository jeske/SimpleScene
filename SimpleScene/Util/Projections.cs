using System;
using OpenTK;
using System.Collections.Generic;
using SimpleScene;

namespace Util3d
{
    public static class Projections
    {
        public static void SimpleShadowmapProjection(
            List<SSObject> objects, 
            SSLight light,
            FrustumCuller frustum, // can be null (disabled)
            out Matrix4 proj,
            out Matrix4 view)
        {
            if (light.Type != SSLight.LightType.Directional) {
                throw new NotSupportedException();
            }

            // light-aligned unit vectors
            Vector3 lightZ = light.Direction.Normalized();
            Vector3 lightX, lightY;
            OpenTKHelper.TwoPerpAxes(lightZ, out lightX, out lightY);

            var excluded = new List<SSObject> ();

            // Step 1: light-direction aligned AABB of the visible objects
            Vector3 aabbMin = new Vector3 (float.PositiveInfinity);
            Vector3 aabbMax = new Vector3 (float.NegativeInfinity);
            foreach (var obj in objects) {
                if (obj.renderState.toBeDeleted
                 || !obj.renderState.visible
                 || !obj.renderState.castsShadow) {
                    continue;
                } else if (frustum != null && obj.boundingSphere != null
                        && !frustum.isSphereInsideFrustum(obj.Pos, obj.ScaledRadius)) {
                    excluded.Add(obj);
                } else {
                    // determine AABB in light coordinates of the objects so far
                    Vector3 lightAlignedPos = OpenTKHelper.ProjectCoord(obj.Pos, lightX, lightY, lightZ);
                    Vector3 rad = new Vector3(obj.ScaledRadius);
                    Vector3 localMin = lightAlignedPos - rad;
                    Vector3 localMax = lightAlignedPos + rad;
                    aabbMin = Vector3.ComponentMin(aabbMin, localMin);
                    aabbMax = Vector3.ComponentMax(aabbMax, localMax);
                }
            }

            // TODO what happens if all objects are exluded?

            // Step 2: Extend Z of AABB to cover objects "between" current AABB and the light
            foreach (var obj in excluded) {
                Vector3 lightAlignedPos = OpenTKHelper.ProjectCoord(obj.Pos, lightX, lightY, lightZ);
                Vector3 rad = new Vector3(obj.ScaledRadius);
                Vector3 localMin = lightAlignedPos - rad;
                Vector3 localMax = lightAlignedPos + rad;

                if (OpenTKHelper.RectsOverlap(aabbMin.Xy, aabbMax.Xy, localMin.Xy, localMax.Xy)
                 && localMin.Z < aabbMax.Z) {
                    aabbMin = Vector3.ComponentMin(aabbMin, localMin);
                    aabbMax = Vector3.ComponentMax(aabbMax, localMax);
                }
            }

            // Finish the projection matrix
            proj = Matrix4.CreateOrthographicOffCenter(
                aabbMin.X, aabbMax.X,
                aabbMin.Y, aabbMax.Y,
                1f, 1f + (aabbMax.Z - aabbMin.Z));

            // Use center of AABB in regular coordinates to get the view matrix
            Vector3 centerAligned = (aabbMin + aabbMax) / 2f;
            Vector3 center = centerAligned.X * lightX
                           + centerAligned.Y * lightY
                           + centerAligned.Z * lightZ;
            float farEnough = (centerAligned.Z - aabbMin.Z) + 1f;
            view = Matrix4.LookAt(center - farEnough * lightZ, center, lightY);
        }
    }
}


