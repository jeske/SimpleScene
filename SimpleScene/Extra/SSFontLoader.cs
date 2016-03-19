using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using MatterHackers.Agg.Font;

namespace SimpleScene
{
    public static class SSFontLoader
    {
        public static TypeFace loadTypeFace(string path)
        {
            var reader = SSAssetManager.OpenStreamReader(path);
            TypeFace newTypeFace = new TypeFace ();
            newTypeFace.ReadSVG(reader.ReadToEnd());
            return newTypeFace;
        }
        #if false
        public static FontFamily[] loadFontFamilies(SSAssetManager.Context ctx, string filename)
        {
        var stream = ctx.Open(filename);
        var binReader = new BinaryReader (stream);
        byte[] bytes = binReader.ReadBytes(int.MaxValue);
        var fontCollection = new PrivateFontCollection ();
        unsafe {
        fixed (byte* ptr = bytes) {
        fontCollection.AddMemoryFont(new IntPtr (ptr), bytes.Length);
        }
        }
        return fontCollection.Families;
        }
        #endif
    }
}

