using System;
using System.Collections.Generic;
using System.Text;

namespace TVGL.Curves
{
    public class Ellipse : ConicSection
    { }
    public class Parabola : ConicSection
    { }
    public class Hyperbola : ConicSection
    { }
    public abstract  class ConicSection
    {
        Plane plane;
    }
}
