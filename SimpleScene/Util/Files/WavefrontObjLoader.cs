// WavefrontObjLoader.cs
//
// Wavefront .OBJ 3d fileformat loader in C# (csharp dot net)
//
// Copyright (C) 2012 David Jeske, and given to the public domain
//
// Originally Based on DXGfx code by Guillaume Randon, Copyright (C) 2005, BSD License (See below notice)
//
// BSD License  
// DXGfx® - http://www.eteractions.com
// Copyright (c) 2005
// by Guillaume Randon
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE 
// AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Wavefront OBJ file format documentation:
//
// http://en.wikipedia.org/wiki/Wavefront_.obj_file
// http://www.fileformat.info/format/wavefrontobj/egff.htm
// http://www.fileformat.info/format/material/
// http://www.martinreddy.net/gfx/3d/OBJ.spec
//
// NOTE: OBJ uses CIE-XYZ color space...
//
// http://www.codeproject.com/Articles/19045/Manipulating-colors-in-NET-Part-1
// 
// TODO: handle 'o' object names, and 'g' object groups
// TODO: handle negative vertex indices in face specification
// TODO: handle "s" smoothing group
// TODO: handle "Tr"/"d" material transparency/alpha
//
// NOTE: OBJ puts (0,0) in the Upper Left, OpenGL Lower Left, DirectX Lower Left
// 
// http://stackoverflow.com/questions/4233152/how-to-setup-calculate-texturebuffer-in-gltexcoordpointer-when-importing-from-ob


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;


using OpenTK;

using SimpleScene;
using SimpleScene.Util;


namespace SimpleScene.Util3d {

    public class WavefrontObjParseException : Exception {
        public WavefrontObjParseException(string reason) : base(reason) { }
    }

    public class WavefrontObjLoader {

        public struct Face {
            public Int16[] v_idx;
            public Int16[] n_idx;
            public Int16[] tex_idx;
        }
        
        public int numFaces = 0;        
        public int numIndices = 0;
        public bool hasNormals = false;        
        
        // these are all indexed by "raw" vertex number from the OBJ file
        // NOTE: these indicies are shared by the Faces in each material, so
        //       if you need per material indicies, you'll need to rebuild your own
        //       vertex lists and indicies.

        public List<Vector2> texCoords = new List<Vector2>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector4> positions = new List<Vector4>();

		public List<MaterialInfoWithFaces> materials = new List<MaterialInfoWithFaces>();

		private MaterialInfoWithFaces createImplicitMaterial() {
			MaterialInfoWithFaces makeMaterial = MaterialInfoWithFaces.CreateImplicitMaterialWithFaces();
            materials.Add(makeMaterial);
            return makeMaterial;
        }
            

        /// <summary>
        /// This method is used to load information stored in .mtl files referenced by the .obj file.
        /// </summary>
        /// <param name="d3ddevice"></param>
        /// <param name="file"></param>
       
        private void parseOBJ(string basePath, string filename) {
			MaterialInfoWithFaces currentMaterial = null;

            StreamReader sr = SSAssetManager.OpenStreamReader (Path.Combine(basePath, filename));

            //Read the first line of text
			string line = sr.ReadLine();

			//Continue to read until you reach end of file            
			while (line != null) 
			{
                // handle line continuation with "\"
                if (line.Length > 0) {
                    while (line[line.Length-1] == '\\') {
                        line = line.Substring(0,line.Length-1); // remove line extender..
                        var nextline = sr.ReadLine();
                        if (nextline != null && nextline.Length != 0) {
                            line = line + nextline; // merge with next line
                        } else {
                            break; // be sure to avoid infinite loop...
                        }
                    }
                }

                // split the line into tokens, separated by space
                string[] tokens = line.Split(" ".ToArray(), 2);
                if (tokens.Length < 2) {
                    goto next_line;
                }

                string firstToken = tokens[0];
                string lineContent = tokens[1];

                switch(firstToken) {
                        /* unsupported features - fatal */
                    case "cstype":    // curved surface type (bmatrix, bezier, bspline, cardinal, taylor)
                    case "deg":       // curve attr: degree
                    case "step":       // curve attr: step size
                    case "bmat":      // curve attr: basis matrix
                    case "surf":      // surface
                    case "parm":      // curve body: paramater value
                    case "trim":      // curve body: outer trimming loop
                    case "hole":      // curve body: inner trimming loop
                    case "scrv":      // curve body: special curve
                    case "sp":        // curve body: special point
                    case "end":       // curve body: end
                    case "con":       // connection between free form surfaces
                    case "vp":        // paramater space vertex (for free form surfaces)

                    case "bevel":     // bevel interpolation
                    case "c_interp":  // color interpolation
                    case "d_interp":  // dissolve interpolation
                    case "lod":       // level of detail                                        
                    case "ctech":     // Curve approximation technique
                    case "stech":     // Surface approximation technique
                    case "mg":        // merging group (for free form surfaces)

                        throw new WavefrontObjParseException("WavefrontObjLoader.cs: fatal error, token not supported :  " + firstToken);
                        /* unsupported features - warning */
                    case "o":         // object name                  
                    case "g":         // group name
                    case "s":         // smoothing group
                    case "shadow_obj":// shadow casting
                    case "trace_obj": // ray tracing
                        Console.WriteLine("WavefrontObjLoader.cs: warning - unsupported wavefront token : " + firstToken);
                        break;

                        /* supported features */
                    case "#":   // Nothing to read, these are comments.                        
                        break;
                    case "v":   // Vertex position
						positions.Add(WavefrontParser.readVector4(lineContent, null));
                        break;
                    case "vn":  // vertex normal direction vector
						normals.Add(WavefrontParser.readVector3(lineContent, null));   
                        break;
                    case "vt":  // Vertex texcoordinate
						texCoords.Add(WavefrontParser.readVector2(lineContent,null));
                        break;
                    case "f":   // Face                    
						string[] values = WavefrontParser.FilteredSplit(lineContent, null);
                        int numPoints = values.Length;
                    
                        Face face = new Face(); 
                        face.v_idx = new Int16[numPoints];
                        face.n_idx = new Int16[numPoints];
                        face.tex_idx = new Int16[numPoints];  // todo: how do outside clients know if there were texcoords or not?!?! 

                        for (int i = 0; i < numPoints; i++)
                        {
                            
                            // format is "loc_index[/tex_index[/normal_index]]"  e.g. 3 ; 3/2 ; 3/2/5
                            // but middle part can me empty, e.g. 3//5
                            string[] indexes = values[i].Split('/');    

                            int iPosition = (int.Parse(indexes[0]) - 1);  // adjust 1-based index                    
                            if (iPosition < 0) { iPosition += positions.Count + 1; } // adjust negative indicies
                            face.v_idx[i] = (Int16)iPosition; 
                            numIndices++;                
                            
                            // initialize other indicies to not provided, in case they are missing
                            face.n_idx[i] = -1;
                            face.tex_idx[i] = -1;
                            
                            if (indexes.Length > 1)
                            {
                                string tex_index = indexes[1];
                                if (tex_index != "") {
                                    int iTexCoord = int.Parse(tex_index) - 1; // adjust 1-based index
                                    if (iTexCoord < 0) { iTexCoord += texCoords.Count + 1; }  // adjust negative indicies

                                    face.tex_idx[i] = (Int16)iTexCoord;
                                }

                                if (indexes.Length > 2)
                                {    
                                    hasNormals = true;
                                    int iNormal = int.Parse(indexes[2]) - 1; // adjust 1 based index
                                    if (iNormal < 0) { iNormal += normals.Count + 1; } // adjust negative indicies

                                    face.n_idx[i] = (Int16)iNormal;                                
                                }
                            }
                        }
                        if (currentMaterial == null) {
                            // no material in file, so create one
                            currentMaterial = createImplicitMaterial();
                        }
                        currentMaterial.faces.Add(face);
                        currentMaterial.nbrIndices += face.v_idx.Length;
                        numFaces++;                                            
                        break;
                    case "mtllib":  // load named material file
                        string mtlFile = lineContent;
						{
                        var mtls = SSWavefrontMTLInfo.ReadMTLs (Path.Combine(basePath, mtlFile));
							foreach (var mtl in mtls) {
								materials.Add (new MaterialInfoWithFaces (mtl));
							}
						}
                        break;
                    case "usemtl":  // use named material (from material file previously loaded)
                        bool found = false;

                        string matName = lineContent;

                        for (int i = 0; i < materials.Count; i++)
                        {
                            if (matName.Equals(materials[i].mtl.name))
                            {
                                found = true;
                                currentMaterial = materials[i];                            
                            }
                        }

                        if (!found)
                        {
                            throw new WavefrontObjParseException("Materials are already loaded so we should have it!");
                        }
                        break;
                }                

            next_line:
				//Read the next line
				line = sr.ReadLine();
			}

			//close the file
			sr.Close();


            // debug print loaded stats
            Console.WriteLine("WavefrontObjLoader.cs: file processed...");
            Console.WriteLine("   vertex positions: {0}", positions.Count);
            Console.WriteLine("   vertex   normals: {0}", normals.Count);
            Console.WriteLine("   vertex texCoords: {0}", texCoords.Count);            
            foreach(var mtl in materials) {
                Console.WriteLine(
                              "           Material:      faces: {1}  indicies: {2}  ({0})", mtl.mtl.name, mtl.faces.Count, mtl.nbrIndices);
                Console.WriteLine( 
                              "                       diff Tex: {0}", mtl.mtl.diffuseTextureResourceName);                        
            }
            Console.WriteLine("WavefrontObjLoader.cs: end.");

        }

		public WavefrontObjLoader(string path) 
		{
            string basePath = Path.GetDirectoryName(path);
            string filename = Path.GetFileName(path); 
            this.parseOBJ(basePath, filename);
        }
        
        public static System.Drawing.Color CIEXYZtoColor(Vector4 xyzColor) {
            if (xyzColor.X + xyzColor.Y + xyzColor.Z < 0.01f) {
                return System.Drawing.Color.FromArgb(150, 150, 150);
            } else {
                // this is not a proper color conversion.. just a hack approximation..
                return System.Drawing.Color.FromArgb((int)(xyzColor.X * 255), (int)(xyzColor.Y * 255), (int)(xyzColor.Z * 255));
            }
        }

        public static Int32 CIEXYZtoRGB(Vector4 xyzColor) {
            if (xyzColor.X + xyzColor.Y + xyzColor.Z < 0.01f) {
                return System.Drawing.Color.FromArgb(150, 150, 150).ToArgb();
            } else {
                // this is not a proper color conversion.. just a hack approximation..
                return System.Drawing.Color.FromArgb((int)(xyzColor.X * 255), (int)(xyzColor.Y * 255), (int)(xyzColor.Z * 255)).ToArgb();
            }
        }

		public class MaterialInfoWithFaces
		{
			public static MaterialInfoWithFaces CreateImplicitMaterialWithFaces()
			{
				MaterialInfoWithFaces newMat = new MaterialInfoWithFaces(new SSWavefrontMTLInfo ());
				newMat.mtl.name = "[ implicit material ]";
				return newMat;
			}
		    
			public MaterialInfoWithFaces(SSWavefrontMTLInfo sourceMtl)
			{
				mtl = sourceMtl;
			}

			public SSWavefrontMTLInfo mtl;
			public List<Face> faces = new List<Face>();
			public int nbrIndices;
		}
    }
}
