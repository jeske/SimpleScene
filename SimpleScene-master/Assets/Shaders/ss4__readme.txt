// Copyright(C) David W. Jeske, 2014, All Rights Reserved.

// "SS4" shaders implement GLSL single-pass wireframes as described here...
// http://strattonbrazil.blogspot.com/2011/09/single-pass-wireframe-rendering_10.html

// another method of single-pass wireframes is this GLSL wireframe geometry shader
// which outputs additional GL-Line geometry for every triangle. 
// We don't use this method because GLSL120 is not allowed to output additional
// primitives, and because it still suffers from z-fighting. 
// http://www.lighthouse3d.com/tutorials/glsl-core-tutorial/geometry-shader/

// https://wiki.engr.illinois.edu/display/graphics/Geometry+Shader+Hello+World
// GLSL fragment shader and bump mapping tutorial
// http://fabiensanglard.net/bumpMapping/index.php
