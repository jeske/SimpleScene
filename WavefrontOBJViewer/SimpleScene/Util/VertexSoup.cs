using System;
using System.Collections.Generic;
// using System.Linq;
using System.Text;

using System.Drawing;

namespace Util3d {

    public class VertexSoup<VERTEX_STRUCT> where VERTEX_STRUCT : struct {
        
        Dictionary<VERTEX_STRUCT,Int16> vertexToIndexMap = new Dictionary<VERTEX_STRUCT,Int16>();
        public List<VERTEX_STRUCT> verticies = new List<VERTEX_STRUCT>();

        public Int16[] digestVerticies(VERTEX_STRUCT[] vertex_list) {
            Int16[] retval = new Int16[vertex_list.Length];

            for(int x=0;x<vertex_list.Length;x++) {            
                if (vertexToIndexMap.ContainsKey(vertex_list[x])) {
                    retval[x] = vertexToIndexMap[vertex_list[x]];
                } else {
                    Int16 nextIndex = (Int16)verticies.Count;
                    vertexToIndexMap[vertex_list[x]] = nextIndex;
                    verticies.Add(vertex_list[x]);
                    retval[x] = nextIndex;

                }
            }
            return retval;
        }
        
        public VertexSoup() {
        }

    }
}


