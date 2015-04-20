using System;

namespace SimpleScene
{
	public class SSSkeletalAnimationChannel
	{
		public SSSkeletalAnimation m_currAnimation = null;
		public SSSkeletalAnimation m_prevAnimation = null;
		public int[] m_topLevelJoints;

		public SSSkeletalAnimationChannel (int[] activeJoints)
		{
		}

		void ApplyChannels(SSSkeletalMesh mesh, 
						   SSSkeletalAnimationChannel[] channels)
		{

		}
	}
}

