using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;


namespace WindowsDesktopPresenter
{
    internal class Held3DViewModel : INotifyPropertyChanged, IDisposable
    {
        public string Title
        {
            get => title;
            set
            {
                if (title == value) return;
                title = value;
                this.OnPropertyChanged("Title");
            }
        }
        public bool HasClosed
        {
            get => hasClosed;
            set
            {
                if (hasClosed == value) return;
                hasClosed = value;
                this.OnPropertyChanged("HasClosed");
            }
        }
        public int UpdateInterval
        {
            get => updateInterval;
            set
            {
                if (updateInterval == value) return;
                updateInterval = value;
                this.timer.Change(startupTimerInterval, updateInterval);
            }
        }
        private Queue<IList<GeometryModel3D>> SeriesQueue;
        internal void AddNewSeries(IEnumerable<GeometryModel3D> solids)
        {
            SeriesQueue.Clear();
            EnqueueNewSeries(solids);
        }

        internal void EnqueueNewSeries(IEnumerable<GeometryModel3D> solids)
        {
            SeriesQueue.Enqueue(solids as IList<GeometryModel3D> ?? solids.ToList());
        }


        private string title;
        private int updateInterval = 15;
        private const int startupTimerInterval = 0;
        private bool hasClosed;

        public Window OwnedWindow { get; }

        private readonly Timer timer;

        public Held3DViewModel(Window window)
        {
            this.OwnedWindow = window;
            this.timer = new Timer(OnTimerElapsed, null, startupTimerInterval, UpdateInterval);
            SeriesQueue = new Queue<IList<GeometryModel3D>>();

            EffectsManager = new DefaultEffectsManager();

            // setup lighting            
            this.AmbientLightColor = System.Windows.Media.Color.FromArgb(1, 12, 12, 12);
            this.DirectionalLightColor = System.Windows.Media.Color.FromArgb(1, 128, 128, 128);
            this.DirectionalLightDirection1 = new Vector3D(-10, -20, 10);
            this.DirectionalLightDirection2 = new Vector3D(10, 20, -10);
            this.DirectionalLightDirection3 = new Vector3D(20, -10, 10);
            this.DirectionalLightDirection4 = new Vector3D(-20, 10, -10);
            this.DirectionalLightDirection5 = new Vector3D(-10, 10, 20);
            this.DirectionalLightDirection6 = new Vector3D(10, -10, -20);

        }

        private void OnTimerElapsed(object state)
        {
            bool newDataToShow;
            lock (this.Solids)
                newDataToShow = Update();
            if (newDataToShow)
                OnPropertyChanged("Solids");
        }

        private bool Update()
        {
            if (SeriesQueue.Count == 0) return false;
            var series = SeriesQueue.Dequeue();
            var newNumberItems = series.Count;
            for (int i = 0; i < newNumberItems; i++)
                OwnedWindow.Dispatcher.Invoke(() =>
                {
                    if (lastNumberItems > i)
                        Solids[i] = series[i];
                    else Solids.Add(series[i]);
                });
            for (int i = lastNumberItems - 1; i >= newNumberItems; i--)
                OwnedWindow.Dispatcher.Invoke(() => Solids.RemoveAt(i));
            if (lastNumberItems == 0)
                OwnedWindow.Dispatcher.Invoke(ResetCameraCommand);
            lastNumberItems = newNumberItems;
            return true;
        }
        int lastNumberItems = 0;
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string property)
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

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    this.timer.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                if (EffectsManager != null)
                {
                    var effectManager = EffectsManager as IDisposable;
                    Disposer.RemoveAndDispose(ref effectManager);
                }
                disposedValue = true;
                GC.SuppressFinalize(this);
            }
        }

        #endregion




        protected bool SetValue<T>(ref T backingField, T value, [CallerMemberName] string propertyName = "")
        {
            if (object.Equals(backingField, value))
            {
                return false;
            }

            backingField = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }
        public const string Orthographic = "Orthographic Camera";

        public const string Perspective = "Perspective Camera";

        private string cameraModel;

        private Camera camera;



        public List<string> CameraModelCollection { get; private set; }

        private Viewport3DX view;

        public string CameraModel
        {
            get
            {
                return cameraModel;
            }
            set
            {
                if (SetValue(ref cameraModel, value, "CameraModel"))
                {
                    OnCameraModelChanged();
                }
            }
        }

        public Camera Camera
        {
            get
            {
                return camera;
            }

            protected set
            {
                var origCameraIsNull = camera == null;
                Point3D camPosition;
                Vector3D lookPosition;
                Vector3D upDirection;
                if (!origCameraIsNull)
                {
                    camPosition = camera.Position;
                    lookPosition = camera.LookDirection;
                    upDirection = camera.UpDirection;
                }
                SetValue(ref camera, value, "Camera");
                CameraModel = value is PerspectiveCamera
                                       ? Perspective
                                       : value is OrthographicCamera ? Orthographic : null;
                if (!origCameraIsNull)
                {
                    camera.Position = camPosition;
                    camera.LookDirection = lookPosition;
                    camera.UpDirection = upDirection;
                }
                ResetCameraCommand();
            }
        }

        private bool groundPlaneVisible = false;
        public bool GroundPlaneVisible
        {
            get
            {
                return groundPlaneVisible;
            }
            set
            {
                SetValue(ref groundPlaneVisible, value);
            }
        }

        private IEffectsManager effectsManager;
        public IEffectsManager EffectsManager
        {
            get { return effectsManager; }
            protected set
            {
                SetValue(ref effectsManager, value);
            }
        }

        protected OrthographicCamera defaultOrthographicCamera;
        protected PerspectiveCamera defaultPerspectiveCamera;
        public event EventHandler CameraModelChanged;


        protected virtual void OnCameraModelChanged()
        {
            var eh = CameraModelChanged;
            if (eh != null)
            {
                eh(this, new EventArgs());
            }
        }



        public Material SelectedMaterial { get; } = new PhongMaterial() { EmissiveColor = SharpDX.Color.LightYellow };
        public ObservableElement3DCollection Solids { private set; get; } = new ObservableElement3DCollection();
        public Vector3D DirectionalLightDirection1 { get; private set; }
        public Vector3D DirectionalLightDirection2 { get; private set; }
        public Vector3D DirectionalLightDirection3 { get; private set; }
        public Vector3D DirectionalLightDirection4 { get; private set; }
        public Vector3D DirectionalLightDirection5 { get; private set; }
        public Vector3D DirectionalLightDirection6 { get; private set; }
        public System.Windows.Media.Color DirectionalLightColor { get; private set; }
        public System.Windows.Media.Color AmbientLightColor { get; private set; }

        private FillMode fillMode = FillMode.Solid;
        public FillMode FillMode
        {
            set
            {
                SetValue(ref fillMode, value);
            }
            get
            {
                return fillMode;
            }
        }

        private bool wireFrame = false;
        public bool Wireframe
        {
            set
            {
                if (SetValue(ref wireFrame, value))
                {
                    if (value)
                    {
                        FillMode = FillMode.Wireframe;
                    }
                    else
                    {
                        FillMode = FillMode.Solid;
                    }
                    UpdateSolidsWithFill();
                }
            }
            get
            {
                return wireFrame;
            }
        }

        private void UpdateSolidsWithFill()
        {
            foreach (var solid in Solids)
                ((GeometryModel3D)solid).FillMode = FillMode;
        }

        private Geometry3D selectedGeometry;
        public Geometry3D SelectedGeometry
        {
            set
            {
                if (SetValue(ref selectedGeometry, value))
                {
                    SelectedTransform = Solids.Where(x => ((GeometryModel3D)x).Geometry == value)
                        .Select(x => x.Transform).First();
                }
            }
            get { return selectedGeometry; }
        }
        private Media3D.Transform3D selectedTransform;
        public Media3D.Transform3D SelectedTransform
        {
            set
            {
                SetValue(ref selectedTransform, value);
            }
            get { return selectedTransform; }
        }




        internal void SetUpCamera(Viewport3DX view)
        {
            defaultOrthographicCamera = new OrthographicCamera
            {
                Position = new Point3D(7, 10, 12),
                LookDirection = new Vector3D(-7, -10, -12),
                UpDirection = new Vector3D(0, 0, 1),
                NearPlaneDistance = 0.1f,
                FarPlaneDistance = 5000
            };
            defaultPerspectiveCamera = new PerspectiveCamera
            {
                Position = new Point3D(7, 10, 12),
                LookDirection = new Vector3D(-7, -10, -12),
                UpDirection = new Vector3D(0, 0, 1),
                NearPlaneDistance = 0.1f,
                FarPlaneDistance = 5000
            };
            CameraModel = Perspective;
            // on camera changed callback
            CameraModelChanged += (s, e) =>
            {
                if (cameraModel == Orthographic)
                {
                    if (!(Camera is OrthographicCamera))
                        Camera = defaultOrthographicCamera;
                }
                else if (cameraModel == Perspective)
                {
                    if (!(Camera is PerspectiveCamera))
                        Camera = defaultPerspectiveCamera;
                }
                else
                {
                    throw new HelixToolkitException("Camera Model Error.");
                }
            };
            CameraModelCollection = new List<string>()
            {
                Orthographic,
                Perspective,
            };
            this.view = view;
            Camera = defaultPerspectiveCamera;
            ResetCameraCommand();
        }
        internal void ResetCameraCommand()
        {
            view.ZoomExtents();
            //ResetCameraCommand = new DelegateCommand(() =>
            //{
            //    (Camera as OrthographicCamera).Reset();
            //    (Camera as OrthographicCamera).FarPlaneDistance = 5000;
            //    (Camera as OrthographicCamera).NearPlaneDistance = 0.1f;
            //});
        }


    }
}
