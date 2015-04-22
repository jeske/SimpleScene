// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Collections.Generic;


using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	public enum WireframeMode {
		None = 0,
		GLSL_SinglePass = 1,
		GL_Lines = 2,
	};

	public struct SSRenderStats {
		public int objectsDrawn;
		public int objectsCulled;
	}     

	public class SSRenderConfig {
        public SSRenderStats renderStats;

		public SSMainShaderProgram MainShader;
        public SSPssmShaderProgram PssmShader;
        public SSInstanceShaderProgram InstanceShader;
		public SSInstancePssmShaderProgram InstancePssmShader;

		public bool drawGLSL = true;
		public bool useVBO = true;
        public bool drawingShadowMap = false;
		public bool drawingPssm = false;

        public bool usePoissonSampling = true;
        public int numPoissonSamples = 8;
        public SSMainShaderProgram.LightingMode lightingMode = SSMainShaderProgram.LightingMode.BlinnPhong;
		//public SSMainShaderProgram.LightingMode lightingMode = SSMainShaderProgram.LightingMode.ShadowMapDebug;

        public bool renderBoundingSpheres = false;
        public bool renderCollisionShells = false;

        public bool frustumCulling = true;

		public WireframeMode drawWireframeMode;
		public Matrix4 invCameraViewMat = Matrix4.Identity;
		public Matrix4 projectionMatrix = Matrix4.Identity;

		public static WireframeMode NextWireFrameMode(WireframeMode val) {
			int newVal = (int)val;
			newVal++;
			if (newVal > (int)WireframeMode.GL_Lines) {
				newVal = (int)WireframeMode.None;
			}
			return (WireframeMode)newVal;
		}
	}

    public sealed class SSScene
    {
        private SSCamera m_activeCamera = null;
        private SSRenderConfig m_renderConfig = new SSRenderConfig();
        private List<SSObject> m_objects = new List<SSObject>();
        private List<SSLightBase> m_lights = new List<SSLightBase>();

        public List <SSObject> Objects { get { return m_objects; } }

        public List<SSLightBase> Lights { get { return m_lights; } }

        public SSCamera ActiveCamera { 
            get { return m_activeCamera; }
            set { m_activeCamera = value; }
        }

        #region convenience pass-through parameters
        public SSMainShaderProgram MainShader {
            get { return m_renderConfig.MainShader; }
            set { 
                m_renderConfig.MainShader = value;
                if (m_renderConfig.MainShader != null) {
                    m_renderConfig.MainShader.Activate();
                    m_renderConfig.MainShader.SetupShadowMap(m_lights);
                    m_renderConfig.MainShader.Deactivate();
                }
            }
        }

        public SSPssmShaderProgram PssmShader {
            get { return m_renderConfig.PssmShader; }
            set { m_renderConfig.PssmShader = value; }
        }

        public SSInstanceShaderProgram InstanceShader {
            get { return m_renderConfig.InstanceShader; }
            set { m_renderConfig.InstanceShader = value; }
        }

		public SSInstancePssmShaderProgram InstancePssmShader {
			get { return m_renderConfig.InstancePssmShader; }
			set { m_renderConfig.InstancePssmShader = value; }
		}
        #endregion

        public bool FrustumCulling {
            get { return m_renderConfig.frustumCulling; }
            set { m_renderConfig.frustumCulling = value; }
        }

        public Matrix4 ProjectionMatrix {
            get { return m_renderConfig.projectionMatrix; }
            set { m_renderConfig.projectionMatrix = value; }
        }

        public Matrix4 InvCameraViewMatrix {
            get { return m_renderConfig.invCameraViewMat; }
            set { m_renderConfig.invCameraViewMat = value; }
        }

        public WireframeMode DrawWireFrameMode {
            get { return m_renderConfig.drawWireframeMode; }
            set { m_renderConfig.drawWireframeMode = value; }
        }

        public bool RenderBoundingSpheres {
            get { return m_renderConfig.renderBoundingSpheres; }
            set { m_renderConfig.renderBoundingSpheres = value; }
        }

        #region SSScene Events
        public delegate void BeforeRenderObjectHandler(SSObject obj, SSRenderConfig renderConfig);
        public event BeforeRenderObjectHandler BeforeRenderObject;
        #endregion

        public void AddObject(SSObject obj) {
            m_objects.Add(obj);
        }

        public void RemoveObject(SSObject obj) {
            // todo threading
            m_objects.Remove(obj);
        }

        public void AddLight(SSLightBase light) {
            if (m_lights.Contains(light)) {
                return;
            }
            m_lights.Add(light);
            if (MainShader != null) {
                MainShader.SetupShadowMap(m_lights);
            }
			if (InstanceShader != null) {
				InstanceShader.SetupShadowMap(m_lights);
			}
        }

        public void RemoveLight(SSLightBase light) {
            if (!m_lights.Contains(light)) {
                throw new Exception ("Light not found.");
            }
            m_lights.Remove(light);
            if (MainShader != null) {
                MainShader.Activate();
            }
			if (InstanceShader != null) {
				InstanceShader.Activate();
			}
        }

        public SSObject Intersect(ref SSRay worldSpaceRay) {
            SSObject nearestIntersection = null;
            float nearestDistance = float.MinValue;
            // distances get "smaller" as they move in camera direction for some reason (why?)
            foreach (var obj in m_objects) {
                float distanceAlongRay;
                if (obj.Intersect(ref worldSpaceRay, out distanceAlongRay)) {
                    // intersection must be in front of the camera ( < 0.0 )
                    if (distanceAlongRay < 0.0) {
                        Console.WriteLine("intersect: [{0}] @distance: {1}", obj.Name, distanceAlongRay);
                        // then we want the nearest one (numerically biggest
                        if (distanceAlongRay > nearestDistance) {
                            nearestDistance = distanceAlongRay;
                            nearestIntersection = obj;
                        }
                    }
                }
            }

            return nearestIntersection;
        }

        public void Update(float fElapsedMS) {
            // update all objects.. TODO: add elapsed time since last update..
            foreach (var obj in m_objects) {
                obj.Update(fElapsedMS);
            }
        }

        #region Render Pass Logic
        public void RenderShadowMap(float fov, float aspect, float nearZ, float farZ) {
			// Shadow Map Pass(es)
            foreach (var light in m_lights) {
                if (light.ShadowMap != null) {
                    light.ShadowMap.PrepareForRender(m_renderConfig, m_objects, fov, aspect, nearZ, farZ);
                    renderPass(false, light.ShadowMap.FrustumCuller);
                    light.ShadowMap.FinishRender(m_renderConfig);
                }
            }
		}

        public void Render() {
			setupLighting ();
            
            // compute a world-space frustum matrix, so we can test against world-space object positions
            Matrix4 frustumMatrix = m_renderConfig.invCameraViewMat * m_renderConfig.projectionMatrix;
            renderPass(true, new Util3d.FrustumCuller(ref frustumMatrix));

            disableLighting();
        }

        private void setupLighting() {
            GL.Enable(EnableCap.Lighting);
            foreach (var light in m_lights) {
                light.SetupLight(ref m_renderConfig);
            }
            if (MainShader != null) {
                MainShader.Activate();
                MainShader.UniLightingMode = m_renderConfig.lightingMode;
            }
			if (InstanceShader != null) {
				InstanceShader.Activate();
				InstanceShader.UniLightingMode = m_renderConfig.lightingMode;
			}
        }

        private void disableLighting() {
            GL.Disable(EnableCap.Lighting);
            foreach (var light in m_lights) {
                light.DisableLight();
            }
        }

        private void renderPass(bool notifyBeforeRender, Util3d.FrustumCuller fc = null) {
            // reset stats
            m_renderConfig.renderStats = new SSRenderStats();

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref m_renderConfig.projectionMatrix);

            bool needObjectDelete = false;

            foreach (var obj in m_objects) {
                if (obj.renderState.toBeDeleted) { needObjectDelete = true; continue; }
                if (!obj.renderState.visible) continue; // skip invisible objects
                if (m_renderConfig.drawingShadowMap && !obj.renderState.castsShadow) continue; // skip non-shadow casters

                // frustum test... 
                #if true
				if (m_renderConfig.frustumCulling && obj.renderState.frustumCulling &&
                    fc != null &&
                    obj.boundingSphere != null &&
					!fc.isSphereInsideFrustum(obj.boundingSphere.Pos, obj.ScaledRadius)) {
                    m_renderConfig.renderStats.objectsCulled++;
                    continue; // skip the object
                }
                #endif

                // finally, render object
                if (notifyBeforeRender && BeforeRenderObject != null) {
                    BeforeRenderObject(obj, m_renderConfig);
                }
                m_renderConfig.renderStats.objectsDrawn++;
                obj.Render(ref m_renderConfig);
            }

            if (needObjectDelete) {
                m_objects.RemoveAll(o => o.renderState.toBeDeleted);
            }
        }

        #endregion
        

        public SSScene() {
            // Register SS types for loading by SSAssetManager
            SSAssetManager.RegisterLoadDelegate<SSTexture>(
                (ctx, filename) => { return new SSTexture(ctx, filename); }
            );
            SSAssetManager.RegisterLoadDelegate<SSTextureWithAlpha>(
                (ctx, filename) => { return new SSTextureWithAlpha(ctx, filename); }
            );
            SSAssetManager.RegisterLoadDelegate<SSMesh_wfOBJ>(
                (ctx, filename) => { return new SSMesh_wfOBJ(ctx, filename); }
            );
            SSAssetManager.RegisterLoadDelegate<SSVertexShader>(
                (ctx, filename) => { return new SSVertexShader(ctx, filename); }
            );
            SSAssetManager.RegisterLoadDelegate<SSFragmentShader>(
                (ctx, filename) => { return new SSFragmentShader(ctx, filename); }
            );
            SSAssetManager.RegisterLoadDelegate<SSGeometryShader>(
                (ctx, filename) => { return new SSGeometryShader(ctx, filename); }
            );
			SSAssetManager.RegisterLoadDelegate<SSSkeletalMeshMD5[]> (
				(ctx, filename) => { return SSMD5MeshParser.ReadMeshes(ctx, filename); }
			);
			SSAssetManager.RegisterLoadDelegate<SSSkeletalAnimationMD5> (
				(ctx, filename) => { return SSMD5AnimParser.ReadAnimation(ctx, filename); }
			);
        }
    }
}

