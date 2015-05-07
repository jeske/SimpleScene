using System;
using OpenTK;
using System.Globalization;
using System.Collections.Generic;


namespace SimpleScene
{
	public static class WavefrontParser
	{

		public static float parseFloat(string data) {
			// we have to use InvariantCulture to get the float-format parsing we expect
			return float.Parse(data, CultureInfo.InvariantCulture);            
		}
		/// <summary>
		/// This method is used to split string in a list of strings based on the separator passed to hte method.
		/// </summary>
		/// <param name="strIn"></param>
		/// <param name="separator"></param>
		/// <returns></returns>
		public static string[] FilteredSplit(string strIn, char[] separator) {
			string[] valuesUnfiltered = strIn.Split(separator);

			// Sometime if we have a white space at the beginning of the string, split
			// will remove an empty string. Let's remove that.
			List<string> listOfValues = new List<string>();
			foreach (string str in valuesUnfiltered) {
				if (str != "") {
					listOfValues.Add(str);
				}
			}
			string[] values = listOfValues.ToArray();

			return values;
		}

		public static Vector4 readVector4(string strIn, char[] separator) {
			string[] values = FilteredSplit(strIn, separator);

			if (values.Length == 3) {       // W optional
				return new Vector4(
					parseFloat(values[0]), 
					parseFloat(values[1]),
					parseFloat(values[2]),
					0f);
			} else if (values.Length == 4) {
				return new Vector4(
					parseFloat(values[0]),
					parseFloat(values[1]),
					parseFloat(values[2]),
					parseFloat(values[3]));
			} else {
				throw new Exception("readVector4 found wrong number of vectors : " + strIn);
			}
		}

		public static Vector3 readVector3(string strIn, char[] separator) {
			string[] values = FilteredSplit(strIn, separator);

			if (values.Length == 3) {
				return new Vector3(
					parseFloat(values[0]),
					parseFloat(values[1]),
					parseFloat(values[2]));
			} else {
				throw new Exception("readVector3 found wrong number of vectors : " + strIn);
			}
		}


		public static Vector2 readVector2(string strIn, char[] separator) {
			string[] values = FilteredSplit(strIn, separator);

			ASSERT(values.Length == 2, "readVector2 found wrong number of vectors : " + strIn);
			return new Vector2(
				parseFloat(values[0]),
				parseFloat(values[1]));

		}

		private static void ASSERT(bool test_true, string reason) {
			if (!test_true) {
				throw new Exception("WavefrontParser Error: " + reason);
			}
		}
	}
}

