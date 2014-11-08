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
            out Matrix4 view
        )
        {
            if (light.Type != SSLight.LightType.Directional) {
                throw new NotSupportedException();
            }

            // light "Z" vector
            Vector3 lightZ = light.Direction.Normalized();
            Vector3 lightX, lightY;
            TwoPerpAxes(lightZ, out lightX, out lightY);

            var excluded = new List<SSObject> ();

            // Step 1: light-direction aligned AABB of the visible objects
            Vector3 aabbMin = new Vector3 (float.PositiveInfinity);
            Vector3 aabbMax = new Vector3 (float.NegativeInfinity);
            foreach (var obj in objects) {
                if (obj.renderState.toBeDeleted
                 || !obj.renderState.visible
                 || !obj.renderState.castsShadow) {
                    continue;
                } else if (obj.boundingSphere != null
                        && !frustum.isSphereInsideFrustum(obj.Pos, obj.ScaledRadius)) {
                    excluded.Add(obj);
                } else {
                    // determine AABB in light coordinates of the objects so far
                    Vector3 lightAlignedPos = DirAlignedCoord(obj.Pos, lightX, lightY, lightZ);
                    Vector3 rad = new Vector3(obj.ScaledRadius);
                    Vector3 localMin = lightAlignedPos - rad;
                    Vector3 localMax = lightAlignedPos + rad;
                    for (int i = 0; i < 3; ++i) {
                        if (localMin[i] < aabbMin[i]) {
                            aabbMin [i] = localMin[i];
                        }
                        if (localMax[i] > aabbMax[i]) {
                            aabbMax [i] = localMax[i];
                        }
                    }
                }
            }

            // Step 2: Extend Z of AABB to cover objects "between" current AABB and the light
            foreach (var obj in excluded) {
                Vector3 lightAlignedPos = DirAlignedCoord(obj.Pos, lightX, lightY, lightZ);
                Vector3 rad = new Vector3(obj.ScaledRadius);
                Vector3 localMin = lightAlignedPos - rad;
                Vector3 localMax = lightAlignedPos + rad;

                if (RectsOverlap(aabbMin.Xy, aabbMax.Xy, localMin.Xy, localMax.Xy)
                 && localMin.Z < aabbMin.Z) {
                    aabbMin.Z = localMin.Z;
                }
            }

            // Finish the projection matrix
            proj = Matrix4.CreateOrthographicOffCenter(
                aabbMin.X, aabbMax.X,
                aabbMin.Y, aabbMax.Y,
                1f, 1f + (aabbMax.Z - aabbMin.Z));

            // Use center of AABB in regular coordinates to get the view matrix
            Vector3 center = (aabbMin + aabbMax) / 2f;
            center = DirAlignedCoord(center, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);
            view = Matrix4.LookAt(center - lightZ, center, lightY);
        }

        public static void TwoPerpAxes(Vector3 zAxis, 
                                       out Vector3 xAxis, 
                                       out Vector3 yAxis,
                                       float delta = 0.01f) {
            // pick two perpendicular axes to an axis
            zAxis.Normalize();
            if (Math.Abs(zAxis.X) < delta
             && Math.Abs(zAxis.Y) < delta) { // special case
                xAxis = Vector3.UnitX;
            } else {
                xAxis = new Vector3(zAxis.Y, -zAxis.X, 0.0f);
            }
            yAxis = Vector3.Cross(zAxis, xAxis);
        }

        public static Vector3 DirAlignedCoord(Vector3 pt, 
                                              Vector3 dirX, Vector3 dirY, Vector3 dirZ) {
            // Assumes xAxis, yAxis, and zAxis are normalized
            Vector3 ret;
            ret.X = Vector3.Dot(pt, dirX) / pt.X;
            ret.Y = Vector3.Dot(pt, dirY) / pt.Y;
            ret.Z = Vector3.Dot(pt, dirZ) / pt.Z;
            return ret;
        }

        public static bool RectsOverlap(Vector2 r1Min, Vector2 r1Max,
                                        Vector2 r2Min, Vector2 r2Max) {
            return !(r1Max.X < r2Min.X || r2Max.X < r1Min.X
                  || r1Max.Y < r2Min.Y || r2Max.Y < r1Min.Y);
        }
    }
}


