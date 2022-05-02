using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace TVGL
{
    public class Window3DPlotViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string info = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

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

        private string subTitle;

        private string title;

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                SetValue(ref title, value, "Title");
            }
        }

        private string heading;

        public string Heading
        {
            get
            {
                return heading;
            }
            set
            {
                SetValue(ref title, value, "Heading");
            }
        }

        public string SubTitle
        {
            get
            {
                return subTitle;
            }
            set
            {
                SetValue(ref subTitle, value, "SubTitle");
            }
        }

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

        private bool groundPlaneVisible = true;
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

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
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

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Window3DPlotViewModel()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion


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


        public Window3DPlotViewModel(string heading = "", string title = "", string subtitle = "")
        {
            if (string.IsNullOrWhiteSpace(heading))
                Heading = DateTime.Now.ToShortDateString();
            else Heading = heading;
            if (string.IsNullOrWhiteSpace(title))
                Title = "title";
            else Title = title;
            if (!string.IsNullOrWhiteSpace(subtitle))
                SubTitle = subtitle;
            EffectsManager = new DefaultEffectsManager();

            // setup lighting            
            this.AmbientLightColor = System.Windows.Media.Color.FromArgb(1, 12, 12, 12);
            this.DirectionalLightColor = System.Windows.Media.Color.FromArgb(1, 188, 188, 188);
            this.DirectionalLightDirection1 = new Vector3D(10, -20, 10);
            this.DirectionalLightDirection2 = new Vector3D(10, 20, 10);
            this.DirectionalLightDirection3 = new Vector3D(20, 10, 10);
            this.DirectionalLightDirection4 = new Vector3D(-20, 10, 10);
            this.DirectionalLightDirection5 = new Vector3D(10, 10, 20);
            this.DirectionalLightDirection6 = new Vector3D(10, 10, -20);
        }

        internal void SetUpCamera(Viewport3DX view)
        {
            defaultOrthographicCamera = new OrthographicCamera
            {
                Position = new Point3D(7, 10, 12),
                LookDirection = new Vector3D(-7, -10, -12),
                UpDirection = new Vector3D(0, 1, 0),
                NearPlaneDistance = 0.1f,
                FarPlaneDistance = 5000
            };
            defaultPerspectiveCamera = new PerspectiveCamera
            {
                Position = new Point3D(7, 10, 12),
                LookDirection = new Vector3D(-7, -10, -12),
                UpDirection = new Vector3D(0, 1, 0),
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
        public void Add(IEnumerable<GeometryModel3D> models)
        {
            foreach (var model in models)
                Solids.Add(model);
        }
    }
}