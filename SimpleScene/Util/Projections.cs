using System;
using OpenTK;
using System.Collections.Generic;
using SimpleScene;

namespace Util3d
{
    public static class Projections
    {
        const float c_alpha = 0.5f; // logarithmic component ratio (GPU Gems 3 10.1.12)

        public static void ParallelShadowmapProjections(
            List<SSObject> objects,
            SSLight light,
            Matrix4 cameraView, Matrix4 cameraProj,
            int numShadowMaps,
            ref List<Matrix4> shadowViews,
            ref List<Matrix4> shadowProjs
            // ideally this would have, as input, nearZ, farZ, width and height of camera proj
        )
        {
            // Step 1: Extract the old near Z, far Z from the camera proj matrix
            // http://www.terathon.com/gdc07_lengyel.pdf
            float A = cameraProj [2, 2];
            float B = cameraProj [2, 3];
            float nearZ = 2f * B / (A - 1);
            float farZ = 2f * B / (A + 1);

            // Step 2: Try to shrink [nearZ, farZ] range to the objects inside the frustum
            Matrix4 cameraViewProj = cameraView * cameraProj;
            FrustumCuller frustum = new FrustumCuller (ref cameraViewProj);

            float objFarZ = float.NegativeInfinity;
            float objNearZ = float.PositiveInfinity;
            foreach (var obj in objects) {
                // pass through all shadow casters and receivers
                if (obj.renderState.toBeDeleted
                 || !obj.renderState.visible) {
                    continue;
                }
                float rad = obj.ScaledRadius;
                if (frustum.isSphereInsideFrustum(obj.Pos, rad)) {
                    float currViewZ = - Vector3.Transform(obj.Pos, cameraView).Z;
                    objNearZ = Math.Min(objNearZ, currViewZ - rad);
                    objFarZ = Math.Max(objFarZ, currViewZ + rad);
                }
            }
            nearZ = Math.Max(nearZ, objNearZ);
            farZ = Math.Min(farZ, objFarZ);

            // Step 3: Dispatch shadowmap view/projection calculations with a modified
            // camera projection matrix (nearZ and farZ modified) for each frustum split
            shadowProjs.Clear();
            shadowViews.Clear();
            float prevFarZ = nearZ;
            Matrix4 nextView, nextProj;
            for (int i = 1; i <= numShadowMaps; ++i) {
                // generate frustum splits using Practical Split Scheme (GPU Gems 3, 10.2.1)
                float iRatio = (float)i / (float)numShadowMaps;
                float cLog = nearZ * (float)Math.Pow(farZ / nearZ, iRatio);
                float cUni = nearZ + (farZ - nearZ) * iRatio;
                float nextFarZ = c_alpha * cLog + (1f - c_alpha) * cUni;
                float nextNearZ = prevFarZ;

                // modify the view proj matrix with the nearZ, farZ values for the current split
                cameraProj [2, 2] = (nextNearZ + nextFarZ) / (nextNearZ - nextFarZ);
                cameraProj [2, 3] = nextFarZ * nextNearZ / (nextNearZ - nextFarZ);

                SimpleShadowmapProjection(
                    objects, light, 
                    cameraView, cameraProj,
                    out nextView, out nextProj);

                shadowViews.Add(nextView);
                shadowProjs.Add(nextProj);
                prevFarZ = nextFarZ;
            }
        }

        public static void SimpleShadowmapProjection(
            List<SSObject> objects,
            SSLight light,
            Matrix4 cameraView, Matrix4 cameraProj,
            out Matrix4 shadowView, out Matrix4 shadowProj)
        {
            if (light.Type != SSLight.LightType.Directional) {
                throw new NotSupportedException();
            }
			
            // light-aligned unit vectors
            Vector3 lightZ = light.Direction.Normalized();
            Vector3 lightX, lightY;
            OpenTKHelper.TwoPerpAxes(lightZ, out lightX, out lightY);
            // transform matrix from regular space into light aligned space
            Matrix4 lightTransform = new Matrix4 (
                 lightX.X, lightX.Y, lightX.Z, 0f,
                 lightY.X, lightY.Y, lightY.Z, 0f,
                 lightZ.X, lightZ.Y, lightZ.Z, 0f,
                 0f,       0f,       0f,       0f
            );

            // Step 0: AABB of frustum corners in light coordinates
            Vector3 frustumBBMin = new Vector3 (float.PositiveInfinity);
            Vector3 frustumBBMax = new Vector3 (float.NegativeInfinity);
            Matrix4 cameraViewProj = cameraView * cameraProj;
            List<Vector3> corners = FrustumCorners(ref cameraViewProj);
            for (int i = 0; i < corners.Count; ++i) {
                Vector3 corner = Vector3.Transform(corners [i], lightTransform);
                frustumBBMin = Vector3.ComponentMin(frustumBBMin, corner);
                frustumBBMax = Vector3.ComponentMax(frustumBBMax, corner);
            }

            // Step 1: light-direction aligned AABB of shadow receivers,
            FrustumCuller frustum = new FrustumCuller (ref cameraViewProj);

            Vector3 projBBMin = new Vector3 (float.PositiveInfinity);
            Vector3 projBBMax = new Vector3 (float.NegativeInfinity);
            foreach (var obj in objects) {
                // pass through all shadow casters and receivers
                if (obj.renderState.toBeDeleted
                 || !obj.renderState.visible) {
                    continue;
                } else if (frustum == null || ( obj.boundingSphere != null
                        && frustum.isSphereInsideFrustum(obj.Pos, obj.ScaledRadius))) {
                    // determine AABB in light coordinates of the objects so far
                    Vector3 lightAlignedPos = Vector3.Transform(obj.Pos, lightTransform);
                    Vector3 rad = new Vector3(obj.ScaledRadius);
                    Vector3 localMin = lightAlignedPos - rad;
                    Vector3 localMax = lightAlignedPos + rad;
                    projBBMin = Vector3.ComponentMin(projBBMin, localMin);
                    projBBMax = Vector3.ComponentMax(projBBMax, localMax);
                }
            }

            // Step 1B: trim by the frustum bounding box, but leave min Z alone
            projBBMin.Xy = Vector2.Min(projBBMin.Xy, frustumBBMin.Xy);
            projBBMax = Vector3.ComponentMin(projBBMax, frustumBBMax);

            // Step 2: Extend Z of AABB to cover shadow casters between current AABB and the light,
            foreach (var obj in objects) {
                // pass through all shadow casters
				if (obj.renderState.toBeDeleted
                 || !obj.renderState.visible
                 || !obj.renderState.castsShadow) {
                    continue;
				}
                Vector3 lightAlignedPos = Vector3.Transform(obj.Pos, lightTransform);
                Vector3 rad = new Vector3(obj.ScaledRadius);
                Vector3 localMin = lightAlignedPos - rad;
                if (localMin.Z < projBBMin.Z) {
                    Vector3 localMax = lightAlignedPos + rad;
                    if (OpenTKHelper.RectsOverlap(projBBMin.Xy, projBBMax.Xy, localMin.Xy, localMax.Xy)) {
                        projBBMin.Z = localMin.Z;
                    }
                }
            }

            // Use center of AABB in regular coordinates to get the view matrix
            Vector3 centerAligned = (projBBMin + projBBMax) / 2f;

            // Finish the view matrix
            float width, height, nearZ, farZ;
            Vector3 viewEye, viewTarget, viewUp;
            viewTarget = centerAligned.X * lightX
                       + centerAligned.Y * lightY
                       + centerAligned.Z * lightZ;
            float farEnough = (centerAligned.Z - projBBMin.Z) + 1f;
            viewEye = viewTarget - farEnough * lightZ;
            viewUp = lightY;
            shadowView = Matrix4.LookAt(viewEye, viewTarget, viewUp);

            // Finish the projection matrix
			width = projBBMax.X - projBBMin.X;
			height = projBBMax.Y - projBBMin.Y;
			nearZ = 1f;
			farZ = 1f + (projBBMax.Z - projBBMin.Z);
            shadowProj = Matrix4.CreateOrthographic(width, height, nearZ, farZ);
        }

        private static readonly Vector4[] c_viewCube = {
            new Vector4(-1f, -1f, 0f, 1f),
            new Vector4(-1f, 1f, 0f, 1f),
            new Vector4(1f, 1f, 0f, 1f),
            new Vector4(1f, -1f, 0f, 1f),

            new Vector4(-1f, -1f, 1f, 1f),
            new Vector4(-1f, 1f, 1f, 1f),
            new Vector4(1f, 1f, 1f, 1f),
            new Vector4(1f, -1f, 1f, 1f),
        };

        public static List<Vector3> FrustumCorners(ref Matrix4 modelViewProj) {
            Matrix4 inverse = modelViewProj;
            //inverse.Transpose();
            inverse.Invert();
            var ret = new List<Vector3>(c_viewCube.Length);
            for (int i = 0; i < c_viewCube.Length; ++i) {
                Vector4 corner = Vector4.Transform(c_viewCube [i], inverse);
                corner /= corner.W;
                ret.Add(corner.Xyz);
            }
            return ret;
        }
    }
}


