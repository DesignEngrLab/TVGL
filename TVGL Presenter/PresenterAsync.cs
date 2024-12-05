// ***********************************************************************
// Assembly         : TVGL Presenter
// Author           : Matt
// Created          : 05-20-2016
//
// Last Modified By : Matt
// Last Modified On : 05-24-2016
// ***********************************************************************
// <copyright file="Presenter.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using HelixToolkit.Wpf.SharpDX;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;

namespace TVGL
{
    /// <summary>
    /// The Class HelixPresenter is the only class within the TVGL Helix Presenter
    /// project (TVGL_Presenter.dll). It is a simple static class with one main
    /// function, "Show".
    /// </summary>
    public static partial class Presenter
    {
        public enum HoldType { AddToQueue, Immediate };

        static List<Window2DHeldPlot> plot2DHeldWindows = new List<Window2DHeldPlot>();
        static List<Window3DHeldPlot> plot3DHeldWindows = new List<Window3DHeldPlot>();
        public static void Show(Polygon polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
        {
            var window = GetOrCreateWindow(id);
            window.Dispatcher.Invoke(() =>
            {
                var vm = (HeldViewModel)window.DataContext;
                if (holdType == HoldType.Immediate)
                    vm.AddNewSeries(polygon, plot2DType, closeShape, marker);
                else vm.EnqueueNewSeries(polygon, plot2DType, closeShape, marker);

                if (!string.IsNullOrEmpty(title)) vm.Title = title;

                if (timetoShow > 0)
                    vm.UpdateInterval = timetoShow;

                if (!window.IsVisible && !vm.HasClosed)
                    window.Show();
            });
        }

        private static Window GetOrCreateWindow(int id)
        {
            Window2DHeldPlot window = null;
            var makeNew = false;
            if (id < 0)
            {
                if (plot2DHeldWindows.Count != 0)
                    id = plot2DHeldWindows.Count - 1;
                else id = 0;
            }
            if (id >= 0 && id < plot2DHeldWindows.Count)
            {
                window = plot2DHeldWindows[id];
                if (window == null) makeNew = true;
                else
                {
                    bool hasClosed = true;
                    window.Dispatcher.Invoke(() => hasClosed = ((HeldViewModel)window.DataContext).HasClosed);
                    if (hasClosed)
                    {
                        plot2DHeldWindows[id] = null;
                        makeNew = true;
                    }
                }
            }
            else makeNew = true;

            if (makeNew) // then number is outside current count
            {
                while (plot2DHeldWindows.Count <= id)
                    plot2DHeldWindows.Add(null);
                Thread newWindowThread = new Thread(ThreadStartingPoint);
                newWindowThread.SetApartmentState(ApartmentState.STA);
                newWindowThread.IsBackground = true;
                newWindowThread.Start(id);
                while (plot2DHeldWindows[id] == null)
                    Thread.Sleep(100);
                window = plot2DHeldWindows[id];
            }
            return window;
        }

        private static void ThreadStartingPoint(object indexObj)
        {
            var index = (int)indexObj;
            var window = new Window2DHeldPlot();
            plot2DHeldWindows[index] = window;
            window.Show();
            System.Windows.Threading.Dispatcher.Run();
        }


        #region Show and Hang Solids
        public static void Show(Solid solid, string title = "",
            HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
        {
            if (solid is CrossSectionSolid css)
                throw new NotImplementedException();
            else
                Show([solid], title, holdType, timetoShow, id);
        }

        public static void Show(ICollection<Solid> solids, string title = "",
            HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
        {
            var window = GetOrCreate3DWindow(id);
            window.Dispatcher.Invoke(() =>
            {
                var vm = (Held3DViewModel)window.DataContext;
                if (!string.IsNullOrEmpty(title)) vm.Title = title;

                if (timetoShow > 0)
                    vm.UpdateInterval = timetoShow;
                if (holdType == HoldType.Immediate)
                    vm.AddNewSeries(ConvertSolidsToModel3D(solids));
                else vm.EnqueueNewSeries(ConvertSolidsToModel3D(solids));
                if (!window.IsVisible && !vm.HasClosed)
                    window.Show();
            });
        }
        public static void Show(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool> closePaths = null,
            IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, string title = "",
            HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1, params Solid[] solids)
        {
            var window = GetOrCreate3DWindow(id);
            window.Dispatcher.Invoke(() =>
            {
                var vm = (Held3DViewModel)window.DataContext;
                if (!string.IsNullOrEmpty(title)) vm.Title = title;

                if (timetoShow > 0)
                    vm.UpdateInterval = timetoShow;
                if (holdType == HoldType.Immediate)
                    vm.AddNewSeries(ConvertSolidsToModel3D(solids).Concat(ConvertPathsToLineModels(paths,closePaths,lineThicknesses,colors)));
                else vm.EnqueueNewSeries(ConvertSolidsToModel3D(solids).Concat(ConvertPathsToLineModels(paths, closePaths, lineThicknesses, colors)));
                if (!window.IsVisible && !vm.HasClosed)
                    window.Show();
            });
        }


        private static Window GetOrCreate3DWindow(int id)
        {
            Window3DHeldPlot window = null;
            var makeNew = false;
            if (id < 0)
            {
                if (plot3DHeldWindows.Count != 0)
                    id = plot3DHeldWindows.Count - 1;
                else id = 0;
            }
            if (id >= 0 && id < plot3DHeldWindows.Count)
            {
                window = plot3DHeldWindows[id];
                if (window == null) makeNew = true;
                else
                {
                    bool hasClosed = true;
                    window.Dispatcher.Invoke(() => hasClosed = ((Held3DViewModel)window.DataContext).HasClosed);
                    if (hasClosed)
                    {
                        plot3DHeldWindows[id] = null;
                        makeNew = true;
                    }
                }
            }
            else makeNew = true;

            if (makeNew) // then number is outside current count
            {
                while (plot3DHeldWindows.Count <= id)
                    plot3DHeldWindows.Add(null);
                Thread newWindowThread = new Thread(ThreadStartingPoint3D);
                newWindowThread.SetApartmentState(ApartmentState.STA);
                newWindowThread.IsBackground = true;
                newWindowThread.Start(id);
                while (plot3DHeldWindows[id] == null)
                    Thread.Sleep(100);
                window = plot3DHeldWindows[id];
            }
            return window;
        }
        private static void ThreadStartingPoint3D(object indexObj)
        {
            var index = (int)indexObj;
            var window = new Window3DHeldPlot();
            plot3DHeldWindows[index] = window;
            window.Show();
            System.Windows.Threading.Dispatcher.Run();
        }



        #endregion
    }
}
