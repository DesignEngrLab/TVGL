using System.Linq;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using Color = System.Windows.Media.Color;
using Vector3 = SharpDX.Vector3;
using Colors = System.Windows.Media.Colors;
using Color4 = SharpDX.Color4;
using SharpDX.Direct3D11;
using System.Collections.Generic;
using HelixToolkit.SharpDX.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System;
using TVGL;

namespace TVGLPresenter
{


    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string info = "")
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(info));
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
                SetValue(ref camera, value, "Camera");
                CameraModel = value is PerspectiveCamera
                                       ? Perspective
                                       : value is OrthographicCamera ? Orthographic : null;
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

        protected OrthographicCamera defaultOrthographicCamera = new OrthographicCamera { Position = new System.Windows.Media.Media3D.Point3D(0, 0, 5), LookDirection = new System.Windows.Media.Media3D.Vector3D(-0, -0, -5), UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0), NearPlaneDistance = 1, FarPlaneDistance = 100 };

        protected PerspectiveCamera defaultPerspectiveCamera = new PerspectiveCamera { Position = new System.Windows.Media.Media3D.Point3D(0, 0, 5), LookDirection = new System.Windows.Media.Media3D.Vector3D(-0, -0, -5), UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0), NearPlaneDistance = 0.5, FarPlaneDistance = 150 };

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
        ~MainViewModel()
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
        public Geometry3D DefaultModel { get; private set; }
        public Geometry3D Grid { get; private set; }
        public Geometry3D FloorModel { private set; get; }
        public PhongMaterial DefaultMaterial { get; private set; }
        public PhongMaterial FloorMaterial { get; } = PhongMaterials.Silver;
        public Color GridColor { get; private set; }

        public Transform3D DefaultTransform { get; private set; }
        public Transform3D GridTransform { get; private set; }

        public Vector3D DirectionalLightDirection1 { get; private set; }
        public Vector3D DirectionalLightDirection2 { get; private set; }
        public Vector3D DirectionalLightDirection3 { get; private set; }
        public Color DirectionalLightColor { get; private set; }
        public Color AmbientLightColor { get; private set; }

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
                }
            }
            get
            {
                return wireFrame;
            }
        }



        public IList<Matrix> Instances { private set; get; }

        public MainViewModel(IList<TVGL.TessellatedSolid> tessellatedSolids)
        {

            // camera models
            CameraModelCollection = new List<string>()
            {
                Orthographic,
                Perspective,
            };

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

            // default camera model
            CameraModel = Perspective;

            Title = "Demo (HelixToolkitDX)";
            SubTitle = "Default Base View Model";
            EffectsManager = new DefaultEffectsManager();
            // ----------------------------------------------
            // titles
            this.Title = "Hardware Tessellation Demo";
            this.SubTitle = "WPF & SharpDX";

            // ---------------------------------------------
            // camera setup
            this.Camera = new PerspectiveCamera { Position = new Point3D(7, 10, 12), LookDirection = new Vector3D(-7, -10, -12), UpDirection = new Vector3D(0, 1, 0) };

            // ---------------------------------------------
            // setup lighting            
            this.AmbientLightColor = Color.FromArgb(1, 12, 12, 12);
            this.DirectionalLightColor = Colors.White;
            this.DirectionalLightDirection1 = new Vector3D(-0, -20, -20);
            this.DirectionalLightDirection2 = new Vector3D(-0, -1, +50);
            this.DirectionalLightDirection3 = new Vector3D(0, +1, 0);

            // ---------------------------------------------
            // model trafo
            this.DefaultTransform = new Media3D.TranslateTransform3D(0, 0, 0);

            // ---------------------------------------------
            // model material
            this.DefaultMaterial = new PhongMaterial
            {
                AmbientColor = Colors.Gray.ToColor4(),
                DiffuseColor = Colors.Red.ToColor4(), // Colors.LightGray,
                SpecularColor = Colors.White.ToColor4(),
                SpecularShininess = 100f,
                DiffuseMap = TextureModel.Create(new System.Uri(@"./TextureCheckerboard2.dds", System.UriKind.RelativeOrAbsolute).ToString()),
                NormalMap = TextureModel.Create(new System.Uri(@"./TextureCheckerboard2_dot3.dds", System.UriKind.RelativeOrAbsolute).ToString()),
                EnableTessellation = true,
                RenderShadowMap = true
            };
            FloorMaterial.RenderShadowMap = true;
            // ---------------------------------------------
            // init model
            var model = MakeModelVisual3D(tessellatedSolids[0]);
            model.Colors = new Color4Collection(model.Positions.Select(x => new Color4(1, 0, 0, 1)));
            DefaultModel = model;
            // ---------------------------------------------
            // floor plane grid
            this.Grid = LineBuilder.GenerateGrid(10);
            this.GridColor = Colors.Black;
            this.GridTransform = new Media3D.TranslateTransform3D(-5, -4, -5);

            var builder = new MeshBuilder(true, true, true);
            builder.AddBox(new Vector3(0, -5, 0), 60, 0.5, 60, BoxFaces.All);
            FloorModel = builder.ToMesh();

            Instances = new Matrix[] { Matrix.Identity, Matrix.Translation(10, 0, 10), Matrix.Translation(-10, 0, 10), Matrix.Translation(10, 0, -10), Matrix.Translation(-10, 0, -10), };
        }

        /// <summary>
        /// load the model from obj-file
        /// </summary>
        /// <param name="filename">filename</param>
        /// <param name="faces">Determines if facades should be treated as triangles (Default) or as quads (Quads)</param>
        //private void LoadModel(string filename, MeshFaces faces)
        //{
        //    // load model
        //    var reader = new ObjReader();
        //    var objModel = reader.Read(filename, new ModelInfo() { Faces = faces });
        //    var model = objModel[0].Geometry as MeshGeometry3D;
        //    model.Colors = new Color4Collection(model.Positions.Select(x => new Color4(1, 0, 0, 1)));
        //    DefaultModel = model;
        //}
        /// <summary>
        /// Makes the model visual3 d.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <returns>Visual3D.</returns>
        private static MeshGeometry3D MakeModelVisual3D(TessellatedSolid ts)
        {
            //var defaultMaterial = new PhongMaterial()
            //{
            //    DiffuseColor = new SharpDX.Color4(
            //        ts.SolidColor.Rf, ts.SolidColor.Gf, ts.SolidColor.Bf, ts.SolidColor.Af)
            //};
            //if (ts.HasUniformColor)
            {
                var positions =
                    ts.Faces.SelectMany(
                        f => f.Vertices.Select(v =>
                            new SharpDX.Vector3((float)v.Coordinates[0], (float)v.Coordinates[1], (float)v.Coordinates[2])));
                var normals =
                    ts.Faces.SelectMany(f =>
                        f.Vertices.Select(v =>
                            new SharpDX.Vector3((float)f.Normal[0], (float)f.Normal[1], (float)f.Normal[2])));
                var colors = ts.Faces.Select(f => f.Color != null
                ? new SharpDX.Color4(f.Color.Rf, f.Color.Gf, f.Color.Bf, f.Color.Af)
                : new SharpDX.Color4(ts.SolidColor.Rf, ts.SolidColor.Gf, ts.SolidColor.Bf, ts.SolidColor.Af));
                return new MeshGeometry3D
                {
                    Positions = new Vector3Collection(positions),
                    // TriangleIndices = new Int32Collection(triIndices),
                    Normals = new Vector3Collection(normals),
                    //Colors = new Color4Collection(colors)
                };
            }
        }
    }
}