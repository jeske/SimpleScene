## Dynamic 3d BVH - 3d Bounding Volume Hierarchy

Copyright(C) 2014, by David W. Jeske. Released under Apache 2.0 and to the public domain.

### About

This is a 3d Bounding Volume Hiearchy implementation in C#. It is used for sorting objects that occupy 
volume and answer geometric queries about them, such as ray, box, and sphere intersection. 

It includes an efficient algorithm for incrementally re-optimizing the BVH when contained objects move. 

For more information about what a BVH is, and about how to use this code, see the CodeProject article:

* [Dynamic Bounding Volume Hierarchy in C#](https://www.codeproject.com/Articles/832957/Dynamic-Bounding-Volume-Hiearchy-in-Csharp)

<table>
<tr>
<td>ssBVH.cs</td>
<td> The root interface to the BVH </td></tr>
<tr>
<td>ssBVH_Node.cs</td>
<td> The code for managing, traversing, and optimizing the BVH </td></tr>
<tr>
<td>ssBVH_SSObject.cs</td>
<td> A example SSBVHNodeAdaptor intergration of the BVH with the SimpleScene 3d scene manager, and a SSBVHRender SimpleScene rendering object to render the BVH boundaries in OpenGL. </td></tr>
<tr>
<td>ssBVH_Sphere.cs</td>
<td> An example SSBVHNodeAdaptor for placing spheres in the BVH.</td></tr>
</table>

### References

* ["Fast, Effective BVH Updates for Animated Scenes" (Kopta, Ize, Spjut, Brunvand, David, Kensler)](https://github.com/jeske/SimpleScene/blob/master/SimpleScene/Util/ssBVH/docs/BVH_fast_effective_updates_for_animated_scenes.pdf)
* [Space Partitioning: Octree vs. BVH](http://thomasdiewald.com/blog/?p=1488)

### Pictures

<img src="https://www.codeproject.com/KB/openGL/832957/Screen_Shot_2014-11-15_at_9.42.26_AM.png">
