using System;
using System.Drawing;

namespace SimpleScene
{
    public class SSTextureWithAlpha : SSTexture
    {
        public SSTextureWithAlpha(SSAssetManager.Context ctx, string filename)
            : base() {
            Bitmap textureBitmap = new Bitmap(ctx.Open(filename));
            loadFromBitmap(textureBitmap, name: filename, hasAlpha: true);
        }
    }
}

