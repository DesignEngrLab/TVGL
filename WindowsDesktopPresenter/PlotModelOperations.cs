using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL;
using MarkerType = TVGL.MarkerType;

namespace WindowsDesktopPresenter
{
    /// <summary>
    /// A set of extension methods for plotting data on a PlotModel. These can be used by the Window2DPlot class OR by another non-Windows based class.
    /// </summary>
    public static class PlotModelOperations
    {
        public static void PlotData(this PlotModel plotModel, IList<double[]> points, Plot2DType plot2DType, bool closeShape, MarkerType marker)
        {
            if (plot2DType == Plot2DType.Line)
                plotModel.AddLineSeriesToModel(points, closeShape, marker);
            else
                plotModel.AddScatterSeriesToModel(points, marker);
            plotModel.SetAxes(points.Select(v => new Vector2(v[0], v[1])));
        }

        public static void PlotData(this PlotModel plotModel, IEnumerable<Vector2> points, Plot2DType plot2DType, bool closeShape, MarkerType marker)
        {
            plotModel.Series.Clear();
            if (plot2DType == Plot2DType.Line)
                plotModel.AddLineSeriesToModel(points, closeShape, marker);
            else
                plotModel.AddScatterSeriesToModel(points.ToList(), marker);
            plotModel.SetAxes(points);
            plotModel.InvalidatePlot(false);
        }

        /// <summary>
        /// Plots a line. If no line color is provided, it will alternate colors.
        /// </summary>
        /// <param name="listOfArrayOfPoints"></param>
        /// <param name="plot2DType"></param>
        /// <param name="closePaths"></param>
        /// <param name="marker"></param>
        /// <param name="color"></param>
        public static void PlotData(this PlotModel plotModel, IEnumerable<IEnumerable<Vector2>> listOfArrayOfPoints, Plot2DType plot2DType, IEnumerable<bool> closePaths,
            MarkerType marker, Color color = null)
        {
            var allPoints = new List<Vector2>();
            var closedEnumerator = closePaths.GetEnumerator();
            foreach (var points in listOfArrayOfPoints)
            {
                if (points == null || !points.Any()) continue;

                while (!closedEnumerator.MoveNext())
                    closedEnumerator = closePaths.GetEnumerator();
                var isClosed = closedEnumerator.Current;

                allPoints.AddRange(points);
                if (plot2DType == Plot2DType.Line)
                    plotModel.AddLineSeriesToModel(points, isClosed, marker, color);
                else
                    plotModel.AddScatterSeriesToModel(points, marker, color);
            }
            plotModel.SetAxes(allPoints);
        }

        public static void PlotData(this PlotModel plotModel, IEnumerable<IEnumerable<Vector2>> listOfListOfPoints1, IEnumerable<IEnumerable<Vector2>> listOfListOfPoints2, Plot2DType plot2DType, bool closeShape, MarkerType marker1, MarkerType marker2)
        {
            foreach (var points in listOfListOfPoints1)
            {
                if (plot2DType == Plot2DType.Line)
                    plotModel.AddLineSeriesToModel(points, closeShape, marker1);
                else
                    plotModel.AddScatterSeriesToModel(points, marker1);
            }
            foreach (var points in listOfListOfPoints2)
            {
                if (plot2DType == Plot2DType.Line)
                    plotModel.AddLineSeriesToModel(points, closeShape, marker2);
                else
                    plotModel.AddScatterSeriesToModel(points, marker2);
            }
            var allpoints = listOfListOfPoints1.SelectMany(v => v).ToList();
            allpoints.AddRange(listOfListOfPoints2.SelectMany(v => v));
            plotModel.SetAxes(allpoints);
        }

        public static void PlotData(this PlotModel plotModel, IEnumerable<IEnumerable<IEnumerable<Vector2>>> listofListOfListOfPoints, Plot2DType plot2DType, bool closeShape, MarkerType marker)
        {
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
                            MarkerType = (OxyPlot.MarkerType)(int)marker,
                            Color = OxyColor.FromRgb(color.R, color.G, color.B)
                        };
                        foreach (var point in points)
                        {
                            series.Points.Add(new DataPoint(point.X, point.Y));
                        }
                        if (closeShape) series.Points.Add(new DataPoint(points.First().X, points.First().Y));
                        plotModel.Series.Add(series);
                    }
                }
                else
                {
                    foreach (var points in listOfListOfPoints)
                    {
                        plotModel.AddScatterSeriesToModel(points, marker);
                    }
                }
            }
            plotModel.SetAxes(listofListOfListOfPoints.SelectMany(poly => poly.SelectMany(v => v)));
        }

        /// <summary>
        ///     Adds the line series to model.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void AddLineSeriesToModel(this PlotModel plotModel, IEnumerable<Vector2> points, bool closeShape, MarkerType marker, TVGL.Color color = null)
        {
            AddLineSeriesToModel(plotModel, PointsToDouble(points), closeShape, marker, color);
        }

        /// <summary>
        ///     Adds the line series to model.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        private static void AddLineSeriesToModel(this PlotModel plotModel, IList<double[]> points, bool closeShape, MarkerType marker, TVGL.Color color = null)
        {
            if (!points.Any()) return;
            var series = new LineSeries { MarkerType = (OxyPlot.MarkerType)(int)marker };

            if (color != null)
                series.Color = OxyColor.FromRgb(color.R, color.G, color.B);

            foreach (var point in points)
                //point[0] == x, point[1] == y
                series.Points.Add(new DataPoint(point[0], point[1]));
            if (closeShape) series.Points.Add(new DataPoint(points[0][0], points[0][1]));
            plotModel.Series.Add(series);
        }

        /// <summary>
        /// Adds an Area Series to a model. Keep this public.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="marker"></param>
        /// <param name="lineColor"></param>
        /// <param name="fillColor"></param>
        public static void AddAreaSeriesToModel(this PlotModel plotModel, IEnumerable<Vector2> points, MarkerType marker, Color lineColor, Color fillColor)
        {
            if (points == null || points.Count() < 3) return;
            if (lineColor == null) lineColor = new Color(KnownColors.Black);
            if (fillColor == null) fillColor = new Color(KnownColors.LightGray);

            var areaSeries = new AreaSeries
            {
                Color = OxyColor.FromArgb(lineColor.A, lineColor.R, lineColor.G, lineColor.B),
                Fill = OxyColor.FromArgb(fillColor.A, fillColor.R, fillColor.G, fillColor.B),
                MarkerType = (OxyPlot.MarkerType)(int)marker,
                LineStyle = LineStyle.Solid,
            };

            foreach (var point in points)
                //point[0] == x, point[1] == y
                areaSeries.Points.Add(new DataPoint(point.X, point.Y));

            //Close shape is required for fill.
            areaSeries.Points.Add(new DataPoint(points.First().X, points.First().Y));
            plotModel.Series.Add(areaSeries);
        }

        /// <summary>
        ///     Adds the scatter series to model.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="marker">The marker.</param>
        private static void AddScatterSeriesToModel(this PlotModel plotModel, IEnumerable<Vector2> points, MarkerType marker, TVGL.Color color = null)
        {
            AddScatterSeriesToModel(plotModel, PointsToDouble(points), marker, color);
        }

        /// <summary>
        ///     Adds the scatter series to model.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="marker">The marker.</param>
        private static void AddScatterSeriesToModel(this PlotModel plotModel, IList<double[]> points, MarkerType marker, TVGL.Color color = null)
        {
            var series = new LineSeries
            {
                MarkerType = (OxyPlot.MarkerType)(int)marker,
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
            plotModel.Series.Add(series);
        }


        internal static void SetAxes(this PlotModel plotModel, IEnumerable<Vector2> polygons)
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


        private static List<double[]> PointsToDouble(IEnumerable<Vector2> points)
        {
            return points.Select(p => new[] { p.X, p.Y }).ToList();
        }

    }
}
