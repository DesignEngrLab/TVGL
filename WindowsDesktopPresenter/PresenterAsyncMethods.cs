using System.Collections.Generic;
using System.Threading;
using System.Windows;

namespace WindowsDesktopPresenter
{
    /// <summary>
    /// The Class HelixPresenter is the only class within the TVGL Helix Presenter
    /// project (TVGL_Presenter.dll). It is a simple  class with one main
    /// function, "Show".
    /// </summary>
    internal static class PresenterAsyncMethods
    {
        internal static T GetOrCreate3DWindow<T>(int id, List<T> plotWindows) where T : Window, new()
        {
            T window = null;
            var makeNew = false;
            if (id < 0)
            {
                if (plotWindows.Count != 0)
                    id = plotWindows.Count - 1;
                else id = 0;
            }
            if (id >= 0 && id < plotWindows.Count)
            {
                window = plotWindows[id];
                if (window == null) makeNew = true;
                else
                {
                    bool hasClosed = true;
                    window.Dispatcher.Invoke(() => hasClosed = ((HeldViewModel)window.DataContext).HasClosed);
                    if (hasClosed)
                    {
                        plotWindows[id] = null;
                        makeNew = true;
                    }
                }
            }
            else makeNew = true;

            if (makeNew) // then number is outside current count
            {
                while (plotWindows.Count <= id)
                    plotWindows.Add(null);
                Thread newWindowThread = new Thread((index) => ThreadStartingPoint3D(index, plotWindows));
                newWindowThread.SetApartmentState(ApartmentState.STA);
                newWindowThread.IsBackground = true;
                newWindowThread.Start(id);
                while (plotWindows[id] == null)
                    Thread.Sleep(100);
                window = plotWindows[id];
            }
            return window;
        }

        private static void ThreadStartingPoint3D<T>(object indexObj, List<T> plotWindows) where T : Window, new()
        {
            var index = (int)indexObj;
            var window = new T();
            plotWindows[index] = window;
            window.Show();
            System.Windows.Threading.Dispatcher.Run();
        }


    }
}
