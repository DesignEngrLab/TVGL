
using System;

namespace TVGL.Numerics
{
    /// <summary>
    /// Class IntrinsicAttribute.
    /// Implements the <see cref="System.Attribute" />
    /// </summary>
    /// <seealso cref="System.Attribute" />
    internal class IntrinsicAttribute : Attribute
    {
        // someday need to figure out how to connect this with single-instruction multiple dispatch
        // or SIMD
        // there are different approaches depending on the hardware. 
        // Intel SSE
        // ARM ADV
        // also what's RyuJIT?
    }
}