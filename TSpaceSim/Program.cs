using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TGameToolkit.Attributes;
using TGameToolkit.Drawing;
using TGameToolkit.GUI_Elements;
using TGameToolkit.Lighting;
using TGameToolkit.Objects;
using TGameToolkit.Utils;
using TGameToolkit.Windowing;
using TSpaceSim;

namespace TGameToolkit
{
    class Program
    {
        public static void Main(string[] args)
        {
            GameWindowSettings gameWindowSettings = GameWindowSettings.Default;
            NativeWindowSettings nativeWindowSettings = NativeWindowSettings.Default;
            gameWindowSettings.UpdateFrequency = 30;
            nativeWindowSettings.Title = "TGameToolkit App";
            nativeWindowSettings.Location = new Vector2i(0,0);
            nativeWindowSettings.ClientSize = (1000, 1000);
            var app = new AppWindow(gameWindowSettings, nativeWindowSettings);
            var rand = new Random();
            
            //app.RootElements.Add(new Element(app, texture:Noise.GetNoiseTex(3, 1000)));

            var sky = new GameObject();
            var skyShader = Shader.GenShader("Shaders/sky.vert", "Shaders/sky.frag");
            skyShader.OnUse = s =>
            {
                s.SetVector3("camPos", Scene.GameCamera.Pos);
                s.SetMatrix4("view", Scene.GameCamera.GetViewMatrix());
                s.SetMatrix4("projection", Scene.GameCamera.GetProjectionMatrix());
            };
            sky.Meshes.Add(MeshBuilder.GetNcSphereMesh(skyShader, 100, 90));
            Scene.GameObjects.Add(sky);
            
            var planet = new Planet();
            Scene.GameObjects.Add(planet);


            var shader = new Shader(Postprocessor.BaseVtxShaderSrc, File.ReadAllText("Shaders/postprocess.frag"));
            shader.Use();
            shader.SetFloat("atmRadius", 13f);
            shader.SetFloat("oceanRadius", planet.PlanetRadius);
            shader.SetInt("atmDepthSteps", 10);
            shader.SetFloat("scatterStrength", 2f);
            shader.OnUse = s =>
            {
                s.SetVector3("center", planet.Pos);
                s.SetVector3("viewPos", Scene.GameCamera.Pos);
                s.SetVector3("oceanCol", new Vector3(0, 0.2f, 1));
                s.SetVector3("rgbScatterFactors", new Vector3(0.04164931f, 0.11243753f, 0.27723694f));
                s.SetVector3("sunPos", new Vector3(100, -5, 30));
            };
            Scene.ScenePostprocessor = new Postprocessor(shader);
            app.RootElements.Add(new ObjectController(app, Vector2i.Zero, planet));
            app.RootElements.Add(new ShaderController(app, (0, 500), shader));
            app.Run();
        }
    }
}