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

            // Step 1: light-direction aligned AABB of the visible objects
            Matrix4 cameraViewProj = cameraView * cameraProj;
            FrustumCuller frustum = new FrustumCuller (ref cameraViewProj);

            Vector3 projBBMin = new Vector3 (float.PositiveInfinity);
            Vector3 projBBMax = new Vector3 (float.NegativeInfinity);
            foreach (var obj in objects) {
                if (obj.renderState.toBeDeleted
                 || !obj.renderState.visible
                 || !obj.renderState.castsShadow) {
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

            // Step 2: Extend Z of AABB to cover objects "between" current AABB and the light,

			// compute the camera's position in lightspace, because we need to
			// include everything "closer" that the midline of the camera frustum
            Vector3 cameraPos = cameraView.ExtractTranslation();
            Vector3 lightAlignedCameraPos = Vector3.Transform(cameraPos, lightTransform);
			float minZTest = lightAlignedCameraPos.Z;
		
            foreach (var obj in objects) {
				if (obj.renderState.toBeDeleted
                 || !obj.renderState.visible
                 || !obj.renderState.castsShadow) {
                    continue;
				}

                Vector3 lightAlignedPos = Vector3.Transform(obj.Pos, lightTransform);
                Vector3 rad = new Vector3(obj.ScaledRadius);
                Vector3 localMin = lightAlignedPos - rad;
                Vector3 localMax = lightAlignedPos + rad;

                if (OpenTKHelper.RectsOverlap(projBBMin.Xy, projBBMax.Xy, localMin.Xy, localMax.Xy)
                 && localMin.Z < minZTest) {
                    projBBMin = Vector3.ComponentMin(projBBMin, localMin);
                    projBBMax = Vector3.ComponentMax(projBBMax, localMax);
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

        private static readonly Vector3[] c_viewCube = {
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, 0f, 1f),
            new Vector3(1f, 0f, 1f),
            new Vector3(1f, 1f, 1f),
            new Vector3(0f, 1f, 1f),
        };

        public static List<Vector3> FrustumCorners(ref Matrix4 modelViewProj) {
            Matrix4 inverse = modelViewProj.Inverted();
            var ret = new List<Vector3>(c_viewCube.Length);
            for (int i = 0; i < ret.Count; ++i) {
                ret [i] = Vector3.Transform(c_viewCube [i], inverse);
            }
            return ret;
        }
    }
}


