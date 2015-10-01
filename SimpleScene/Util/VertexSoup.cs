// Copyright(C) David W. Jeske, 2012
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;

namespace SimpleScene.Util3d {

	// VertexSoup
	//
	// This class is used to accumulate a uniquefied set of fully-configured-3d verticies
	// for 3d rendering. Each VERTEX_STRUCT passed to digestVerticies() is either mapped
	// to an existing matching vertex, or issued a new vertex index. 
	//
	// default structure hashcode is bitwise XOR if it contains no gaps and no references
	// default structure equality is bitwise equality
	// 
	// if you define your own GetHashCode() or Equals() 
	// -or- your struct contains referneces, be careful
	
    public class VertexSoup<VERTEX_STRUCT> {
        
        Dictionary<VERTEX_STRUCT,UInt16> vertexToIndexMap = new Dictionary<VERTEX_STRUCT,UInt16>();
        public List<VERTEX_STRUCT> verticies = new List<VERTEX_STRUCT>();
        private readonly bool deDup;

		public UInt16 digestVertex(ref VERTEX_STRUCT vertex) {
			UInt16 retval;
		    if (deDup && vertexToIndexMap.ContainsKey(vertex)) {
                retval = vertexToIndexMap[vertex];
            } else {
                UInt16 nextIndex = (UInt16)verticies.Count;
                vertexToIndexMap[vertex] = nextIndex;
                verticies.Add(vertex);
                retval = nextIndex;
            }

			return retval;
		}

        public UInt16[] digestVerticies(VERTEX_STRUCT[] vertex_list) {
            UInt16[] retval = new UInt16[vertex_list.Length];

            for(int x=0;x<vertex_list.Length;x++) {
				retval[x] = digestVertex(ref vertex_list[x]);          
            }
            return retval;
        }
        
        public VertexSoup(bool deDup = true) {
            this.deDup = deDup;
        }
    }
}


