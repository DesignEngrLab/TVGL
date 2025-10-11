using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using TVGL;
using MarkerType = TVGL.MarkerType;

namespace WindowsDesktopPresenter
{
    internal class Held2DViewModel : HeldViewModel
    {
        public PlotModel PlotModel { get; set; }
        private Queue<ICollection<LineSeries>> SeriesQueue;
        internal void AddNewSeries(IEnumerable<IEnumerable<Vector2>> paths, Plot2DType plot2DType, IEnumerable<bool> closePaths,
            MarkerType marker)
        {
            SeriesQueue.Clear();
            EnqueueNewSeries(paths, plot2DType, closePaths, marker);
        }

        internal void EnqueueNewSeries(IEnumerable<IEnumerable<Vector2>> paths, Plot2DType plot2DType, IEnumerable<bool> closePaths,
            MarkerType marker)
        {
            var listOfPlots = new List<LineSeries>();
            var closedEnumerator = closePaths != null ? closePaths.GetEnumerator() : new Repeater<bool>(true);
            foreach (var path in paths)
            {
                closedEnumerator.MoveNext();
                var isClosed = closedEnumerator.Current;
                var series = new LineSeries();
                foreach (var vertex in path)
                    series.Points.Add(new DataPoint(vertex.X, vertex.Y));
                if (isClosed)
                    series.Points.Add(new DataPoint(path.First().X, path.First().Y));
                series.MarkerType = (OxyPlot.MarkerType)(int)marker;
                if (plot2DType == Plot2DType.Line)
                    series.LineStyle = LineStyle.Solid;
                else series.LineStyle = LineStyle.None;
                series.MarkerType = (OxyPlot.MarkerType)(int)marker;
                listOfPlots.Add(series);
            }
            SeriesQueue.Enqueue(listOfPlots);
        }



        public Held2DViewModel()
        {
            this.timer = new Timer(OnTimerElapsed);
            SeriesQueue = new Queue<ICollection<LineSeries>>();
            PlotModel = new PlotModel();
        }


        internal void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.timer.Change(Timeout.Infinite, Timeout.Infinite);

            //PlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = -100, Maximum = 200 });
            //PlotModel.InvalidatePlot(true);
            //this.watch.Start();

            this.RaisePropertyChanged("PlotModel");

            this.timer.Change(100, UpdateInterval);
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.timer.Dispose();
                }
            }

            this.disposed = true;
        }

        private void OnTimerElapsed(object state)
        {
            bool newDataToShow;
            lock (this.PlotModel.SyncRoot)
            {
                newDataToShow = Update();
            }
            if (newDataToShow)
            {
                this.RaisePropertyChanged("PlotModel");
                this.PlotModel.InvalidatePlot(true);
            }
        }

        private bool Update()
        {
            if (SeriesQueue.Count == 0) return false;
            ICollection<LineSeries> series = null;
            try { series = SeriesQueue.Dequeue(); }
            catch { return false; }
            if (series == null) return false;
            PlotModel.Series.Clear();
            lock (series)
                foreach (var s in series)
                    PlotModel.Series.Add(s);
            return true;
        }



    }
}
