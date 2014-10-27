// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Collections.Generic;


using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	public enum WireframeMode {
		None,
		GLSL_SinglePass,
		GL_Lines,
	};

	public struct SSRenderStats {
		public int objectsDrawn;
		public int objectsCulled;
	}     

	public class SSRenderConfig {
		public SSRenderStats renderStats;

		public SSMainShaderProgram BaseShader;
        public SSShadowMapShaderProgram ShadowMapShader;

		public bool drawGLSL = true;
		public bool useVBO = true;
        public bool drawingShadowMap = false;

		public bool renderBoundingSpheres;
		public bool renderCollisionShells;

		public bool frustumCulling;

		public WireframeMode drawWireframeMode;
		public Matrix4 invCameraViewMat;
		public Matrix4 projectionMatrix;

		public static void toggle(ref WireframeMode val) {
			int value = (int)val;
			value++;
			if (value > (int)WireframeMode.GL_Lines) {
				value = (int)WireframeMode.None;
			}
			val = (WireframeMode)value;
		}
	}

    public sealed class SSScene
    {
        public List<SSObject> objects = new List<SSObject>();
        public List<SSLight> lights = new List<SSLight>();

        public SSCamera activeCamera;

        public SSRenderConfig renderConfig = new SSRenderConfig();

        #region SSScene Events
        public delegate void BeforeRenderObjectHandler(SSObject obj, SSRenderConfig renderConfig);
        public event BeforeRenderObjectHandler BeforeRenderObject;
        #endregion


        public void Update(float fElapsedMS) {
            // update all objects.. TODO: add elapsed time since last update..
            foreach (var obj in objects) {
                obj.Update(fElapsedMS);
            }
        }

		private void SetupLights() {
            // setup the projection matrix
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref renderConfig.projectionMatrix);

            GL.Enable(EnableCap.Lighting);
            foreach (var light in lights) {
                light.SetupLight(ref renderConfig);
            }
        }

        public void setProjectionMatrix(Matrix4 projectionMatrix) {
            renderConfig.projectionMatrix = projectionMatrix;
        }

        public void setInvCameraViewMatrix(Matrix4 invCameraViewMatrix) {
            renderConfig.invCameraViewMat = invCameraViewMatrix;
        }

        public void Render() {
			SetupLights ();
            #if true
            // Shadow Map Pass(es)
            foreach (var light in lights) {
                if (light.ShadowMap != null) {
                    light.ShadowMap.PrepareForRender(ref renderConfig);
                    renderPass(false);
                    light.ShadowMap.FinishRender(ref renderConfig);
                }
            }
            #endif

            // load the projection matrix .. 
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref renderConfig.projectionMatrix);

            // compute a world-space frustum matrix, so we can test against world-space object positions
            Matrix4 frustumMatrix = renderConfig.invCameraViewMat * renderConfig.projectionMatrix;

            renderPass(true, new Util3d.FrustumCuller(ref frustumMatrix));
        }

        private void renderPass(bool notifyBeforeRender, Util3d.FrustumCuller fc = null) {
            // reset stats
            renderConfig.renderStats = new SSRenderStats();

            bool needObjectDelete = false;

            foreach (var obj in objects) {
                if (obj.renderState.toBeDeleted) { needObjectDelete = true; continue; }
                if (!obj.renderState.visible) continue; // skip invisible objects
                // frustum test... 
                if (renderConfig.frustumCulling &&
                    fc != null &&
                    obj.boundingSphere != null &&
                    !fc.isSphereInsideFrustum(obj.Pos, obj.boundingSphere.radius * obj.Scale.LengthFast)) {
                    renderConfig.renderStats.objectsCulled++;
                    continue; // skip the object
                }

                // finally, render object
                if (notifyBeforeRender && BeforeRenderObject != null) {
                    BeforeRenderObject(obj, renderConfig);
                }
                renderConfig.renderStats.objectsDrawn++;
                obj.Render(ref renderConfig);
            }

            if (needObjectDelete) {
                objects.RemoveAll(o => o.renderState.toBeDeleted);
            }
        }

        public void addObject(SSObject obj) {
            objects.Add(obj);
        }

        public void removeObject(SSObject obj) {
            // todo threading
            objects.Remove(obj);
        }

        public void addLight(SSLight light) {
            lights.Add(light);
        }

        public SSObject Intersect(ref SSRay worldSpaceRay) {
            SSObject nearestIntersection = null;
            float nearestDistance = float.MinValue;
            // distances get "smaller" as they move in camera direction for some reason (why?)
            foreach (var obj in objects) {
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

        public SSScene() {
            // Register SS types for loading by SSAssetManager
            SSAssetManager.RegisterLoadDelegate<SSTexture>(
                (ctx, filename) => { return new SSTexture(ctx, filename); }
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
        }
    }
}

