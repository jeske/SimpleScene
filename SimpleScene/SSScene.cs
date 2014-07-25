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

		public bool drawGLSL = true;
		public bool useVBO = true;

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
		public ICollection<SSObject> objects = new List<SSObject>();
		public ICollection<SSLight> lights = new List<SSLight> ();

		public SSCamera activeCamera;

		public SSRenderConfig renderConfig = new SSRenderConfig ();

		public void Update() {
			// update all objects.. TODO: add elapsed time since last update..
			foreach (var obj in objects) {
				obj.Update ();
			}
		}

		public void SetupLights() {
			// setup the projection matrix
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix(ref renderConfig.projectionMatrix);

			GL.Enable (EnableCap.Lighting);
			foreach (var light in lights) {
				light.SetupLight (ref renderConfig);
			}
		}
			
		public void setProjectionMatrix (Matrix4 projectionMatrix) {
			renderConfig.projectionMatrix = projectionMatrix;
		}
		
		public void setInvCameraViewMatrix (Matrix4 invCameraViewMatrix) {
			renderConfig.invCameraViewMat = invCameraViewMatrix;
		}

		public void Render ()
		{			
			// reset stats
			renderConfig.renderStats = new SSRenderStats();

			// load the projection matrix .. 
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix(ref renderConfig.projectionMatrix);

			// compute a world-space frustum matrix, so we can test against world-space object positions
			Matrix4 frustumMatrix = renderConfig.invCameraViewMat * renderConfig.projectionMatrix;			
			var fc = new Util3d.FrustumCuller (ref frustumMatrix); 
			
			foreach (var obj in objects) {					
				// frustum test... currently still broken
				if (renderConfig.frustumCulling &&
					obj.boundingSphere != null && !fc.isSphereInsideFrustum(obj.Pos,obj.boundingSphere.radius)) {
					renderConfig.renderStats.objectsCulled++;
					continue; // skip the object
				}

				// finally, render object
				renderConfig.renderStats.objectsDrawn++;
				obj.Render (ref renderConfig);
			}
		}

		public void addObject(SSObject obj) {
			objects.Add (obj);
		}

		public void removeObject(SSObject obj) {
		    // todo threading
		    objects.Remove(obj);
		}

		public void addLight(SSLight light) {
			lights.Add (light);
		}

		public void Intersect(ref SSRay worldSpaceRay) {
		    foreach (var obj in objects) {
		        bool result = obj.Intersect(ref worldSpaceRay);
		    }
		}

		public SSScene ()  {  }
	}
}

