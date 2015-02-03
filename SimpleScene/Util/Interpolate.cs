using System.Collections.Generic;

namespace SimpleScene.Util
{
    class Interpolate {
        static public float Lerp(float start, float finish, float ammount)
        {
            // TODO: template this?
            return start + (finish - start) * ammount;
        }
    }
}

