using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Collections.Generic;
using SimpleScene.Util3d;

namespace SimpleScene
{
    public class SSSimpleShadowMap : SSShadowMapBase
    {
        // http://www.opengl-tutorial.org/intermediate-tutorials/tutorial-16-shadow-mapping/

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

            ComputeProjections(objects, m_light,
                               renderConfig.invCameraViewMatrix, renderConfig.projectionMatrix);

            // update info for the regular draw pass later
			Matrix4[] vp = { m_shadowViewMatrix * m_shadowProjMatrix * c_biasMatrix };
			configureDrawShader (ref renderConfig, renderConfig.mainShader, vp);
			configureDrawShader (ref renderConfig, renderConfig.instanceShader, vp);

            // setup for render shadowmap pass
            renderConfig.projectionMatrix = m_shadowProjMatrix;
            renderConfig.invCameraViewMatrix = m_shadowViewMatrix;
            SSShaderProgram.DeactivateAll();
		}

		private void configureDrawShader(ref SSRenderConfig renderConfig, SSMainShaderProgram pgm, Matrix4[] vp)
		{
			if (pgm == null) return;
			pgm.Activate ();
			pgm.UniNumShadowMaps = 1;
			if (renderConfig.usePoissonSampling) {
				Vector2[] poissonScales = { new Vector2 (1f) };
				pgm.UniPoissonScaling = poissonScales;
			}
			pgm.UniShadowMapVPs = vp;
		}

        private void ComputeProjections(List<SSObject> objects,
                                        SSLightBase light,
                                        Matrix4 cameraView, Matrix4 cameraProj)
        {
            if (light.GetType() != typeof(SSDirectionalLight)) {
                throw new NotSupportedException();
            }
            SSDirectionalLight dirLight = (SSDirectionalLight)light;

            // light-aligned unit vectors
            Vector3 lightZ = dirLight.Direction.Normalized();
            Vector3 lightX, lightY;
            OpenTKHelper.TwoPerpAxes(lightZ, out lightX, out lightY);
            // transform matrix from regular space into light aligned space
            Matrix4 lightTransform = new Matrix4 (
                lightX.X, lightX.Y, lightX.Z, 0f,
                lightY.X, lightY.Y, lightY.Z, 0f,
                lightZ.X, lightZ.Y, lightZ.Z, 0f,
                0f,       0f,       0f,       0f
            );

            // Find AABB of frustum corners in light coordinates
            Matrix4 cameraViewProj = cameraView * cameraProj;
            SSAABB frustumLightBB = SSAABB.FromFrustum(ref lightTransform, ref cameraViewProj);

            bool shrink = false;
            SSAABB objsLightBB = new SSAABB (float.PositiveInfinity, float.NegativeInfinity);
            #if true
            // (optional) scene dependent optimization
            // Trim the light-bounding box by the shadow receivers (only in light-space x,y,maxz)
            SSFrustumCuller cameraFrustum = new SSFrustumCuller (ref cameraViewProj);

            foreach (var obj in objects) {
                // pass through all shadow casters and receivers
				if (obj.renderState.toBeDeleted || !obj.renderState.visible 
                    || !obj.renderState.receivesShadows || obj.localBoundingSphereRadius <= 0f) {
                    continue;
				} else if (cameraFrustum.isSphereInsideFrustum(obj.worldBoundingSphere)) {
                    // determine AABB in light coordinates of the objects so far
                    shrink = true;                        
					Vector3 lightAlignedPos = Vector3.Transform(obj.worldBoundingSphereCenter, lightTransform);
					Vector3 rad = new Vector3(obj.worldBoundingSphereRadius);
                    Vector3 localMin = lightAlignedPos - rad;
                    Vector3 localMax = lightAlignedPos + rad;
                    objsLightBB.UpdateMin(localMin);
                    objsLightBB.UpdateMax(localMax);
                }                 
            }
            #endif

            // Optimize the light-frustum-projection bounding box by the object-bounding-box
            SSAABB resultLightBB = new SSAABB(float.PositiveInfinity, float.NegativeInfinity);
            if (shrink) {
                // shrink the XY & far-Z coordinates..
                resultLightBB.Min.Xy = Vector2.ComponentMax(frustumLightBB.Min.Xy, objsLightBB.Min.Xy);
                resultLightBB.Min.Z = objsLightBB.Min.Z;
                resultLightBB.Max = Vector3.ComponentMin(frustumLightBB.Max, objsLightBB.Max);
            } else {
                resultLightBB = frustumLightBB;
            }

            // View and projection matrices, used by the scene later
            viewProjFromLightAlignedBB(ref resultLightBB, ref lightTransform, ref lightY,
                                       out m_shadowViewMatrix, out m_shadowProjMatrix);

            // Now extend Z of the result AABB to cover shadow-casters closer to the light inside the
            // original box
            foreach (var obj in objects) {
				if (obj.renderState.toBeDeleted || !obj.renderState.visible 
                    || !obj.renderState.castsShadow || obj.localBoundingSphereRadius <= 0f) {
                    continue;
                }
				Vector3 lightAlignedPos = Vector3.Transform(obj.worldBoundingSphereCenter, lightTransform);
				Vector3 rad = new Vector3(obj.worldBoundingSphereRadius);
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

            // Generate frustum culler from the BB extended towards light to include shadow casters
            Matrix4 frustumView, frustumProj;
            viewProjFromLightAlignedBB(ref resultLightBB, ref lightTransform, ref lightY,
                                       out frustumView, out frustumProj);
            Matrix4 frustumMatrix = frustumView * frustumProj;
            FrustumCuller = new SSFrustumCuller (ref frustumMatrix);
        }
    }
}
