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
    /// Defines which edges are hidden.
    /// </summary>
    [Flags]
    internal enum EdgeFlags
    {
        /// <summary>
        /// All edges as visibles (default).
        /// </summary>
        Visibles = 0,
        /// <summary>
        /// First edge is invisible.
        /// </summary>
        First = 1,
        /// <summary>
        /// Second edge is invisible.
        /// </summary>
        Second = 2,
        /// <summary>
        /// Third edge is invisible.
        /// </summary>
        Third = 4,
        /// <summary>
        /// Fourth edge is invisible.
        /// </summary>
        Fourth = 8
    }

    /// <summary>
    /// Represents a 3DFace <see cref="SharpDxf.Entities.IEntityObject">entity</see>.
    /// </summary>
    internal class Face3d :
        DxfObject
    {
        #region private fields

        private const EntityType TYPE = EntityType.Face3D;
        private Vector3 firstVertex;
        private Vector3 secondVertex;
        private Vector3 thirdVertex;
        private Vector3 fourthVertex;
        private EdgeFlags edgeFlags;
        private Layer layer;
        private AciColor color;
        private LineType lineType;
        private Dictionary<ApplicationRegistry, XData> xData;

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <c>Face3D</c> class.
        /// </summary>
        /// <param name="firstVertex">3d face <see cref="Vector3">first vertex</see>.</param>
        /// <param name="secondVertex">3d face <see cref="Vector3">second vertex</see>.</param>
        /// <param name="thirdVertex">3d face <see cref="Vector3">third vertex</see>.</param>
        /// <param name="fourthVertex">3d face <see cref="Vector3">fourth vertex</see>.</param>
        internal Face3d(Vector3 firstVertex, Vector3 secondVertex, Vector3 thirdVertex, Vector3 fourthVertex)
            : base(DxfObjectCode.Face3D)
        {
            this.firstVertex = firstVertex;
            this.secondVertex = secondVertex;
            this.thirdVertex = thirdVertex;
            this.fourthVertex = fourthVertex;
            this.edgeFlags = EdgeFlags.Visibles;
            this.layer = Layer.Default;
            this.color = AciColor.ByLayer;
            this.lineType = LineType.ByLayer;
            this.xData = new Dictionary<ApplicationRegistry, XData>();
        }

        /// <summary>
        /// Initializes a new instance of the <c>Face3D</c> class.
        /// </summary>
        internal Face3d()
            : base(DxfObjectCode.Face3D)
        {
            this.firstVertex = Vector3.Zero;
            this.secondVertex = Vector3.Zero;
            this.thirdVertex = Vector3.Zero;
            this.fourthVertex = Vector3.Zero;
            this.edgeFlags = EdgeFlags.Visibles;
            this.layer = Layer.Default;
            this.color = AciColor.ByLayer;
            this.lineType = LineType.ByLayer;
            this.xData = new Dictionary<ApplicationRegistry, XData>();
        }

        #endregion

        #region internal properties

        /// <summary>
        /// Gets or sets the first 3d face <see cref="SharpDxf.Vector3">vertex</see>.
        /// </summary>
        internal Vector3 FirstVertex
        {
            get { return this.firstVertex; }
            set { this.firstVertex = value; }
        }

        /// <summary>
        /// Gets or sets the second 3d face <see cref="SharpDxf.Vector3">vertex</see>.
        /// </summary>
        internal Vector3 SecondVertex
        {
            get { return this.secondVertex; }
            set { this.secondVertex = value; }
        }

        /// <summary>
        /// Gets or sets the third 3d face <see cref="SharpDxf.Vector3">vertex</see>.
        /// </summary>
        internal Vector3 ThirdVertex
        {
            get { return this.thirdVertex; }
            set { this.thirdVertex = value; }
        }

        /// <summary>
        /// Gets or sets the fourth 3d face <see cref="SharpDxf.Vector3">vertex</see>.
        /// </summary>
        internal Vector3 FourthVertex
        {
            get { return this.fourthVertex; }
            set { this.fourthVertex = value; }
        }

        /// <summary>
        /// Gets or set the 3d face edge visibility.
        /// </summary>
        internal EdgeFlags EdgeFlags
        {
            get { return this.edgeFlags; }
            set { this.edgeFlags = value; }
        }

        #endregion

        #region IEntityObject Members

        /// <summary>
        /// Gets the entity <see cref="SharpDxf.Entities.EntityType">type</see>.
        /// </summary>
        internal EntityType Type
        {
            get { return TYPE; }
        }

        /// <summary>
        /// Gets or sets the entity <see cref="SharpDxf.AciColor">color</see>.
        /// </summary>
        internal AciColor Color
        {
            get { return this.color; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                this.color = value;
            }
        }

        /// <summary>
        /// Gets or sets the entity <see cref="SharpDxf.Tables.Layer">layer</see>.
        /// </summary>
        internal Layer Layer
        {
            get { return this.layer; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                this.layer = value;
            }
        }

        /// <summary>
        /// Gets or sets the entity <see cref="SharpDxf.Tables.LineType">line type</see>.
        /// </summary>
        internal LineType LineType
        {
            get { return this.lineType; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                this.lineType = value;
            }
        }

        /// <summary>
        /// Gets or sets the entity <see cref="SharpDxf.XData">extende data</see>.
        /// </summary>
        internal Dictionary<ApplicationRegistry, XData> XData
        {
            get { return this.xData; }
            set { this.xData = value; }
        }

        #endregion

        #region overrides

        /// <summary>
        /// Converts the value of this instance to its equivalent string representation.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return TYPE.ToString();
        }

        #endregion
    }
}