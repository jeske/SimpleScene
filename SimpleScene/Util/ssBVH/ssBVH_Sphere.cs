using System;
using System.Collections.Generic;
using OpenTK;

using SimpleScene;

namespace SimpleScene.Util.ssBVH
{
	public class SSSphereBVHNodeAdaptor : SSBVHNodeAdaptor<SSSphere>
	{
		protected ssBVH<SSSphere> _BVH;
		protected Dictionary <SSSphere, ssBVHNode<SSSphere>> sphereToLeafMap 
			= new Dictionary <SSSphere, ssBVHNode<SSSphere>>();
		public ssBVH<SSSphere> BVH { get { return _BVH; } }

		public void setBVH(ssBVH<SSSphere> BVH)
		{
			_BVH = BVH;
		}

		public Vector3 objectpos(SSSphere sphere)
		{
			return sphere.center;
		}

		public float radius(SSSphere sphere)
		{
			return sphere.radius;
		}

		public void checkMap(SSSphere sphere) 
		{
			if (!sphereToLeafMap.ContainsKey (sphere)) {
				throw new Exception("missing map for shuffled child");
			}
		}

		public void unmapObject(SSSphere sphere) 
		{
			sphereToLeafMap.Remove(sphere);
		}

		public void mapObjectToBVHLeaf(SSSphere sphere, ssBVHNode<SSSphere> leaf) 
		{  
			sphereToLeafMap[sphere] = leaf;
		}

		public ssBVHNode<SSSphere> getLeaf(SSSphere sphere)
		{
			return sphereToLeafMap [sphere];
		}
	}

	public class SSSphereBVH : ssBVH<SSSphere>
	{
		public SSSphereBVH(int maxSpheresPerLeaf=1)
			: base(new SSSphereBVHNodeAdaptor(),
				   new List<SSSphere>(),
				   maxSpheresPerLeaf)
		{
		}
	}
}

