// Copyright(C) David W. Jeske, Sergey Butylkov 2014
// Released to the public domain. Use, modify and relicense at will.

using System;
using OpenTK;
using System.Collections.Generic;
using SimpleScene;

namespace Util3d
{
    public static class Projections
    {
        private const float c_alpha = 0.992f; // logarithmic component ratio (GPU Gems 3 10.1.12)

        private static readonly Matrix4[] c_cropMatrices = {
            new Matrix4 (
                .5f, 0f, 0f, 0f,
                0f, .5f, 0f, 0f,
                0f, 0f, 1f, 0f,
                -.5f, -.5f, 0f, 1f),
            new Matrix4 (
                .5f, 0f, 0f, 0f,
                0f, .5f, 0f, 0f,
                0f, 0f, 1f, 0f,
                +.5f, -.5f, 0f, 1f),
            new Matrix4 (
                .5f, 0f, 0f, 0f,
                0f, .5f, 0f, 0f,
                0f, 0f, 1f, 0f,
                -.5f, +.5f, 0f, 1f),
            new Matrix4 (
                .5f, 0f, 0f, 0f,
                0f, .5f, 0f, 0f,
                0f, 0f, 1f, 0f,
                +.5f, +.5f, 0f, 1f),
        };

        public static void ParallelShadowmapProjections(
            List<SSObject> objects,
            SSLight light,
            Matrix4 cameraView,
            Matrix4 cameraProj,
            float fov, float aspect, float nearZ, float farZ,
            int numShadowMaps,
            Matrix4[] shadowViewsProjs,
            float[] viewSplits
            // ideally this would have, as input, nearZ, farZ, width and height of camera proj
        )
        {
            // based on GPU Gems 3 Ch 10. Parallel-Split Shadow Maps on Programmable GPUs
            // http://http.developer.nvidia.com/GPUGems3/gpugems3_ch10.html
            
            // Dispatch shadowmap view/projection calculations with a modified
            // camera projection matrix (nearZ and farZ modified) for each frustum split
            float prevFarZ = nearZ;
            Matrix4 nextView, nextProj;
            for (int i = 0; i < numShadowMaps; ++i) {
                // generate frustum splits using Practical Split Scheme (GPU Gems 3, 10.2.1)
                float iRatio = (float)(i+1) / (float)numShadowMaps;
                float cLog = nearZ * (float)Math.Pow(farZ / nearZ, iRatio);
                float cUni = nearZ + (farZ - nearZ) * iRatio;
                float nextFarZ = c_alpha * cLog + (1f - c_alpha) * cUni;
                float nextNearZ = prevFarZ;

                // exported to the shader
                viewSplits [i] = nextFarZ;

                // create a view proj matrix with the nearZ, farZ values for the current split
                cameraProj = Matrix4.CreatePerspectiveFieldOfView(fov, aspect, nextNearZ, nextFarZ);

                Console.WriteLine("Split {0}: ", i);
                // then calculate the shadowmap for that frustrum section...
                SimpleShadowmapProjection(
                    objects, light, 
                    cameraView, cameraProj,
                    out nextView, out nextProj);

                shadowViewsProjs [i] =  nextView * nextProj * c_cropMatrices[i];
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
            Matrix4 lightWorldMatrix = new Matrix4 (
                 lightX.X, lightX.Y, lightX.Z, 0f,
                 lightY.X, lightY.Y, lightY.Z, 0f,
                 lightZ.X, lightZ.Y, lightZ.Z, 0f,
                 0f,       0f,       0f,       0f
            );
            Matrix4 worldLightMatrix = lightWorldMatrix.Inverted();

            // Step 0: AABB of frustum corners in light coordinates
            Vector3 frustumBBMin = new Vector3 (float.PositiveInfinity);
            Vector3 frustumBBMax = new Vector3 (float.NegativeInfinity);
            Matrix4 cameraViewProj = cameraView * cameraProj;
            List<Vector3> corners = FrustumCorners(ref cameraViewProj);
            for (int i = 0; i < corners.Count; ++i) {
                Vector3 corner = Vector3.Transform(corners [i], worldLightMatrix);
                frustumBBMin = Vector3.ComponentMin(frustumBBMin, corner);
                frustumBBMax = Vector3.ComponentMax(frustumBBMax, corner);
            }
            
            Vector3 projBBMin = frustumBBMin;
            Vector3 projBBMax = frustumBBMax;            

            if (true) {
                // (optional) scene dependent optimization
			    // Step 1: trim the light-bounding box by the shadow receivers (only in light-space x,y,maxz)
                FrustumCuller cameraFrustum = new FrustumCuller (ref cameraViewProj);               


                Vector3 objBBMin = new Vector3(float.PositiveInfinity);
                Vector3 objBBMax = new Vector3(float.NegativeInfinity);
                bool haveShadowReceiver = false;                
                float lightAlignedNearZForFirstShadowCaster = float.PositiveInfinity;

                foreach (var obj in objects) {
                    // pass through all shadow casters and receivers
                    if (obj.renderState.toBeDeleted || !obj.renderState.visible || obj.boundingSphere == null) {
                        continue;
                    } else if (cameraFrustum.isSphereInsideFrustum(obj.Pos, obj.ScaledRadius)) {
                        // determine AABB in light coordinates of the objects so far
                        haveShadowReceiver = true;                        
                        Vector3 lightAlignedPos = Vector3.Transform(obj.Pos, worldLightMatrix);
                        Vector3 rad = new Vector3(obj.ScaledRadius);
                        Vector3 localMin = lightAlignedPos - rad;
                        Vector3 localMax = lightAlignedPos + rad;
                        
                        objBBMin = Vector3.ComponentMin(objBBMin,localMin);
                        objBBMax = Vector3.ComponentMax(objBBMax,localMax); 
                        lightAlignedNearZForFirstShadowCaster = Math.Min(lightAlignedNearZForFirstShadowCaster,objBBMin.Z);                       
                    }
                     
                    // extend Z of the AABB to cover shadow-casters closer to the light inside the original box
                    {
                        Vector3 lightAlignedPos = Vector3.Transform(obj.Pos, worldLightMatrix);
                        Vector3 rad = new Vector3(obj.ScaledRadius);
                        Vector3 localMin = lightAlignedPos - rad;
                        if (localMin.Z < lightAlignedNearZForFirstShadowCaster) {
                            Vector3 localMax = lightAlignedPos + rad;
                            if (OpenTKHelper.RectsOverlap(projBBMin.Xy, projBBMax.Xy, localMin.Xy, localMax.Xy)) {
                                lightAlignedNearZForFirstShadowCaster = Math.Min(lightAlignedNearZForFirstShadowCaster, localMin.Z);
                            }
                        }
                    }
                    
                }
                
                // optimize the light-frustum-projection bounding box by the object-bounding-box
                if (haveShadowReceiver) {                
                    // shrink the XY & far-Z coordinates..
                    projBBMin.X = Math.Max(projBBMin.X,objBBMin.X);
                    projBBMin.Y = Math.Max(projBBMin.Y,objBBMin.Y);
                    
                    projBBMax.X = Math.Min(projBBMax.X,objBBMax.X);
                    projBBMax.Y = Math.Min(projBBMax.Y,objBBMax.Y);
                    projBBMax.Z = Math.Min(projBBMax.Z,objBBMax.Z);                    

                    // extend nearZ towards the light                    
                    projBBMin.Z = Math.Min(projBBMin.Z, lightAlignedNearZForFirstShadowCaster);
                }
            }
           

            // Finish the view matrix
            {
                // Use center of AABB in regular coordinates to get the view matrix  
                Vector3 target_lightSpace = (projBBMin + projBBMax) / 2f;                
                Vector3 eye_lightSpace = new Vector3(target_lightSpace.X,target_lightSpace.Y,projBBMin.Z);
                
                Vector3 viewTarget = Vector3.Transform(target_lightSpace,lightWorldMatrix); 
                Vector3 viewEye = Vector3.Transform(eye_lightSpace,lightWorldMatrix);
                
                Console.WriteLine("shadowmap Matrix: {0} {1}",viewEye,viewTarget);

                Vector3 viewUp = lightY;
                shadowView = Matrix4.LookAt(viewEye, viewTarget, viewUp);

                // Finish the projection matrix
                {
                    float width, height, nearZ, farZ;
			        width = (projBBMax.X - projBBMin.X);
			        height = (projBBMax.Y - projBBMin.Y);
			        nearZ = 1f + 10f; farZ = 1f + (projBBMax.Z - projBBMin.Z);
                    shadowProj = Matrix4.CreateOrthographic(width, height, nearZ, farZ);
                }
            }
        }

        private static readonly Vector4[] c_homogenousCorners = {
            new Vector4(-1f, -1f, -1f, 1f),
            new Vector4(-1f, 1f, -1f, 1f),
            new Vector4(1f, 1f, -1f, 1f),
            new Vector4(1f, -1f, -1f, 1f),

            new Vector4(-1f, -1f, 1f, 1f),
            new Vector4(-1f, 1f, 1f, 1f),
            new Vector4(1f, 1f, 1f, 1f),
            new Vector4(1f, -1f, 1f, 1f),
        };

        public static List<Vector3> FrustumCorners(ref Matrix4 modelViewProj) {
            Matrix4 inverse = modelViewProj;
            inverse.Invert();
            var ret = new List<Vector3>(c_homogenousCorners.Length);
            for (int i = 0; i < c_homogenousCorners.Length; ++i) {
                Vector4 corner = Vector4.Transform(c_homogenousCorners [i], inverse);
                corner /= corner.W;
                ret.Add(corner.Xyz);
            }
            return ret;
        }
    }
}


