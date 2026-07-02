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

using TVGL;

namespace SharpDxf.Entities
{
    /// <summary>
    /// Represents a nurbs curve vertex.
    /// </summary>
    internal class NurbsVertex
    {
        #region private fields

        private Vector2 location;
        private double weight;

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <c>NurbsVertex</c> class.
        /// </summary>
        internal NurbsVertex()
        {
            this.location = Vector2.Zero;
            this.weight = 1;
        }

        /// <summary>
        /// Initializes a new instance of the <c>NurbsVertex</c> class.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        internal NurbsVertex(double x, double y)
        {
            this.location = new Vector2(x, y);
            this.weight = 1;
        }

        /// <summary>
        /// Initializes a new instance of the <c>NurbsVertex</c> class.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="weight">Nurbs vertex weight.</param>
        internal NurbsVertex(double x, double y, double weight)
        {
            this.location = new Vector2(x, y);
            this.weight = weight;
        }

        /// <summary>
        /// Initializes a new instance of the <c>NurbsVertex</c> class.
        /// </summary>
        /// <param name="location">Nurbs vertex <see cref="Vector2">location</see>.
        internal NurbsVertex(Vector2 location)
        {
            this.location = location;
            this.weight = 1;
        }

        /// <summary>
        /// Initializes a new instance of the <c>NurbsVertex</c> class.
        /// </summary>
        /// <param name="location">Nurbs vertex <see cref="Vector2">location</see>.
        /// <param name="weight">Nurbs vertex weight.</param>
        internal NurbsVertex(Vector2 location, double weight)
        {
            this.location = location;
            this.weight = weight;
        }

        #endregion

        #region internal properties

        /// <summary>
        /// Gets or sets the vertex <see cref="SharpDxf.Vector2">location</see>.
        /// </summary>
        internal Vector2 Location
        {
            get { return this.location; }
            set { this.location = value; }
        }

        /// <summary>
        /// Gets or sets the vertex weight.
        /// </summary>
        internal double Weight
        {
            get { return this.weight; }
            set { this.weight = value; }
        }

        #endregion
    }
}