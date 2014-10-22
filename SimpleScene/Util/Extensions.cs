// Copyright(C) David W. Jeske, 2013
// Released to the public domain. 

using System;
using System.IO;



namespace SimpleScene
{
	public static class Extensions
	{

		public static string AsString(this Stream input) {
			using (StreamReader reader = new StreamReader(input, System.Text.Encoding.UTF8)) {
				return reader.ReadToEnd();
			}
		}
	}
}

