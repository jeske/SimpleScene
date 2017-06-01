### SimpleScene 

A Simple 3d OpenGL/OpenTK Scene manager in C#, which runs on Windows, Mac, and Linux.

(C) Copyright 2015-2017 by David W. Jeske, Sergey Butylkov

Released under the Apache 2.0 license.

### Motivation

When I started to learn 3D, [Axiom](http://axiomengine.sourceforge.net/wiki/index.php/Main_Page) was just getting 
started, and as a port of ORGE, it was/is fairly complex to build and understand. Unity didn't exist. Instead I 
started with a simpler approachable library called Brume3D, and I learned quite a bit from it. However, it had 
two main problems. It was released under the LGPL, and it only ran on Windows (D3D). I found both of these points 
objectionable, so when I had learned enough, I created my own simple 3d library with OpenGL (OpenTK).

### Features

* 3d scene rendering with OpenTK, GL2.2 GLSL 120 for maximum compatibility
* asset loading
  * wavefront OBJ
  * MD5MESH (with animations)
* instanced rendering
* shadow mapping
* BVH space partitioning (with efficient dynamic updates)
* A rudamentary 2D HUD framework based on Windows GDI


### Documantation

* For more information see <a href="https://github.com/jeske/SimpleScene/wiki">the SimpleScene Wiki</a>.

[[images/testbench0_screenshot1.jpg]]
