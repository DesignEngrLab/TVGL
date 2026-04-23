using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Windows;
using TVGL;
using MarkerType = TVGL.MarkerType;

namespace WindowsDesktopPresenter
{

    /// <summary>
    ///     Class Window2DPlot for use on Windows Desktop.
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


        public Window2DPlot(PlotModel model, string title)
        {
            Title = title;
            Model = model;
            InitializeComponent();
        }

        public Window2DPlot(IEnumerable<Vector2> points, string title, Plot2DType plot2DType, bool closeShape,
         MarkerType marker) : this(title)
        {
            Model.PlotData(points, plot2DType, closeShape, marker);
            InitializeComponent();
        }


        public Window2DPlot(double[,] data, string title) : this(title)
        {
            Model.Series.Clear();
            var heatMapSeries = new HeatMapSeries
            {
                X0 = 0,
                X1 = 99,
                Y0 = 0,
                Y1 = 99,
                Interpolate = true,
                RenderMethod = HeatMapRenderMethod.Bitmap,
                Data = data
            };

            Model.Series.Add(heatMapSeries);
            Model.InvalidatePlot(false);
            InitializeComponent();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Window2DPlot" /> class.
        /// </summary>
        /// <param name="listOfArrayOfPoints">The list of array of points.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public Window2DPlot(IEnumerable<IEnumerable<Vector2>> listOfArrayOfPoints, string title, Plot2DType plot2DType, IEnumerable<bool> closeShape,
            MarkerType marker, Color color = null) : this(title)
        {
            if (plot2DType == Plot2DType.Area)
                throw new Exception("This function is not intended for Plot2DType.Area. LineColor and FillColor are required.");
            Model.PlotData(listOfArrayOfPoints, plot2DType, closeShape, marker, color);
            InitializeComponent();
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
            Model.PlotData(listOfListOfPoints1, listOfListOfPoints2, plot2DType, closeShape, marker1, marker2);
            InitializeComponent();
        }
        public Window2DPlot(IEnumerable<IEnumerable<IEnumerable<Vector2>>> listofListOfListOfPoints,
            string title, Plot2DType plot2DType, bool closeShape, MarkerType marker) : this(title)
        {
            Model.PlotData(listofListOfListOfPoints, plot2DType, closeShape, marker);
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
        public Window2DPlot(IList<double[]> points, string title, Plot2DType plot2DType, bool closeShape, MarkerType marker)
            : this(title)
        {
            Model.PlotData(points, plot2DType, closeShape, marker);
            InitializeComponent();
        }

        /// <summary>
        ///     Gets or sets the model.
        /// </summary>
        /// <value>The model.</value>
        public PlotModel Model { get; set; }
    }
}