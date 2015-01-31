// Copyright(C) David W. Jeske, 2013
// Released to the public domain. 

using System;
using System.IO;
using System.Collections.Generic;

namespace Util
{
	public static class Extensions
	{

		public static string AsString(this Stream input) {
			using (StreamReader reader = new StreamReader(input, System.Text.Encoding.UTF8)) {
				return reader.ReadToEnd();
			}
		}

        public static void ForEachN<T>(this List<T> list, Action<T,int> fn) {
            int n=0;
            list.ForEach( o => { fn(o,n); n++; } );            
        }

	}
}

