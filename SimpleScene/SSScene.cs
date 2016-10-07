// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Collections.Generic;
using System.Diagnostics;

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

		public readonly SSMainShaderProgram mainShader;
        public readonly SSPssmShaderProgram pssmShader;
        public readonly SSInstanceShaderProgram instanceShader;
		public readonly SSInstancePssmShaderProgram instancePssmShader;
        public readonly Dictionary<string, SSShaderProgram> otherShaders;

		public bool drawGLSL = true;
		public bool useVBO = true;
        public bool drawingShadowMap = false;
		public bool drawingPssm = false;

        public bool usePoissonSampling = false;
        public int numPoissonSamples = 8;
        public SSMainShaderProgram.LightingMode lightingMode = SSMainShaderProgram.LightingMode.BlinnPhong;
		//public SSMainShaderProgram.LightingMode lightingMode = SSMainShaderProgram.LightingMode.ShadowMapDebug;

        public bool enableFaceCulling = true;
        public CullFaceMode cullFaceMode = CullFaceMode.Back;
        public bool enableDepthClamp = true;

		public bool renderBoundingSpheresLines = false;
		public bool renderBoundingSpheresSolid = false;

        public bool frustumCulling = true;

		public WireframeMode drawWireframeMode;
		public Matrix4 invCameraViewMatrix = Matrix4.Identity;
		public Matrix4 projectionMatrix = Matrix4.Identity;
        public float timeElapsedS = 0f; // time elapsed since last render update

		public ISSInstancableShaderProgram ActiveInstanceShader {
			get {
				if (instanceShader != null && instanceShader.IsActive) {
					return instanceShader;
				} else if (instancePssmShader != null && instancePssmShader.IsActive) {
					return instancePssmShader;
				}
				return null;
			}
		}

		public SSMainShaderProgram ActiveDrawShader {
			get {
				if (mainShader != null && mainShader.IsActive) {
					return mainShader;
				} else if (instanceShader != null && instanceShader.IsActive) {
					return instanceShader;
				}
				return null;
			}
		}

		public static WireframeMode NextWireFrameMode(WireframeMode val) {
			int newVal = (int)val;
			newVal++;
			if (newVal > (int)WireframeMode.GL_Lines) {
				newVal = (int)WireframeMode.None;
			}
			return (WireframeMode)newVal;
		}

		public SSRenderConfig(SSMainShaderProgram main,
							  SSPssmShaderProgram pssm,
							  SSInstanceShaderProgram instance,
							  SSInstancePssmShaderProgram instancePssm,
                              Dictionary<string, SSShaderProgram> other
        )
		{
			mainShader = main;
			pssmShader = pssm;
			instanceShader = instance;
			instancePssmShader = instancePssm;
            otherShaders = other;
		}
	}

    public sealed class SSScene
    {
		public delegate void SceneUpdateDelegate(float timeElapsedS);

        private SSCamera activeCamera = null;
        public readonly SSRenderConfig renderConfig;
        public List<SSObject> objects = new List<SSObject>();
        public List<SSLightBase> lights = new List<SSLightBase>();
		public event SceneUpdateDelegate preUpdateHooks;
        public event SceneUpdateDelegate postUpdateHooks;
        public event SceneUpdateDelegate preRenderHooks;
        public event SceneUpdateDelegate postRenderHooks;
        public Stopwatch renderStopwatch = new Stopwatch ();

        public SSCamera ActiveCamera { 
            get { return activeCamera; }
            set { activeCamera = value; }
        }

		public SSScene(SSMainShaderProgram main = null,
					   SSPssmShaderProgram pssm = null,
					   SSInstanceShaderProgram instance = null,
					   SSInstancePssmShaderProgram instancePssm = null,
                       Dictionary<string, SSShaderProgram> otherShaders = null)
		{
            renderConfig = new SSRenderConfig (main, pssm, instance, instancePssm, otherShaders);
		}

        #region SSScene Events
        public delegate void BeforeRenderObjectHandler(SSObject obj, SSRenderConfig renderConfig);
        public event BeforeRenderObjectHandler BeforeRenderObject;
        #endregion

        public void AddObject(SSObject obj) {
            if (objects.Contains(obj)) {
                throw new Exception ("scene already contains object: " + obj);
            }
            objects.Add(obj);
        }

        public void RemoveObject(SSObject obj) {
            // todo threading
            objects.Remove(obj);
        }

        public void AddLight(SSLightBase light) {
            if (lights.Contains(light)) {
                return;
            }
            lights.Add(light);
			if (renderConfig.mainShader != null) {
				renderConfig.mainShader.SetupShadowMap(lights);
            }
			if (renderConfig.instanceShader != null) {
				renderConfig.instanceShader.SetupShadowMap(lights);
			}
        }

        public void RemoveLight(SSLightBase light) {
            if (!lights.Contains(light)) {
                throw new Exception ("Light not found.");
            }
            lights.Remove(light);
            if (renderConfig.mainShader != null) {
                renderConfig.mainShader.Activate();
            }
			if (renderConfig.instanceShader != null) {
                renderConfig.instanceShader.Activate();
			}
        }

        public SSObject Intersect(ref SSRay worldSpaceRay, out float nearestDistance) {
            SSObject nearestIntersection = null;
            nearestDistance = float.PositiveInfinity;
            // distances get "smaller" as they move in camera direction for some reason (why?)
            foreach (var obj in objects) {
                float distanceAlongRay;
                DateTime start = DateTime.Now;
				if (obj.selectable && obj.Intersect(ref worldSpaceRay, out distanceAlongRay)) {
                    // intersection must be in front of the camera ( < 0.0 )
                    if (distanceAlongRay > 0.0) {
                        Console.WriteLine("intersect: [{0}] @distance: {1}  in {2}ms", obj.Name, distanceAlongRay, (DateTime.Now - start).TotalMilliseconds);
                        // then we want the nearest one (numerically biggest
                        if (distanceAlongRay < nearestDistance) {
                            nearestDistance = distanceAlongRay;
                            nearestIntersection = obj;
                        }
                    }
                }
            }
            return nearestIntersection;
        }

        public void Update(float fElapsedS) {
			if (preUpdateHooks != null) {
				preUpdateHooks (fElapsedS);
			}
            // update all objects.. TODO: add elapsed time since last update..
            foreach (var obj in objects) {
                obj.Update(fElapsedS);
            }

            if (postUpdateHooks != null) {
                postUpdateHooks(fElapsedS);
            }
        }

        #region Render Pass Logic
        public void RenderShadowMap(float fov, float aspect, float nearZ, float farZ) {
            // clear some basics 
            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.Texture2D);
            GL.ShadeModel(ShadingModel.Flat);
            GL.Disable(EnableCap.ColorMaterial);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Front);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.DepthClamp);
            GL.DepthMask(true);

			// Shadow Map Pass(es)
            foreach (var light in lights) {
                if (light.ShadowMap != null) {
                    light.ShadowMap.PrepareForRender(renderConfig, objects, fov, aspect, nearZ, farZ);
                    renderPass(false, light.ShadowMap.FrustumCuller);
                    light.ShadowMap.FinishRender(renderConfig);
                }
            }
		}

        public void Render() {
            renderConfig.timeElapsedS = (float)renderStopwatch.ElapsedMilliseconds/1000f;
            renderStopwatch.Restart();
            if (preRenderHooks != null) {
                preRenderHooks(renderConfig.timeElapsedS);
            }

            if (renderConfig.enableFaceCulling) {
                GL.Enable(EnableCap.CullFace);
                GL.CullFace(renderConfig.cullFaceMode);
            } else {
                GL.Disable(EnableCap.CullFace);
            }
            if (renderConfig.enableDepthClamp) {
                GL.Enable(EnableCap.DepthClamp);
            } else {
                GL.Disable(EnableCap.DepthClamp);
            }

			setupLighting ();
            
            // compute a world-space frustum matrix, so we can test against world-space object positions
            Matrix4 frustumMatrix = renderConfig.invCameraViewMatrix * renderConfig.projectionMatrix;
            renderPass(true, new SimpleScene.Util3d.SSFrustumCuller(ref frustumMatrix));

            disableLighting();

            if (postRenderHooks != null) {
                postRenderHooks(renderConfig.timeElapsedS);
            }
        }

        private void setupLighting() {
            foreach (var light in lights) {
                light.setupLight(renderConfig);
            }
            if (renderConfig.mainShader != null) {
                renderConfig.mainShader.Activate();
                renderConfig.mainShader.UniLightingMode = renderConfig.lightingMode;
            }
            if (renderConfig.instanceShader != null) {
                renderConfig.instanceShader.Activate();
                renderConfig.instanceShader.UniLightingMode = renderConfig.lightingMode;
			}
        }

        private void disableLighting() {
            foreach (var light in lights) {
                light.DisableLight(renderConfig);
            }
        }

        private void renderPass(bool notifyBeforeRender, SimpleScene.Util3d.SSFrustumCuller fc = null) {
            // reset stats
            renderConfig.renderStats = new SSRenderStats();

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref renderConfig.projectionMatrix);

            bool needObjectDelete = false;

            foreach (var obj in objects) {
				if (obj.preRenderHook != null) {
					obj.preRenderHook (obj, renderConfig);
				}

                if (obj.renderState.toBeDeleted) { needObjectDelete = true; continue; }
                if (!obj.renderState.visible) continue; // skip invisible objects
                if (renderConfig.drawingShadowMap && !obj.renderState.castsShadow) continue; // skip non-shadow casters

                // frustum test... 
                #if true
				if (renderConfig.frustumCulling 
				 && obj.localBoundingSphereRadius >= 0f
				 && obj.renderState.frustumCulling
                 && fc != null 
				 && !fc.isSphereInsideFrustum(obj.worldBoundingSphere)) {
                    renderConfig.renderStats.objectsCulled++;
                    continue; // skip the object
                }
                #endif

                // finally, render object
                if (notifyBeforeRender && BeforeRenderObject != null) {
                    BeforeRenderObject(obj, renderConfig);
                }
                renderConfig.renderStats.objectsDrawn++;
                obj.Render(renderConfig);
            }

            if (needObjectDelete) {
                objects.RemoveAll(o => o.renderState.toBeDeleted);
            }
        }

        #endregion      
    }
}

