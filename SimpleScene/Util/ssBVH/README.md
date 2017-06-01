## Dynamic 3d BVH - 3d Bounding Volume Hierarchy

Copyright(C) 2014, by David W. Jeske
Released under Apache 2.0 and to the public domain

### About

This is a 3d Bounding Volume Hiearchy implementation in C#. It is used for sorting objects that occupy 
volume and answer geometric queries about them, such as ray, box, and sphere intersection. 

It includes an efficient algorithm for incrementally re-optimizing the BVH when contained objects move. 

For more information about using this code, see the CodeProject article [Dynamic Bounding Volume Hierarchy in C#](https://www.codeproject.com/Articles/832957/Dynamic-Bounding-Volume-Hiearchy-in-Csharp)

### References

* [Brief BVH Tutorial](http://www.3dmuve.com/3dmblog/?p=182)
* ["Fast, Effective BVH Updates for Animated Scenes" (Kopta, Ize, Spjut, Brunvand, David, Kensler)](https://github.com/jeske/SimpleScene/blob/master/SimpleScene/Util/ssBVH/docs/BVH_fast_effective_updates_for_animated_scenes.pdf)
* [Space Partitioning: Octree vs. BVH](http://thomasdiewald.com/blog/?p=1488)
