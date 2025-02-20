using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;

namespace TVGL
{
    internal class HeldViewModel : INotifyPropertyChanged, IDisposable
    {
        public string Title
        {
            get => title;
            set
            {
                if (title == value) return;
                title = value;
                this.RaisePropertyChanged("Title");
            }
        }
        public bool HasClosed
        {
            get => hasClosed;
            set
            {
                if (hasClosed == value) return;
                hasClosed = value;
                this.RaisePropertyChanged("HasClosed");
            }
        }
        public PlotModel PlotModel { get; set; }
        public int UpdateInterval
        {
            get => updateInterval;
            set
            {
                if (updateInterval == value) return;
                updateInterval = value;
                this.RaisePropertyChanged("UpdateInterval");
                this.timer.Change(updateInterval, updateInterval);
            }
        }
        private Queue<ICollection<LineSeries>> SeriesQueue;
        internal void AddNewSeries(IEnumerable<IEnumerable<Vector2>> paths, Plot2DType plot2DType, IEnumerable<bool> closePaths, MarkerType marker)
        {
            SeriesQueue.Clear();
            EnqueueNewSeries(paths, plot2DType, closePaths, marker);
        }

        internal void EnqueueNewSeries(IEnumerable<IEnumerable<Vector2>> paths, Plot2DType plot2DType, IEnumerable<bool> closePaths, MarkerType marker)
        {
            var listOfPlots = new List<LineSeries>();
            if (closePaths == null) closePaths = [true];
            var closedEnumerator = closePaths.GetEnumerator();
            foreach (var path in paths)
            {
                while (!closedEnumerator.MoveNext())
                    closedEnumerator = closePaths.GetEnumerator();
                var isClosed = closedEnumerator.Current;
                var series = new LineSeries();
                foreach (var vertex in path)
                    series.Points.Add(new DataPoint(vertex.X, vertex.Y));
                if (isClosed)
                    series.Points.Add(new DataPoint(path.First().X, path.First().Y));
                series.MarkerType = marker;
                if (plot2DType == Plot2DType.Line)
                    series.LineStyle = LineStyle.Solid;
                else series.LineStyle = LineStyle.None;
                series.MarkerType = marker;
                listOfPlots.Add(series);
            }
            SeriesQueue.Enqueue(listOfPlots);
        }


        private bool disposed;
        private string title;
        private int updateInterval = 15;
        private bool hasClosed;
        private readonly Timer timer;

        public HeldViewModel()
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
            var series = SeriesQueue.Dequeue();
            PlotModel.Series.Clear();
            lock (series)
                foreach (var s in series)
                    PlotModel.Series.Add(s);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string property)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }

        internal void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            HasClosed = true;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
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
    }
}
