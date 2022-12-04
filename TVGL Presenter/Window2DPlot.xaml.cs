// ***********************************************************************
// Assembly         : TVGL Presenter
// Author           : Matt
// Created          : 05-20-2016
//
// Last Modified By : Matt
// Last Modified On : 05-24-2016
// ***********************************************************************
// <copyright file="Window3DPlot.xaml.cs" company="OxyPlot">
//     The MIT License (MIT)
/*
  Copyright(c) 2014 OxyPlot contributors


  Permission is hereby granted, free of charge, to any person obtaining a
  copy of this software and associated documentation files (the
  "Software"), to deal in the Software without restriction, including
  without limitation the rights to use, copy, modify, merge, publish,
  distribute, sublicense, and/or sell copies of the Software, and to
  permit persons to whom the Software is furnished to do so, subject to
  the following conditions:
  
  The above copyright notice and this permission notice shall be included
  in all copies or substantial portions of the Software.
  
  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
  OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
  MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
  IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
  CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
  TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
  SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
// </copyright>
// <summary></summary>
// ***********************************************************************

using OxyPlot;
using OxyPlot.Series;
using System.Collections.Generic;
using System.Linq;
using System.Windows;



namespace TVGL
{
    /// <summary>
    ///     Enum Plot2DType
    /// </summary>
    public enum Plot2DType
    {
        /// <summary>
        ///     The line
        /// </summary>
        Line,

        /// <summary>
        ///     The points
        /// </summary>
        Points
    }

    /// <summary>
    ///     Class Window2DPlot.
    /// </summary>
    public partial class Window2DPlot : Window
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Window2DPlot" /> class.
        /// </summary>
        /// <param name="title">The title.</param>
        private Window2DPlot(string title)
        {
            Title = title;
            Model = new PlotModel();
        }

        public Window2DPlot(IEnumerable<Vector2> points, string title, Plot2DType plot2DType, bool closeShape,
         MarkerType marker) : this(title)
        {
            if (plot2DType == Plot2DType.Line)
                AddLineSeriesToModel(points.ToList(), closeShape, marker);
            else
                AddScatterSeriesToModel(points.ToList(), marker);
            SetAxes(points);
            InitializeComponent();
            Title = title;
        }


        /// <summary>
        ///     Initializes a new instance of the <see cref="Window2DPlot" /> class.
        /// </summary>
        /// <param name="listOfArrayOfPoints">The list of array of points.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public Window2DPlot(IEnumerable<IEnumerable<Vector2>> listOfArrayOfPoints, string title, Plot2DType plot2DType, bool closeShape,
            MarkerType marker) : this(title)
        {
            var allPoints = new List<Vector2>();
            foreach (var points in listOfArrayOfPoints)
            {
                if (points == null || !points.Any()) continue;
                allPoints.AddRange(points);
                if (plot2DType == Plot2DType.Line)
                    AddLineSeriesToModel(points, closeShape, marker);
                else
                    AddScatterSeriesToModel(points, marker);
            }
            SetAxes(allPoints);
            InitializeComponent();
            Title = title;
        }



        /// <summary>
        ///     Initializes a new instance of the <see cref="Window2DPlot" /> class.
        ///     This version allows different markers to be set for each set of polygons.
        /// </summary>
        /// <param name="listOfListOfPoints2"></param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="listOfListOfPoints1"></param>
        /// <param name="marker1"></param>
        /// <param name="marker2"></param>
        public Window2DPlot(IEnumerable<IEnumerable<Vector2>> listOfListOfPoints1,
            IEnumerable<IEnumerable<Vector2>> listOfListOfPoints2,
            string title, Plot2DType plot2DType, bool closeShape,
            MarkerType marker1, MarkerType marker2) : this(title)
        {
            foreach (var points in listOfListOfPoints1)
            {
                if (plot2DType == Plot2DType.Line)
                    AddLineSeriesToModel(points, closeShape, marker1);
                else
                    AddScatterSeriesToModel(points, marker1);
            }
            foreach (var points in listOfListOfPoints2)
            {
                if (plot2DType == Plot2DType.Line)
                    AddLineSeriesToModel(points, closeShape, marker2);
                else
                    AddScatterSeriesToModel(points, marker2);
            }
            var allpoints = listOfListOfPoints1.SelectMany(v => v).ToList();
            allpoints.AddRange(listOfListOfPoints2.SelectMany(v => v));
            SetAxes(allpoints);
            InitializeComponent();
            Title = title;
        }

        public Window2DPlot(IEnumerable<IEnumerable<IEnumerable<Vector2>>> listofListOfListOfPoints,
            string title, Plot2DType plot2DType, bool closeShape, MarkerType marker) : this(title)
        {
            var i = 0;

            var colorPalet = Color.GetRandomColors().GetEnumerator();
            foreach (var listOfListOfPoints in listofListOfListOfPoints)
            {
                if (plot2DType == Plot2DType.Line)
                {
                    //Set each list of points as its own color.
                    //Close each list of points 
                    foreach (var points in listOfListOfPoints)
                    {
                        colorPalet.MoveNext();
                        var color = colorPalet.Current;
                        var series = new LineSeries
                        {
                            MarkerType = marker,
                            Color = OxyColor.FromRgb(color.R, color.G, color.B)
                        };
                        foreach (var point in points)
                        {
                            series.Points.Add(new DataPoint(point.X, point.Y));
                        }
                        if (closeShape) series.Points.Add(new DataPoint(points.First().X, points.First().Y));
                        Model.Series.Add(series);
                    }
                }
                else
                {
                    foreach (var points in listOfListOfPoints)
                    {
                        AddScatterSeriesToModel(points, marker);
                    }
                }
            }
            SetAxes(listofListOfListOfPoints.SelectMany(poly => poly.SelectMany(v => v)));
            InitializeComponent();
            Title = title;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Window2DPlot" /> class.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public Window2DPlot(IList<double[]> points, string title, Plot2DType plot2DType, bool closeShape, MarkerType marker)
            : this(title)
        {
            if (plot2DType == Plot2DType.Line)
                AddLineSeriesToModel(points, closeShape, marker);
            else
                AddScatterSeriesToModel(points, marker);
            SetAxes(points.Select(v => new Vector2(v[0], v[1])));
            InitializeComponent();
            Title = title;
        }

        /// <summary>
        ///     Gets or sets the model.
        /// </summary>
        /// <value>The model.</value>
        public PlotModel Model { get; set; }

        /// <summary>
        ///     Adds the line series to model.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        private void AddLineSeriesToModel(IEnumerable<Vector2> points, bool closeShape, MarkerType marker, TVGL.Color color = null)
        {
            AddLineSeriesToModel(PointsToDouble(points), closeShape, marker, color);
        }

        /// <summary>
        ///     Adds the line series to model.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        private void AddLineSeriesToModel(IList<double[]> points, bool closeShape, MarkerType marker, TVGL.Color color = null)
        {
            if (!points.Any()) return;
            var series = new LineSeries { MarkerType = marker };

            //Add color to series if applicable
            if (color != null)
            {
                series.Color = OxyColor.FromRgb(color.R, color.G, color.B);
            }

            foreach (var point in points)
                //point[0] == x, point[1] == y
                series.Points.Add(new DataPoint(point[0], point[1]));
            if (closeShape) series.Points.Add(new DataPoint(points[0][0], points[0][1]));
            Model.Series.Add(series);
        }

        /// <summary>
        ///     Adds the scatter series to model.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="marker">The marker.</param>
        private void AddScatterSeriesToModel(IEnumerable<Vector2> points, MarkerType marker, TVGL.Color color = null)
        {
            AddScatterSeriesToModel(PointsToDouble(points), marker, color);
        }

        /// <summary>
        ///     Adds the scatter series to model.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="marker">The marker.</param>
        private void AddScatterSeriesToModel(IList<double[]> points, MarkerType marker, TVGL.Color color = null)
        {
            var series = new LineSeries
            {
                MarkerType = marker,
                LineStyle = LineStyle.None
            };
            //Add color to series if applicable
            if (color != null)
            {
                series.Color = OxyColor.FromRgb(color.R, color.G, color.B);
            }

            foreach (var point in points)
                //point[0] == x, point[1] == y
                series.Points.Add(new DataPoint(point[0], point[1]));
            Model.Series.Add(series);
        }


        private void SetAxes(IEnumerable<Vector2> polygons)
        {
            if (!polygons.Any()) return;
            var minX = polygons.Min(p => p.X);
            var maxX = polygons.Max(p => p.X);
            var minY = polygons.Min(p => p.Y);
            var maxY = polygons.Max(p => p.Y);
            if (maxX - minX > maxY - minY)
            {
                var center = (minY + maxY) / 2;
                var halfDim = (maxX - minX) / 2;
                minY = center - halfDim;
                maxY = center + halfDim;
            }
            else
            {
                var center = (minX + maxX) / 2;
                var halfDim = (maxY - minY) / 2;
                minX = center - halfDim;
                maxX = center + halfDim;
            }
            var buffer = 0.1 * (maxX - minX);
            minX -= buffer;
            maxX += buffer;
            minY -= buffer;
            maxY += buffer;

            //Model.Axes.Add(new LinearAxis());
            //Model.Axes.Add(new LinearAxis());
            //Model.Axes[0].Minimum = minX;
            //Model.Axes[0].Maximum = maxX;
            //Model.Axes[1].Minimum = minY;
            //Model.Axes[1].Maximum = maxY;
        }


        private List<double[]> PointsToDouble(IEnumerable<Vector2> points)
        {
            return points.Select(p => new[] { p.X, p.Y }).ToList();
        }


    }
}