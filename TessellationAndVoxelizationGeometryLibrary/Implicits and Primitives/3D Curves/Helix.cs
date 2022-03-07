using System;
using System.Collections.Generic;
using TVGL.Numerics;

namespace TVGL.Primitives
{
    public readonly struct Helix 
    {

        public readonly double Radius;

        public readonly double Pitch;

        public readonly Vector3 Anchor;

        public readonly Vector3 Direction;

        public Helix(Vector3 anchor, Vector3 direction, double radius, double pitch) 
        {
            Anchor = anchor;
            Direction = direction;
            Radius = radius;
            Pitch = pitch;
        }

    }
}
