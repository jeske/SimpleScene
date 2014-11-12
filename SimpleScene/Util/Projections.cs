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
			SSCamera camera,
			out float width, out float height, out float nearZ, out float farZ,
            out Vector3 viewEye, out Vector3 viewTarget, out Vector3 viewUp)
        {
            if (light.Type != SSLight.LightType.Directional) {
                throw new NotSupportedException();
            }
			
            // light-aligned unit vectors
            Vector3 lightZ = light.Direction.Normalized();
            Vector3 lightX, lightY;
            OpenTKHelper.TwoPerpAxes(lightZ, out lightX, out lightY);

            // Step 1: light-direction aligned AABB of the visible objects
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
                    Vector3 lightAlignedPos = OpenTKHelper.ProjectCoord(obj.Pos, lightX, lightY, lightZ);
                    Vector3 rad = new Vector3(obj.ScaledRadius);
                    Vector3 localMin = lightAlignedPos - rad;
                    Vector3 localMax = lightAlignedPos + rad;
                    projBBMin = Vector3.ComponentMin(projBBMin, localMin);
                    projBBMax = Vector3.ComponentMax(projBBMax, localMax);
                }
            }

			if (frustum != null) {
				// then we need to do a second pass, including shadow-casters that
				// are between the camera-frusum and the light

				// compute the camera's position in lightspace, because we need to
				// include everything "closer" that the midline of the camera frustum
				Vector3 lightAlignedCameraPos = OpenTKHelper.ProjectCoord(camera.Pos, lightX, lightY, lightZ);
				float minZTest = lightAlignedCameraPos.Z;
			
	            // TODO what happens if all objects are exluded?

	            // Step 2: Extend Z of AABB to cover objects "between" current AABB and the light
	            foreach (var obj in objects) {
					if (obj.renderState.toBeDeleted
	                 || !obj.renderState.visible
	                 || !obj.renderState.castsShadow) {
	                    continue;
					}
	
	                Vector3 lightAlignedPos = OpenTKHelper.ProjectCoord(obj.Pos, lightX, lightY, lightZ);
	                Vector3 rad = new Vector3(obj.ScaledRadius);
	                Vector3 localMin = lightAlignedPos - rad;
	                Vector3 localMax = lightAlignedPos + rad;
	
	                if (OpenTKHelper.RectsOverlap(projBBMin.Xy, projBBMax.Xy, localMin.Xy, localMax.Xy)
	                 && localMin.Z < minZTest) {
	                    projBBMin = Vector3.ComponentMin(projBBMin, localMin);
	                    projBBMax = Vector3.ComponentMax(projBBMax, localMax);
	                }
	            }
			}
            // Finish the projection matrix

            // Use center of AABB in regular coordinates to get the view matrix
            Vector3 centerAligned = (projBBMin + projBBMax) / 2f;

            viewTarget = centerAligned.X * lightX
                       + centerAligned.Y * lightY
                       + centerAligned.Z * lightZ;
            float farEnough = (centerAligned.Z - projBBMin.Z) + 1f;
            viewEye = viewTarget - farEnough * lightZ;
            viewUp = lightY;

			width = projBBMax.X - projBBMin.X;
			height = projBBMax.Y - projBBMin.Y;
			nearZ = 1f;
			farZ = 1f + (projBBMax.Z - projBBMin.Z);
        }
    }
}


