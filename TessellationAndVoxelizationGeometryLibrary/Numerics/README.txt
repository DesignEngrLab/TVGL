The files in this directory are copied from the 
https://github.com/dotnet/runtime/commit/1a1a541954db674980b09efe426d3b6dfd6ae3c9
which is a base library in .Net now. This is known as System.Numerics
However, their approach is to go with floats (single precision) in all vector classes.
The thinking is that floats can be twice as fast to compute on a 64-bit computer if 
you SIMD your computations (Single instruction, multiple data; this is because floats are 32-bits wide).
But, single precision is only accurate for 7 sig-digs 
(https://docs.microsoft.com/en-us/dotnet/api/system.single)
while double is 15. Seven seems like plenty but we have seen on several instances where even
double precision causes problems (convex hull, polygonal operations, etc.). Plus, since
everything else is already doubles like area and bounds, and applications currently using
TVGL use doubles, it seems problematic to go with this throughout TVGL. 

However, the concept to use Vector2 and Vector3 structs everywhere versus double arrays (which
are reference types) is likely to be a speed and memory improvement.

So, what we've done here is simply copy System.Numerics files and change all floats to doubles.
The files are changed as little as possible so that changes to System.Numerics can be reflected here.


A. error in TransformNormals - need to take the transpose of the inverse of the matrix
B. change Matrix3x2 to Matrix3x3 and add a boolean to both Matrix3x3 and Matrix4x4 which is "IsProjectiveTransform"
	why?
		1. matrix3x3 should be useful in other situations where solving a small system of equations like Ax = b.
		  one such example - still in computational geometry is finding the common point to 3 infinite places
		2. the projective terms could be used in 2D geometry - imagane a situation in which a flat 2D picture (line art or polygons)
		  but the 'camera' needs to be changed like in tilting the camera in a map
		3. no apologies need for internal functions like invert and determinant which are based on the 3 by 3 matrix
		4. adding IsProjectiveTransform to Matrix4x4 greatly simplifies many situations where only affine transformations occur
		   this makes many of those functions simpler and quicker when we can check this boolean prior to a transform, invert, etc.
C. simplify some constructors. why do they not call one another? does this introduce time?

D. make these Matrix structs readonly

E. Matrix4x4 sometimes acts like its transpose
