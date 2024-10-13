//namespace PointCloud;
global using System.Runtime.CompilerServices;
global using System.Collections;
global using System.Collections.Generic;
#if CUSTOMVECTOR
// enter your custom vector types here 
global using Vector2 = PointCloud.Numerics.Vector2;
global using Vector3 = PointCloud.Numerics.Vector3;
global using Vector4 = PointCloud.Numerics.Vector4;
#else
global using Vector2 = System.Numerics.Vector2;
global using Vector3 = System.Numerics.Vector3;
global using Vector4 = System.Numerics.Vector4;
#endif
