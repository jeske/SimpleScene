using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using OpenTK;
using OpenTK.Graphics;
using SimpleScene.Util3d;
using SimpleScene.Util;

namespace SimpleScene
{
	public enum SSWavefrontIlluminationMode {
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
	public class SSWavefrontMTLInfo {

		public string name;

		public bool hasAmbient;
		public Vector4 vAmbient;       // Ka

		public bool hasDiffuse;
		public Vector4 vDiffuse;       // Kd

		public bool hasSpecular;            
		public Vector4 vSpecular;      // Ks
		public float vSpecularWeight;  // Ns

		// textures
		public string ambientTextureResourceName;    // map_Ka
		public string diffuseTextureResourceName;    // map_Kd
		public string specularTextureResourceName;   // map_Ks
		public string bumpTextureResourceName;       // map_bump || bump

		// texture paramaters
		public float bumpIntensity = 1.0f;

		public bool hasIlluminationMode;
		public SSWavefrontIlluminationMode illuminationMode;  // illum

		public bool hasTransparency;
		public float fTransparency;


        public static SSWavefrontMTLInfo[] ReadMTLs(string path)
		{
			var materials = new List<SSWavefrontMTLInfo> ();
			SSWavefrontMTLInfo parseMaterial = null;

            StreamReader sr = SSAssetManager.OpenStreamReader(path);

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
					parseMaterial = new SSWavefrontMTLInfo();
					materials.Add(parseMaterial);
					parseMaterial.name = lineContent;
					break;
				case "Ka": // ambient color
					parseMaterial.vAmbient = WavefrontParser.readVector4(lineContent, null);
					parseMaterial.hasAmbient = true;
					break;
				case "Kd": // diffuse color
					parseMaterial.vDiffuse = WavefrontParser.readVector4(lineContent, null);
					parseMaterial.hasDiffuse = true;
					break;
				case "Ks": // specular color (weighted by Ns)                                 
					parseMaterial.vSpecular = WavefrontParser.readVector4(lineContent,null);
					parseMaterial.hasSpecular = true;
					break;
				case "Ns": // specular color weight                
					parseMaterial.vSpecularWeight = WavefrontParser.parseFloat(lineContent);   
					break;
				case "d":
				case "Tr": // transparency / dissolve (i.e. alpha)
					parseMaterial.fTransparency = WavefrontParser.parseFloat(lineContent);
					parseMaterial.hasTransparency = true;
					break;
				case "illum": // illumination mode                           
					parseMaterial.hasIlluminationMode = true;
					parseMaterial.illuminationMode = (SSWavefrontIlluminationMode) int.Parse(lineContent);
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
								parseMaterial.bumpIntensity = WavefrontParser.parseFloat(parts[2]);
							} else if (parts[0].Equals("-bm")) {
								parseMaterial.bumpTextureResourceName = parts[3];
								parseMaterial.bumpIntensity = WavefrontParser.parseFloat(parts[1]);
							}
						}
					}


					break;
				}

				next_line:
				//Read the next line
				line = sr.ReadLine();
			}

			//close the file
			sr.Close();

			return materials.ToArray();
		}
	}
}

