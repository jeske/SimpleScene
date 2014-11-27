SimpleScene
-----------

Simple 3d OpenGL/OpenTK Scene manager in C#. 

(C) Copyright 2014 by David W. Jeske

Released to the Public Domain AND under the Apache 2.0 license.

SimpleScene contains decent:

- wavefront OBJ loading
- BVH space partitioning

..and several half-working unfinished elements, including:

- 2d HUD component system
- per-pixel GLSL shaders, using GL2 / GLSL120 for maximum hardware compatibility
- shadow mapping
 
WavefrontOBJ loader contains a basic skelleton of using the OpenTK 3d library wrapper for OpenGL, 
loading WavefrontOBJ files, textures, and displaying them in a simple scene. 

