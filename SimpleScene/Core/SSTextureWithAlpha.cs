using System;
using System.IO;
using System.Drawing;

namespace SimpleScene
{
    public class SSTextureWithAlpha : SSTexture
    {
        public SSTextureWithAlpha(string path)
            : base() {
            Bitmap textureBitmap = new Bitmap(SSAssetManager.OpenStream(path));
            string name = Path.GetFileName(path);
            loadFromBitmap(textureBitmap, name, hasAlpha: true);
        }
    }
}

