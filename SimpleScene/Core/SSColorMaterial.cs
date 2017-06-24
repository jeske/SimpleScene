using System;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using SimpleScene.Util;

namespace SimpleScene
{
    public class SSColorMaterial
    {
        public static readonly SSColorMaterial defaultColorMat = new SSColorMaterial();
        public static readonly SSColorMaterial pureAmbient = new SSColorMaterial (
            Color4Helper.Zero, Color4Helper.Full, Color4Helper.Zero, Color4Helper.Zero, 0f);

        public Color4 ambientColor = new Color4(0.0006f,0.0006f,0.0006f,1.0f);
        public Color4 diffuseColor = new Color4(0.3f, 0.3f, 0.3f, 1f);
        public Color4 specularColor = new Color4(0.6f, 0.6f, 0.6f, 1f);
        public Color4 emissionColor = new Color4(0.001f, 0.001f, 0.001f, 1f);
        public float shininess = 10.0f;

        public static SSColorMaterial fromMtl(SSWavefrontMTLInfo info)
        {
            Color4 diffuse = Color4Helper.Zero;
            if (info.hasDiffuse) {
                diffuse = Color4Helper.fromVector4(info.vDiffuse);
            }
            Color4 ambient = Color4Helper.Zero;
            if (info.hasAmbient) {
                ambient = Color4Helper.fromVector4(info.vAmbient);
            }
            Color4 specular = Color4Helper.Zero;
            if (info.hasSpecular) {
                specular = Color4Helper.fromVector4(info.vSpecular);
            }
            return new SSColorMaterial (diffuse, ambient, specular,
                Color4Helper.Zero);
        }

        public SSColorMaterial (
            Color4 diffuse, Color4 ambient, Color4 specular, Color4 emission, 
            float shininess = 10f)
        {
            ambientColor = ambient;
            diffuseColor = diffuse;
            specularColor = specular;
            emissionColor = emission;
            this.shininess = shininess;
        }

        public SSColorMaterial(Color4 diffuse)
        {
            diffuseColor = diffuse;
            ambientColor = Color4Helper.Zero;
            specularColor = Color4Helper.Zero;
            emissionColor = Color4Helper.Zero;
            shininess = 0f;
        }

        public SSColorMaterial()
        { }

        public static void applyColorMaterial(SSColorMaterial colorMat)
        {
            colorMat = colorMat ?? defaultColorMat;
            GL.Material(MaterialFace.Front, MaterialParameter.Ambient, colorMat.ambientColor);
            GL.Material(MaterialFace.Front, MaterialParameter.Diffuse, colorMat.diffuseColor);
            GL.Material(MaterialFace.Front, MaterialParameter.Specular, colorMat.specularColor);
            GL.Material(MaterialFace.Front, MaterialParameter.Emission, colorMat.emissionColor);
            GL.Material(MaterialFace.Front, MaterialParameter.Shininess, colorMat.shininess);
        }
    }
}

