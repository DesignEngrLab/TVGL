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


namespace WindowsDesktopPresenter
{
    internal class Stepped3DViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the maximum step index for the scroll bar (ModelSeries count - 1)
        /// </summary>
        public int MaxStepIndex
        {
            get
            {
                return Math.Max(SolidGroups.Max(g => g?.Count ?? 0),
                Math.Max(SolidTransforms.Max(t => t?.Count ?? 0),
                Math.Max(PathGroups.Max(g => g?.Count ?? 0),
                         PathTransforms.Max(t => t?.Count ?? 0))));
            }
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

        public Stepped3DViewModel()
        {
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
        internal bool Update(int stepIndex)
        {
            // Create a new collection with updated transforms
            var newSolids = new ObservableElement3DCollection();
            var allTransforms = new[] { PathTransforms, SolidTransforms };
            var k = 0;
            foreach (var groups in new List<IList<GeometryModel3D>>[] { PathGroups,SolidGroups  })
            {
                var transforms = allTransforms[k++];
                for (int i = 0; i < transforms.Count; i++)
                {
                    var elements = groups[i];
                    var transformForGroupI = transforms[i];
                    if (transformForGroupI == null)
                    { // only show the group's solids at this current timestep
                        if (stepIndex < elements.Count)
                            newSolids.Add(elements[stepIndex]);
                    }
                    else if (stepIndex < transformForGroupI.Count && transformForGroupI[stepIndex] != null)
                    {
                        var lastIndex = Math.Min(elements.Count, stepIndex) - 1;
                        var start = Math.Max(0, lastIndex-1000);
                        for (int j = start; j <= lastIndex; j++)
                        {
                            elements[j].Transform = transformForGroupI[stepIndex];
                            newSolids.Add(elements[j]);
                        }
                    }
                }
            }
            Elements = newSolids;
            RaisePropertyChanged("Elements");
            return true;
        }

        protected bool SetValue<T>(ref T backingField, T value, [CallerMemberName] string propertyName = "")
        {
            if (object.Equals(backingField, value))
            {
                return false;
            }

            backingField = value;
            this.RaisePropertyChanged(propertyName);
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



        public ObservableElement3DCollection Elements { private set; get; } = [];
        public Material SelectedMaterial { get; } = new PhongMaterial() { EmissiveColor = SharpDX.Color.LightYellow };
        public List<IList<System.Windows.Media.Media3D.Transform3D>> SolidTransforms { get; private set; } = [];

        public List<IList<GeometryModel3D>> SolidGroups { get; private set; } = [];
        public List<IList<System.Windows.Media.Media3D.Transform3D>> PathTransforms { get; internal set; } = [];

        public List<IList<GeometryModel3D>> PathGroups { get; private set; } = [];
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
            foreach (var solid in Elements)
                ((GeometryModel3D)solid).FillMode = FillMode;
        }

        private Geometry3D selectedGeometry;
        public Geometry3D SelectedGeometry
        {
            set
            {
                if (SetValue(ref selectedGeometry, value))
                {
                    SelectedTransform = Elements.Where(x => ((GeometryModel3D)x).Geometry == value)
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
