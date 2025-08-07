using HelixToolkit.SharpDX.Core;
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
    internal abstract class HeldViewModel : INotifyPropertyChanged, IDisposable
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

        protected bool disposed;
        protected string title;
        protected int updateInterval = 15;
        private bool hasClosed;
        protected  Timer timer;

   


        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string property)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }


        #region IDisposable Support

        internal void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            HasClosed = true;
        }
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected bool disposedValue = false; // To detect redundant calls

        protected abstract void Dispose(bool disposing);

        #endregion
    }
}
