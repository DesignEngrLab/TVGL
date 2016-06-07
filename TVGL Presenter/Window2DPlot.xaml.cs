// ***********************************************************************
// Assembly         : TVGL Presenter
// Author           : Matt
// Created          : 05-20-2016
//
// Last Modified By : Matt
// Last Modified On : 05-24-2016
// ***********************************************************************
// <copyright file="MainWindow.xaml.cs" company="OxyPlot">
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

using System.Collections.Generic;
using System.Windows;
using OxyPlot;
using OxyPlot.Series;

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

        /// <summary>
        ///     Initializes a new instance of the <see cref="Window2DPlot" /> class.
        /// </summary>
        /// <param name="listOfArrayOfPoints">The list of array of points.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public Window2DPlot(IList<Point[]> listOfArrayOfPoints, string title, Plot2DType plot2DType, bool closeShape,
            MarkerType marker) : this(title)
        {
            foreach (var points in listOfArrayOfPoints)
            {
                if (plot2DType == Plot2DType.Line)
                    addLineSeriesToModel(points, closeShape, marker);
                else
                    addScatterSeriesToModel(points, marker);
            }
            InitializeComponent();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Window2DPlot" /> class.
        /// </summary>
        /// <param name="listOfListOfPoints">The list of list of points.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public Window2DPlot(IList<List<Point>> listOfListOfPoints, string title, Plot2DType plot2DType, bool closeShape,
            MarkerType marker) : this(title)
        {
            foreach (var points in listOfListOfPoints)
            {
                if (plot2DType == Plot2DType.Line)
                    addLineSeriesToModel(points, closeShape, marker);
                else
                    addScatterSeriesToModel(points, marker);
            }
            InitializeComponent();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Window2DPlot" /> class.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public Window2DPlot(IList<Point> points, string title, Plot2DType plot2DType, bool closeShape, MarkerType marker)
            : this(title)
        {
            if (plot2DType == Plot2DType.Line)
                addLineSeriesToModel(points, closeShape, marker);
            else
                addScatterSeriesToModel(points, marker);
            InitializeComponent();
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
        private void addLineSeriesToModel(IList<Point> points, bool closeShape, MarkerType marker)
        {
            var series = new LineSeries {MarkerType = marker};
            foreach (var point in points)
                series.Points.Add(new DataPoint(point.X, point.Y));
            if (closeShape) series.Points.Add(new DataPoint(points[0].X, points[0].Y));
            Model.Series.Add(series);
        }

        /// <summary>
        ///     Adds the scatter series to model.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="marker">The marker.</param>
        private void addScatterSeriesToModel(IList<Point> points, MarkerType marker)
        {
            var series = new LineSeries {MarkerType = marker};
            foreach (var point in points)
                series.Points.Add(new DataPoint(point.X, point.Y));
            Model.Series.Add(series);
        }
    }
}