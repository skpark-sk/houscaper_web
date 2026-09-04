using UnityEngine;
using UnityEngine.Rendering;

namespace Houscaper
{
    /// <summary>
    /// Single entry point. The scene asset holds nothing but this component; everything else —
    /// camera, scenery, HUD — is assembled here at runtime.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class Bootstrap : MonoBehaviour
    {
        static readonly Vector3 SunDirection = new Vector3(0.42f, 0.78f, 0.32f);

        Material _solid;
        Material _ghost;
        Material _sky;

        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;

            CreateMaterials();
            PushShaderGlobals();

            var camera = CreateCamera();
            var rig = camera.gameObject.AddComponent<CameraRig>();

            CreateScenery();

            var houseFilter = CreateRenderer("House", null, _solid);
            var ghost = CreateRenderer("Ghost", SceneryBuilder.BuildGhost(), _ghost).transform;
            var grid = CreateRenderer("Grid", SceneryBuilder.BuildGrid(), _solid).transform;

            var controller = gameObject.AddComponent<BuildController>();
            var ui = gameObject.AddComponent<HouscaperUI>();

            controller.Initialize(rig, houseFilter, ghost, grid);
            ui.Build(controller);
        }

        void CreateMaterials()
        {
            _solid = new Material(Resources.Load<Shader>("Shaders/Houscaper")) { name = "Solid" };
            _ghost = new Material(Resources.Load<Shader>("Shaders/HouscaperGhost")) { name = "Ghost" };
            _sky = new Material(Resources.Load<Shader>("Shaders/HouscaperSky")) { name = "Sky" };
        }

        static void PushShaderGlobals()
        {
            Shader.SetGlobalVector("_HsSunDir", SunDirection.normalized);
            Shader.SetGlobalColor("_HsSunColor", Palette.SunLight * 0.62f);
            Shader.SetGlobalColor("_HsSkyColor", Palette.SkyTop * 0.46f);
            Shader.SetGlobalColor("_HsGroundColor", Palette.Grass * 0.24f);
            Shader.SetGlobalColor("_HsFogColor", Palette.SkyHorizon);
            Shader.SetGlobalFloat("_HsFogDensity", 0.0085f);
        }

        static Camera CreateCamera()
        {
            var go = new GameObject("Camera", typeof(Camera));
            go.tag = "MainCamera";

            var camera = go.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Palette.SkyHorizon;
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.2f;
            camera.farClipPlane = 900f;
            camera.allowHDR = false;

            return camera;
        }

        void CreateScenery()
        {
            CreateRenderer("Sky", SceneryBuilder.BuildSky(), _sky);
            CreateRenderer("Water", SceneryBuilder.BuildWater(), _solid);
            CreateRenderer("Island", SceneryBuilder.BuildIsland(), _solid);
        }

        static MeshFilter CreateRenderer(string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));

            var filter = go.GetComponent<MeshFilter>();
            if (mesh != null) filter.sharedMesh = mesh;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            return filter;
        }
    }
}
