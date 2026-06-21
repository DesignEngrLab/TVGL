#region SharpDxf, Copyright(C) 2012 Lomatus, Licensed under LGPL.

//                        SharpDxf library( Base on netDxf by Daniel Carvajal )
// Copyright (C) 2012 Lomatus (tourszhou@gmail.com)
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 

#endregion

using System;
using System.Collections.Generic;
using SharpDxf.Tables;
using TVGL;

namespace SharpDxf.Entities
{
    /// <summary>
    /// Represents a dxf Vertex.
    /// </summary>
    /// <remarks>
    /// The Vertex class holds all the information read from the dxf file even if its needed or not.
    /// For internal use only.
    /// </remarks>
    internal class Vertex :
        DxfObject 
        
    {
      
        #region private fields

        private VertexTypeFlags flags;
        private Vector3 location;
        private int[] vertexIndexes;
        private double beginThickness;
        private double endThickness;
        private double bulge;
        private AciColor color;
        private Layer layer;
        private LineType lineType;
        private Dictionary<ApplicationRegistry, XData> xData;

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <c>Vertex</c> class.
        /// </summary>
        internal Vertex()
            : base(DxfObjectCode.Vertex)
        {
            this.flags = VertexTypeFlags.PolylineVertex;
            this.location = Vector3.Zero;
            this.layer = Layer.Default;
            this.color = AciColor.ByLayer;
            this.lineType = LineType.ByLayer;
            this.bulge = 0.0f;
            this.beginThickness = 0.0f;
            this.endThickness = 0.0f;
        }

        /// <summary>
        /// Initializes a new instance of the <c>Vertex</c> class.
        /// </summary>
        /// <param name="location">Vertex <see cref="SharpDxf.Vector3">location</see>.</param>
        internal Vertex(Vector3 location)
            : base(DxfObjectCode.Vertex)
        {
            this.flags = VertexTypeFlags.PolylineVertex;
            this.location = location;
            this.layer = Layer.Default;
            this.color = AciColor.ByLayer;
            this.lineType = LineType.ByLayer;
            this.bulge = 0.0f;
            this.beginThickness = 0.0f;
            this.endThickness = 0.0f;
        }

        /// <summary>
        /// Initializes a new instance of the <c>Vertex</c> class.
        /// </summary>
        /// <param name="location">Vertex <see cref="SharpDxf.Vector2">location</see>.</param>
        internal Vertex(Vector2 location)
            : base(DxfObjectCode.Vertex)
        {
            this.flags = VertexTypeFlags.PolylineVertex;
            this.location = new Vector3(location.X, location.Y, 0.0f);
            this.layer = Layer.Default;
            this.color = AciColor.ByLayer;
            this.lineType = LineType.ByLayer;
            this.bulge = 0.0f;
            this.beginThickness = 0.0f;
            this.endThickness = 0.0f;
        }

        /// <summary>
        /// Initializes a new instance of the <c>Vertex</c> class.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="z">Z coordinate.</param>
        internal Vertex(double x, double y, double z)
            : base(DxfObjectCode.Vertex)
        {
            this.flags = VertexTypeFlags.PolylineVertex;
            this.location = new Vector3(x, y, z);
            this.layer = Layer.Default;
            this.color = AciColor.ByLayer;
            this.lineType = LineType.ByLayer;
            this.bulge = 0.0f;
            this.beginThickness = 0.0f;
            this.endThickness = 0.0f;
        }

        /// <summary>
        /// Initializes a new instance of the <c>Vertex</c> class.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        internal Vertex(double x, double y)
            : base(DxfObjectCode.Vertex)
        {
            this.flags = VertexTypeFlags.PolylineVertex;
            this.location = new Vector3(x, y, 0.0f);
            this.layer = Layer.Default;
            this.color = AciColor.ByLayer;
            this.lineType = LineType.ByLayer;
            this.bulge = 0.0f;
            this.beginThickness = 0.0f;
            this.endThickness = 0.0f;
        }

        #endregion

        #region internal properties
        
        /// <summary>
        /// Gets or sets the polyline vertex <see cref="SharpDxf.Vector3">location</see>.
        /// </summary>
        internal Vector3 Location
        {
            get { return this.location; }
            set { this.location = value; }
        }

        internal int[] VertexIndexes
        {
            get { return this.vertexIndexes; }
            set { this.vertexIndexes = value; }
        }

        /// <summary>
        /// Gets or sets the light weight polyline begin thickness.
        /// </summary>
        internal double BeginThickness
        {
            get { return this.beginThickness; }
            set { this.beginThickness = value; }
        }

        /// <summary>
        /// Gets or sets the light weight polyline end thickness.
        /// </summary>
        internal double EndThickness
        {
            get { return this.endThickness; }
            set { this.endThickness = value; }
        }

        /// <summary>
        /// Gets or set the light weight polyline bulge.Accepted values range from 0 to 1.
        /// </summary>
        /// <remarks>
        /// The bulge is the tangent of one fourth the included angle for an arc segment, 
        /// made negative if the arc goes clockwise from the start point to the endpoint. 
        /// A bulge of 0 indicates a straight segment, and a bulge of 1 is a semicircle.
        /// </remarks>
        internal double Bulge
        {
            get { return this.bulge; }
            set
            {
                if (this.bulge < 0.0 || this.bulge > 1.0f)
                {
                    throw new ArgumentOutOfRangeException("value", value, "The bulge must be a value between zero and one");
                }
                this.bulge = value;
            }
        }

        /// <summary>
        /// Gets or sets the vertyex type.
        /// </summary>
        internal VertexTypeFlags Flags
        {
            get { return this.flags; }
            set { this.flags = value;}
        }

       /// <summary>
        /// Gets or sets the entity color.
        /// </summary>
        internal AciColor Color
        {
            get { return this.color; }
            set { this.color = value; }
        }

        /// <summary>
        /// Gets or sets the entity layer.
        /// </summary>
        internal Layer Layer
        {
            get { return this.layer; }
            set { this.layer = value; }
        }

        /// <summary>
        /// Gets or sets the entity line type.
        /// </summary>
        internal LineType LineType
        {
            get { return this.lineType; }
            set { this.lineType = value; }
        }

        /// <summary>
        /// Gets or sets the entity extended data.
        /// </summary>
        internal Dictionary<ApplicationRegistry, XData> XData
        {
            get { return this.xData; }
            set { this.xData = value; }
        }

        #endregion

        #region overrides

        public override string ToString()
        {
            return this.CodeName;
        }

        #endregion
    }
}