using OxyPlot;
using OxyPlot.Series;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using TVGL;
using MarkerType = TVGL.MarkerType;

namespace WindowsDesktopPresenter
{

    /// <summary>
    ///     Class Window2DPlot.
    /// </summary>
    public partial class Window2DFlipPlot : Window
    {
        const int MaxNumberOfPlots = 100;
        
        ObservableCollection<PlotModel> Models = new ObservableCollection<PlotModel>();
        public PlotModel SelectedModel { get; set; }
        public int SelectedIndex { get; private set; }

        private void ScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Calculate the index based on the value of the ScrollBar
            int selectedIndex = (int)e.NewValue;
            
            // Update the SelectedIndex property
            SelectedIndex = selectedIndex;

            // Update the PlotView by setting the Model property to the selected PlotModel
            if (SelectedIndex >= 0 && SelectedIndex < Models.Count)
            {
                SelectedModel = Models[SelectedIndex];
                mainView.Model = SelectedModel;
                mainView.Model.InvalidatePlot(true);
            }
        }


        /// <summary>
        ///     Initializes a new instance of the <see cref="Window2DPlot" /> class.
        /// </summary>
        /// <param name="title">The title.</param>
        private Window2DFlipPlot(string title)
        {
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
        //public Window2DFlipPlot(IEnumerable<IEnumerable<Vector2>> listOfArrayOfPoints, string title,
        //    Plot2DType plot2DType, bool closeShape, MarkerType marker,
        //    IList<TVGL.Color> colors = null) : this(title)
        //{
        //    var i = 0;
        //    foreach (var points in listOfArrayOfPoints)
        //    {
        //        //Note: both methods below will accept null colors, so set to null by default
        //        TVGL.Color color = null;
        //        if (colors != null)
        //        {
        //            color = colors[i++];
        //        }
        //        if (plot2DType == Plot2DType.Line)
        //            AddLineSeriesToModel(points, closeShape, marker, color);
        //        else
        //            AddScatterSeriesToModel(points, marker, color);
        //    }
        //    InitializeComponent();
        //}


        /// <summary>
        ///     Initializes a new instance of the <see cref="Window2DPlot" /> class.
        ///     This version allows different markers, plotTypes, and allows each set of polygons to be either
        ///     open or closed.
        /// </summary>
        /// <param name="listOfListOfPoints2"></param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="listOfListOfPoints1"></param>
        /// <param name="marker1"></param>
        /// <param name="marker2"></param>
        //public Window2DFlipPlot(IEnumerable<IEnumerable<Vector2>> listOfListOfPoints1,
        //    IEnumerable<IEnumerable<Vector2>> listOfListOfPoints2,
        //    string title, Plot2DType plot2DType1, Plot2DType plot2DType2,
        //    bool closeShape1, bool closeShape2,
        //    MarkerType marker1, MarkerType marker2) : this(title)
        //{
        //    foreach (var points in listOfListOfPoints1)
        //    {
        //        if (plot2DType1 == Plot2DType.Line)
        //            AddLineSeriesToModel(points, closeShape1, marker1);
        //        else
        //            AddScatterSeriesToModel(points, marker1);
        //    }
        //    foreach (var points in listOfListOfPoints2)
        //    {
        //        if (plot2DType2 == Plot2DType.Line)
        //            AddLineSeriesToModel(points, closeShape2, marker2);
        //        else
        //            AddScatterSeriesToModel(points, marker2);
        //    }
        //    InitializeComponent();
        //}

        /// <summary>
        ///     Initializes a new instance of the <see cref="Window2DPlot" /> class.
        ///     This version allows each individual polygon to be open or closed and allows different markers to be set for each set of polygons.
        /// </summary>
        /// <param name="listOfListOfPoints2"></param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="listOfListOfPoints1"></param>
        /// <param name="marker1"></param>
        /// <param name="marker2"></param>
        //public Window2DFlipPlot(IEnumerable<(IEnumerable<Vector2>, bool)> listOfListOfPoints1,
        //    IEnumerable<(IEnumerable<Vector2>, bool)> listOfListOfPoints2,
        //    string title, MarkerType marker1, MarkerType marker2) : this(title)
        //{
        //    foreach (var points in listOfListOfPoints1)
        //    {
        //        AddLineSeriesToModel(points.Item1, points.Item2, marker1);
        //    }
        //    foreach (var points in listOfListOfPoints2)
        //    {
        //        AddLineSeriesToModel(points.Item1, points.Item2, marker2);
        //    }
        //    InitializeComponent();
        //}


        public Window2DFlipPlot(IEnumerable<List<List<Vector2>>> listofListOfListOfPoints, string title,
            Plot2DType plot2DType, bool closeShape, MarkerType marker) : this(title)
        {
            var i = 0;
            var colorPalet = new List<string>(); // ColorPalette();
            foreach (var listOfListOfPoints in listofListOfListOfPoints)
            {
                if (plot2DType == Plot2DType.Line)
                {
                    //Set each list of points as its own color.
                    //Close each list of points 
                    foreach (var points in listOfListOfPoints)
                    {
                        var color = new Color(colorPalet[i]);
                        i++;
                        if (i == colorPalet.Count) i = 0;
                        var series = new LineSeries
                        {
                            MarkerType = (OxyPlot.MarkerType)marker,
                            Color = OxyColor.FromRgb(color.R, color.G, color.B)
                        };
                        foreach (var point in points)
                        {
                            series.Points.Add(new DataPoint(point.X, point.Y));
                        }
                        if (closeShape) series.Points.Add(new DataPoint(points[0].X, points[0].Y));
                        var plotModel = new PlotModel();
                        plotModel.Series.Add(series);
                        Models.Add(plotModel);
                    }
                }
                else
                {
                    foreach (var points in listOfListOfPoints)
                    {
                        var plotModel = new PlotModel();
                        Models.Add(plotModel);
                        AddScatterSeriesToModel(plotModel, points, marker);
                    }
                }
            }
            ReduceModels();
            InitializeComponent();
        }

        private void ReduceModels()
        {
            if (Models.Count > MaxNumberOfPlots)
            {
                var reducedModels = new ObservableCollection<PlotModel>();
                var step = Models.Count / (double)MaxNumberOfPlots;
                var i = 0.0;
                for (; i < Models.Count; i += step)
                    reducedModels.Add(Models[(int)i]);
                reducedModels[^1] = Models[^1];
                Models = reducedModels;
            }
        }

        public Window2DFlipPlot(ICollection<double[,]> allData, string title) : this(title)
        {
            Models.Clear();
            foreach (var data in allData)
            {
                var model = new PlotModel();
                var heatMapSeries = new HeatMapSeries
                {
                    X0 = 0,
                    X1 = data.GetLength(0),
                    Y0 = 0,
                    Y1 = data.GetLength(1),
                    Interpolate = false,
                    RenderMethod = HeatMapRenderMethod.Bitmap,
                    Data = data
                };
                // Color axis (the X and Y axes are generated automatically)
                model.Axes.Add(new OxyPlot.Axes.LinearColorAxis
                {
                    Palette = OxyPalettes.Rainbow(32)
                });
                model.Series.Add(heatMapSeries);
                model.InvalidatePlot(false);
            }
            ReduceModels();
            InitializeComponent();
        }
        public Window2DFlipPlot(ICollection<double[,]> allData, IEnumerable<IEnumerable<Vector2>> allPoints, bool connectPointsInLine, string title) : this(title)
        {
            Models.Clear();
            var dataEnumerator = allData.GetEnumerator();
            var pointsEnumerator = allPoints.GetEnumerator();
            int dataIndex = 0, pointsIndex = 0;
            bool hasData = dataEnumerator.MoveNext();
            bool hasPoints = pointsEnumerator.MoveNext();

            while (hasData || hasPoints)
            {
                PlotModel model = null;
                if (hasData)
                {
                    model = new PlotModel();
                    var data = dataEnumerator.Current;
                    var heatMapSeries = new HeatMapSeries
                    {
                        X0 = 0,
                        X1 = data.GetLength(0),
                        Y0 = 0,
                        Y1 = data.GetLength(1),
                        Interpolate = false,
                        RenderMethod = HeatMapRenderMethod.Bitmap,
                        Data = data
                    };
                    // Color axis (the X and Y axes are generated automatically)
                    model.Axes.Add(new OxyPlot.Axes.LinearColorAxis
                    {
                        Palette = OxyPalettes.Rainbow(32)
                    });
                    model.Series.Add(heatMapSeries);
                    Models.Add(model);
                    hasData = dataEnumerator.MoveNext();
                    dataIndex++;
                }

                if (hasPoints)
                {
                    var points = pointsEnumerator.Current;
                    // If model is null (no data for this index), create a new PlotModel
                    if (model == null)
                    {
                        model = new PlotModel();
                        Models.Add(model);
                    }
                    if (connectPointsInLine)
                    {
                        var lineSeries = new LineSeries { MarkerType = OxyPlot.MarkerType.None, Color = OxyColors.Black };
                        foreach (var point in points)
                            lineSeries.Points.Add(new DataPoint(point.X, point.Y));
                        model.Series.Add(lineSeries);
                    }
                    else
                    {
                        var scatterSeries = new ScatterSeries { MarkerType = OxyPlot.MarkerType.Circle };
                        foreach (var point in points)
                            scatterSeries.Points.Add(new ScatterPoint(point.X, point.Y, 5, 0.1));
                        model.Series.Add(scatterSeries);
                    }
                    model.InvalidatePlot(false);
                    hasPoints = pointsEnumerator.MoveNext();
                    pointsIndex++;
                }
                else if (model != null)
                {
                    model.InvalidatePlot(false);
                }
            }
            ReduceModels();
            InitializeComponent();
        }

        public Window2DFlipPlot(ICollection<double[,]> allData, IEnumerable<IEnumerable<IEnumerable<Vector2>>> allPoints,
            IEnumerable<bool> allConnects, string title) : this(title)
        {
            Models.Clear();

            // Get enumerators for both collections
            var dataEnumerator = allData.GetEnumerator();
            var pointsEnumerator = allPoints.GetEnumerator();
            var connectsEnumerator = allConnects.GetEnumerator();
            int dataIndex = 0, pointsIndex = 0;
            bool hasData = dataEnumerator.MoveNext();
            bool hasPoints = pointsEnumerator.MoveNext();

            while (hasData || hasPoints)
            {
                PlotModel model = null;
                if (hasData)
                {
                    model = new PlotModel();
                    var data = dataEnumerator.Current;
                    var heatMapSeries = new HeatMapSeries
                    {
                        X0 = 0,
                        X1 = data.GetLength(0),
                        Y0 = 0,
                        Y1 = data.GetLength(1),
                        Interpolate = false,
                        RenderMethod = HeatMapRenderMethod.Bitmap,
                        Data = data
                    };
                    // Color axis (the X and Y axes are generated automatically)
                    model.Axes.Add(new OxyPlot.Axes.LinearColorAxis
                    {
                        Palette = OxyPalettes.Rainbow(32)
                    });
                    model.Series.Add(heatMapSeries);
                    Models.Add(model);
                    hasData = dataEnumerator.MoveNext();
                    dataIndex++;
                }

                if (hasPoints)
                {
                    var points = pointsEnumerator.Current;
                    // If model is null (no data for this index), create a new PlotModel
                    if (model == null)
                    {
                        model = new PlotModel();
                        Models.Add(model);
                    }
                    if (connectsEnumerator.MoveNext() && connectsEnumerator.Current)
                    {
                        foreach (var pointSet in points)
                        {
                            var lineSeries = new LineSeries { MarkerType = OxyPlot.MarkerType.None, Color = OxyColors.Black };
                            foreach (var point in pointSet)
                                lineSeries.Points.Add(new DataPoint(point.X, point.Y));
                            model.Series.Add(lineSeries);
                        }
                    }
                    else
                    {
                        foreach (var pointSet in points)
                        {
                            var scatterSeries = new ScatterSeries { MarkerType = OxyPlot.MarkerType.Circle };
                            foreach (var point in pointSet)
                                scatterSeries.Points.Add(new ScatterPoint(point.X, point.Y, 5, 0.1));
                            model.Series.Add(scatterSeries);
                        }
                    }
                    model.InvalidatePlot(false);
                    hasPoints = pointsEnumerator.MoveNext();
                    pointsIndex++;
                }
                else if (model != null)
                {
                    model.InvalidatePlot(false);
                }
            }
            ReduceModels();
            InitializeComponent();
        }

        /// <summary>
        ///     Adds the line series to model.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        private void AddLineSeriesToModel(PlotModel model, IEnumerable<Vector2> points, bool closeShape, MarkerType marker,
            TVGL.Color color = null)
        {
            var series = new LineSeries { MarkerType = (OxyPlot.MarkerType)marker };
            //Add color to series if applicable
            if (color != null)
                series.Color = OxyColor.FromRgb(color.R, color.G, color.B);

            foreach (var point in points)
                series.Points.Add(new DataPoint(point.X, point.Y));
            if (closeShape && points.Any())
                series.Points.Add(new DataPoint(points.First().X, points.First().Y));
            model.Series.Add(series);
        }

        /// <summary>
        ///     Adds the scatter series to model.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="marker">The marker.</param>
        private void AddScatterSeriesToModel(PlotModel model, IEnumerable<Vector2> points, MarkerType marker,
            TVGL.Color color = null)
        {
            var series = new LineSeries { MarkerType = (OxyPlot.MarkerType)marker };
            //Add color to series if applicable
            if (color != null)
                series.Color = OxyColor.FromRgb(color.R, color.G, color.B);

            foreach (var point in points)
                //point[0] == x, point[1] == y
                series.Points.Add(new DataPoint(point.X, point.Y));
            model.Series.Add(series);
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            SelectedModel = Models[0];
            mainView.Model = SelectedModel;
            mainView.Model.InvalidatePlot(true);

        }
    }

}