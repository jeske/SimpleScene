using System;
using System.IO;
using System.Collections.Generic;

namespace SimpleScene
{
	/// <summary>
	/// Stores a currently useful subset of data from Doom3 .mtr files
	/// http://wiki.thedarkmod.com/index.php?title=Basic_Material_File
	/// </summary>
	public class SSDoomMTRInfo
	{
		public string MaterialInfo;
		public string DiffuseMap;
		public string BumpMap;
		public string SpecularMap;

		private static readonly char[] _wordDelimeters = {' ', '\t' };

		public static SSDoomMTRInfo[] ReadMTRs(SSAssetManager.Context ctx, string filename)
		{
			var materials = new List<SSDoomMTRInfo> ();
			SSDoomMTRInfo newMat = null;

			StreamReader reader = ctx.OpenText (filename);
			while (!reader.EndOfStream) {
				string line = reader.ReadLine ();
				string[] words = line.Split (_wordDelimeters);
				if (words.Length > 0) {

				}
			}

			return materials.ToArray ();
		}

	}


}

