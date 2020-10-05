// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;

namespace TVGL
{
    internal class SortByIndexInList : IComparer<TessellationBaseClass>
    {
        public int Compare(TessellationBaseClass x, TessellationBaseClass y)
        {
            if (x.Equals(y)) return 0;
            if (x.IndexInList < y.IndexInList) return -1;
            else return 1;
        }
    }

    internal class ReverseSort : IComparer<double>
    {
        public int Compare(double x, double y)
        {
            if (x == y) return 0;
            if (x < y) return 1;
            return -1;
        }
    }

    internal class ForwardSort : IComparer<double>
    {
        public int Compare(double x, double y)
        {
            if (x == y) return 0;
            if (x < y) return -1;
            return 1;
        }
    }

    internal class AbsoluteValueSort : IComparer<double>
    {
        public int Compare(double x, double y)
        {
            if (Math.Abs(x) == Math.Abs(y)) return 0;
            if (Math.Abs(x) < Math.Abs(y)) return -1;
            return 1;
        }
    }


}