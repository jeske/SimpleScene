using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE 
// AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace Util3d {

    public class WavefrontObjParseException : Exception {
        public WavefrontObjParseException(string reason) : base(reason) { }
    }

    public class WavefrontObjLoader {
        // file format info...
        //
        // http://en.wikipedia.org/wiki/Wavefront_.obj_file
        // http://www.fileformat.info/format/wavefrontobj/egff.htm
        // http://www.fileformat.info/format/material/
        //
        // NOTE: OBJ uses CIE-XYZ color space...
        //
        // http://www.codeproject.com/Articles/19045/Manipulating-colors-in-NET-Part-1
        // 
        // TODO: handle multi-line faces continued with "\"
        // TODO: handle negative vertex indices in face specification
        // TODO: handle "s" smoothing group
        // TODO: handle "Tr"/"d" material transparency/alpha
        //
        // NOTE: OBJ puts (0,0) in the Upper Left, OpenGL Lower Left, DirectX Lower Left
        // 
        // http://stackoverflow.com/questions/4233152/how-to-setup-calculate-texturebuffer-in-gltexcoordpointer-when-importing-from-ob

        public struct Vector_UVW {
            public float U;  // horizontal texture direction (i.e. X)
            public float V;  // vertical texture direction (i.e. Y)
            public float W;  // optional depth of the texture (default 0)
            public Vector_UVW(float u, float v) {
                this.U = u; this.V = v; this.W = 0.0f;
            }
            public Vector_UVW(float u, float v, float w) {
                this.U = u; this.V = v; this.W = w;
            }
        }

        public struct Vector2 {
            public float X, Y;
            public Vector2(float x, float y) {
                this.X = x; this.Y = y;
            }
        }
        public struct Vector_XYZW {
            public float X, Y, Z, W;
            public Vector_XYZW(float x, float y, float z) {
                this.X = x; this.Y = y; this.Z = z; this.W = 1.0f;
            }
            public Vector_XYZW(float x, float y, float z, float w) {
                this.X = x; this.Y = y; this.Z = z; this.W = w;
            }
        }

        public struct Vector_XYZ {
            public float X, Y, Z;
            public Vector_XYZ(float x, float y, float z) {
                this.X = x; this.Y = y; this.Z = z; 
            }            
        }

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

        public List<Vector_UVW> texCoords = new List<Vector_UVW>();
        public List<Vector_XYZ> normals = new List<Vector_XYZ>();
        public List<Vector_XYZW> positions = new List<Vector_XYZW>();

        public List<MaterialFromObj> materials = new List<MaterialFromObj>();

        public enum WffObjIlluminationMode {
            ColorOnAmbientOff = 0,
            ColorOnAmbiendOn = 1,
            HighlightOn = 2,
            ReflectionOnRayTraceOn = 3,
            TransparentyGlassOn_ReflectionRayTraceOn = 4,
            ReflectionFresnelAndRayTraceOn = 5,
            TransparencyRefractionOn_ReflectionFresnelOffRayTraceOn = 6,
            TransparentyRefractionOn_ReflectionFresnelOnRayTraceOn = 7,
            ReflectionOn_RayTraceOff = 8,
            TransparencyGlassOn_ReflectionRayTraceOff = 9,
            CastsShadowsOntoInvisibleSurfaces = 10
        }

        /// <summary>
        /// This structure is used to store material information.
        /// </summary>
        public class MaterialFromObj {
            public string name;

            public bool hasAmbient;
            public Vector_XYZW vAmbient;       // Ka

            public bool hasDiffuse;
            public Vector_XYZW vDiffuse;       // Kd
            
            public bool hasSpecular;            
            public Vector_XYZW vSpecular;      // Ks
            public float vSpecularWeight;  // Ns

            // textures
            public string ambientTextureResourceName;    // map_Ka
            public string diffuseTextureResourceName;    // map_Kd
            public string specularTextureResourceName;   // map_Ks
            public string bumpTextureResourceName;       // map_bump || bump
            
            // texture paramaters
			public float bumpIntensity = 1.0f;

            public bool hasIlluminationMode;
            public WffObjIlluminationMode illuminationMode;  // illum

            public bool hasTransparency;
            public float fTransparency;

            public int nbrIndices;
            public List<Face> faces = new List<Face>();
        }


        /// <summary>
        /// This method is used to split string in a list of strings based on the separator passed to hte method.
        /// </summary>
        /// <param name="strIn"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        private string[] FilteredSplit(string strIn, char[] separator) {
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

        private void ASSERT(bool test_true, string reason) {
            if (!test_true) {
                throw new WavefrontObjParseException("WavefrontObjLoader Error: " + reason);
            }
        }

        private Vector_XYZW readVector_XYZW(string strIn, char[] separator) {
            string[] values = FilteredSplit(strIn, separator);

            if (values.Length == 3) {       // W optional
                return new Vector_XYZW(
                    float.Parse(values[0]),
                    float.Parse(values[1]),
                    float.Parse(values[2]));
            } else if (values.Length == 4) {
                return new Vector_XYZW(
                    float.Parse(values[0]),
                    float.Parse(values[1]),
                    float.Parse(values[2]),
                    float.Parse(values[3]));
            } else {
                throw new WavefrontObjParseException("readVector_XYZW found wrong number of vectors : " + strIn);
            }
        }
        private Vector_XYZ readVector_XYZ(string strIn, char[] separator) {
            string[] values = FilteredSplit(strIn, separator);

            if (values.Length == 3) {
                return new Vector_XYZ(
                    float.Parse(values[0]),
                    float.Parse(values[1]),
                    float.Parse(values[2]));
            } else {
                throw new WavefrontObjParseException("readVector_XYZ found wrong number of vectors : " + strIn);
            }
        }
        

        private Vector2 readVector2(string strIn, char[] separator) {
            string[] values = FilteredSplit(strIn, separator);

            ASSERT(values.Length == 2, "readVector2 found wrong number of vectors : " + strIn);
            return new Vector2(
                float.Parse(values[0]),
                float.Parse(values[1]));

        }

        private Vector_UVW readVector_UVW(string strIn, char[] separator) {
            string[] values = FilteredSplit(strIn, separator);

            
            if (values.Length == 2) {       // W optional
                return new Vector_UVW(
                    float.Parse(values[0]),
                    float.Parse(values[1]));
            } if (values.Length == 3) {
                return new Vector_UVW(
                    float.Parse(values[0]),
                    float.Parse(values[1]),
                    float.Parse(values[2]));
            } else {
                throw new WavefrontObjParseException("readVector_UVW found wrong number of vectors : " + strIn);                
            }
        }


        private MaterialFromObj createImplicitMaterial() {
            MaterialFromObj makeMaterial = new MaterialFromObj();
            materials.Add(makeMaterial);

            makeMaterial.name = "[ Implicit Material ]";

            return makeMaterial;            
        }
            

        /// <summary>
        /// This method is used to load information stored in .mtl files referenced by the .obj file.
        /// </summary>
        /// <param name="d3ddevice"></param>
        /// <param name="file"></param>
       
        public void parseMTL(string file) {
            MaterialFromObj parseMaterial = new MaterialFromObj();
            
            bool first = true;

            // ask the delegate to open the material file for us..
            StreamReader sr = new StreamReader(this.opendelegate(file));            

            //Read the first line of text
            string line = sr.ReadLine();

            //Continue to read until you reach end of file
            while (line != null) {
                string[] tokens = line.Split(" ".ToArray(), 2);
                if (tokens.Length < 2) {
                    goto next_line;
                }

                string firstToken = tokens[0];
                string lineContent = tokens[1];


                switch (firstToken) {
                    case "#":
                        // Nothing to read, these are comments.
                        break;
                    case "newmtl":  // create new named material                
                        if (!first) {
                            materials.Add(parseMaterial);
                            parseMaterial = new MaterialFromObj();
                        }
                        first = false;
                        parseMaterial.name = lineContent;
                        break;
                    case "Ka": // ambient color
                        parseMaterial.vAmbient = readVector_XYZW(lineContent, null);
                        parseMaterial.hasAmbient = true;
                        break;
                    case "Kd": // diffuse color
                        parseMaterial.vDiffuse = readVector_XYZW(lineContent, null);
                        parseMaterial.hasDiffuse = true;
                        break;
                    case "Ks": // specular color (weighted by Ns)                                 
                        parseMaterial.vSpecular = readVector_XYZW(lineContent,null);
                        parseMaterial.hasSpecular = true;
                        break;
                    case "Ns": // specular color weight                
                        parseMaterial.vSpecularWeight = float.Parse(lineContent);   
                        break;
                    case "d":
                    case "Tr": // transparency / dissolve (i.e. alpha)
                        parseMaterial.fTransparency = float.Parse(lineContent);
                        parseMaterial.hasTransparency = true;
                        break;
                    case "illum": // illumination mode                           
                        parseMaterial.hasIlluminationMode = true;
                        parseMaterial.illuminationMode = (WffObjIlluminationMode) int.Parse(lineContent);
                        break;
                    case "map_Kd": // diffuse color map                
                        parseMaterial.diffuseTextureResourceName = lineContent;
                        break;
                    case "map_Ka": // ambient color map
                        parseMaterial.ambientTextureResourceName = lineContent;
                        break;
                    case "map_Ks": // specular color map                
                        parseMaterial.specularTextureResourceName = lineContent;
                        break;
                    case "bump": 
					case "map_Bump":
                    case "map_bump": // bump map  
                        // bump <filename> [-bm <float intensity>]             
                        // bump -bm <float intensity> <filename>
                        string[] parts = lineContent.Split(' ');
                        if (parts.Length == 1) {
                            parseMaterial.bumpTextureResourceName = parts[0];
                        } else {
                            if (parts.Length == 3) {
                                if (parts[1].Equals("-bm")) {
                                    parseMaterial.bumpTextureResourceName = parts[0];
                                    parseMaterial.bumpIntensity = float.Parse(parts[2]);
                                } else if (parts[0].Equals("-bm")) {
                                    parseMaterial.bumpTextureResourceName = parts[3];
                                    parseMaterial.bumpIntensity = float.Parse(parts[1]);
                                }
                            }
                        }
                        
                        
                        break;
                }

            next_line:
                //Read the next line
                line = sr.ReadLine();
            }
            materials.Add(parseMaterial);

            //close the file
            sr.Close();
        }

        private void parseOBJ(StreamReader sr) {
            MaterialFromObj currentMaterial = null;

            //Read the first line of text
			string line = sr.ReadLine();

			//Continue to read until you reach end of file            
			while (line != null) 
			{
                string[] tokens = line.Split(" ".ToArray(), 2);
                if (tokens.Length < 2) {
                    goto next_line;
                }

                string firstToken = tokens[0];
                string lineContent = tokens[1];

                switch(firstToken) {
                    case "#":   // Nothing to read, these are comments.                        
                        break;
                    case "v":   // Vertex position
                        positions.Add(readVector_XYZW(lineContent, null));
                        break;
                    case "vn":  // vertex normal direction vector
                        normals.Add(readVector_XYZ(lineContent, null));   
                        break;
                    case "vt":  // Vertex texcoordinate
                        texCoords.Add(readVector_UVW(lineContent,null));
                        break;
                    case "f":   // Face                    
                        string[] values = FilteredSplit(lineContent, null);
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
                        parseMTL(mtlFile);
                        break;
                    case "usemtl":  // use named material (from material file previously loaded)
                        bool found = false;

                        string matName = lineContent;

                        for (int i = 0; i < materials.Count; i++)
                        {
                            if (matName.Equals(materials[i].name))
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
        }

        public delegate Stream openNamedFile(string resource_name); 
        openNamedFile opendelegate;

        public WavefrontObjLoader(string obj_file_name, openNamedFile opendelegate) {
            this.opendelegate = opendelegate;
            Stream fs = opendelegate(obj_file_name);            
            this.parseOBJ(new StreamReader(fs));
        }
        
        public static System.Drawing.Color CIEXYZtoColor(Vector_XYZW xyzColor) {
            if (xyzColor.X + xyzColor.Y + xyzColor.Z < 0.01f) {
                return System.Drawing.Color.FromArgb(150, 150, 150);
            } else {
                // this is not a proper color conversion.. just a hack approximation..
                return System.Drawing.Color.FromArgb((int)(xyzColor.X * 255), (int)(xyzColor.Y * 255), (int)(xyzColor.Z * 255));
            }
        }

        public static Int32 CIEXYZtoRGB(Vector_XYZW xyzColor) {
            if (xyzColor.X + xyzColor.Y + xyzColor.Z < 0.01f) {
                return System.Drawing.Color.FromArgb(150, 150, 150).ToArgb();
            } else {
                // this is not a proper color conversion.. just a hack approximation..
                return System.Drawing.Color.FromArgb((int)(xyzColor.X * 255), (int)(xyzColor.Y * 255), (int)(xyzColor.Z * 255)).ToArgb();
            }
        }

    }
}
