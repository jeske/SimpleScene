using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Collections.Generic;
using Util3d;

namespace SimpleScene
{
    public class SSSimpleShadowMap : SSShadowMapBase
    {
        // http://www.opengl-tutorial.org/intermediate-tutorials/tutorial-16-shadow-mapping/

        public FrustumCuller FrustumCuller;

        private Matrix4 m_shadowProjMatrix;
        private Matrix4 m_shadowViewMatrix;

        public SSSimpleShadowMap(TextureUnit unit)
            : base(unit, 1024, 1024)
        { }


        public override void PrepareForRender (
            SSRenderConfig renderConfig,
            List<SSObject> objects,
            float fov, float aspect, float nearZ, float farZ) {
            base.PrepareForRenderBase(renderConfig, objects);

            ComputerProjections(objects, m_light,
                                renderConfig.invCameraViewMat, renderConfig.projectionMatrix);

            // update info for the regular draw pass later
            Matrix4[] vp = { m_shadowViewMatrix * m_shadowProjMatrix * c_biasMatrix };
            renderConfig.MainShader.Activate();
            renderConfig.MainShader.UniNumShadowMaps = 1;
            if (renderConfig.usePoissonSampling) {
                Vector2[] poissonScales = { new Vector2 (1f) };
                renderConfig.MainShader.UpdatePoissonScaling(poissonScales);
            }
            renderConfig.MainShader.UpdateShadowMapBiasVPs(vp);

            // setup for render shadowmap pass
            renderConfig.projectionMatrix = m_shadowProjMatrix;
            renderConfig.invCameraViewMat = m_shadowViewMatrix;
            SSShaderProgram.DeactivateAll();
		}

        private void ComputerProjections(List<SSObject> objects,
                                         SSLight light,
                                         Matrix4 cameraView, Matrix4 cameraProj)
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
            Matrix4 cameraViewProj = cameraView * cameraProj;
            SSAABB frustumLightBB = SSAABB.FromFrustum(ref lightTransform, ref cameraViewProj);

            bool shrink = false;
            SSAABB objsLightBB = new SSAABB (float.PositiveInfinity, float.NegativeInfinity);
            #if true
            // (optional) scene dependent optimization
            // Step 1: trim the light-bounding box by the shadow receivers (only in light-space x,y,maxz)
            FrustumCuller cameraFrustum = new FrustumCuller (ref cameraViewProj);

            foreach (var obj in objects) {
                // pass through all shadow casters and receivers
                if (obj.renderState.toBeDeleted || !obj.renderState.visible || obj.boundingSphere == null) {
                    continue;
                } else if (cameraFrustum.isSphereInsideFrustum(obj.Pos, obj.ScaledRadius)) {
                    // determine AABB in light coordinates of the objects so far
                    shrink = true;                        
                    Vector3 lightAlignedPos = Vector3.Transform(obj.Pos, lightTransform);
                    Vector3 rad = new Vector3(obj.ScaledRadius);
                    Vector3 localMin = lightAlignedPos - rad;
                    Vector3 localMax = lightAlignedPos + rad;
                    objsLightBB.UpdateMin(localMin);
                    objsLightBB.UpdateMax(localMax);
                }                 
            }
            #endif

            // optimize the light-frustum-projection bounding box by the object-bounding-box
            SSAABB resultLightBB = new SSAABB(float.PositiveInfinity, float.NegativeInfinity);
            if (shrink) {
                // shrink the XY & far-Z coordinates..
                resultLightBB.Min.Xy = Vector2.ComponentMax(frustumLightBB.Min.Xy, objsLightBB.Min.Xy);
                resultLightBB.Min.Z = objsLightBB.Min.Z;
                resultLightBB.Max = Vector3.ComponentMin(frustumLightBB.Max, objsLightBB.Max);
            } else {
                resultLightBB = frustumLightBB;
            }

            // extend Z of the AABB to cover shadow-casters closer to the light inside the original box
            foreach (var obj in objects) {
                if (obj.renderState.toBeDeleted || !obj.renderState.visible || !obj.renderState.castsShadow) {
                    continue;
                }
                Vector3 lightAlignedPos = Vector3.Transform(obj.Pos, lightTransform);
                Vector3 rad = new Vector3(obj.ScaledRadius);
                Vector3 localMin = lightAlignedPos - rad;
                if (localMin.Z < resultLightBB.Min.Z) {
                    Vector3 localMax = lightAlignedPos + rad;
                    if (OpenTKHelper.RectsOverlap(resultLightBB.Min.Xy, 
                                                  resultLightBB.Max.Xy, 
                                                  localMin.Xy, 
                                                  localMax.Xy)) {
                        resultLightBB.Min.Z = localMin.Z;
                    }
                }
            }  

            // Finish the view matrix
            // Use center of AABB in regular coordinates to get the view matrix
            fromLightAlignedBB(ref resultLightBB, ref lightTransform, ref lightY,
                               out m_shadowViewMatrix, out m_shadowProjMatrix);
        }

        protected static void fromLightAlignedBB(ref SSAABB bb, 
                                                 ref Matrix4 lightTransform, 
                                                 ref Vector3 lightY,
                                                 out Matrix4 viewMatrix,
                                                 out Matrix4 projMatrix)
        {
            Vector3 targetLightSpace = bb.Center();                
            Vector3 eyeLightSpace = new Vector3 (targetLightSpace.X, 
                                                 targetLightSpace.Y, 
                                                 bb.Min.Z);
            Vector3 viewTarget = Vector3.Transform(targetLightSpace, lightTransform.Inverted()); 
            Vector3 viewEye = Vector3.Transform(eyeLightSpace, lightTransform.Inverted());
            Vector3 viewUp = lightY;
            viewMatrix = Matrix4.LookAt(viewEye, viewTarget, viewUp);

            // Finish the projection matrix
            Vector3 diff = bb.Diff();
            float width, height, nearZ, farZ;
            width = diff.X;
            height = diff.Y;
            nearZ = 1f;
            farZ = 1f + diff.Z;
            projMatrix = Matrix4.CreateOrthographic(width, height, nearZ, farZ);
        }
    }
}

