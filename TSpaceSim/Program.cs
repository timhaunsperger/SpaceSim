using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using TGameToolkit.Graphics;
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
            
            //app.RootElements.Add(new Element(app, texture:Noise.WorleyNoiseTex(1000, 1000, 300)));
            var comp = new ComputeShader("Shaders/Noise.comp");
            var tex = new Texture3d(128, 128, 128, internalFormat: PixelInternalFormat.Rgba32f, wrapBehavior: TextureWrapMode.Repeat);
            GL.BindImageTexture(0, tex.Handle, 0, true, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba32f);
            float[] noisePoints = new float[600];
            
            var variation = 0f;
            
            for (int i = 0; i < noisePoints.Length; i+=3)
            {
                var rndPt1 = new Vector3(rand.NextSingle() * 2 - 1, rand.NextSingle() * 2 - 1, rand.NextSingle() * 2 - 1);
                var rndPt2 = new Vector3(rand.NextSingle() * 2 - 1, rand.NextSingle() * 2 - 1, rand.NextSingle() * 2 - 1);
                var noisePt = rndPt1.Normalized() + rndPt2 * variation;
                noisePoints[i] = (noisePt.X + 1) / 2;
                noisePoints[i+1] = (noisePt.Y + 1) / 2;
                noisePoints[i+2] = (noisePt.Z + 1) / 2;
            }

            int noisePointsBuf = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, noisePointsBuf);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, noisePoints.Length * 4, noisePoints, BufferUsageHint.StaticRead);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, noisePointsBuf);
            
            comp.Dispatch(128, 128, 128);
            comp.BlockGpu();
            
            // Add background sphere
            var sky = new GameObject();
            var skyShader = Shader.GenShader("Shaders/sky.vert", "Shaders/sky.frag");
            skyShader.OnUse = s =>
            {
                s.SetVector3("camPos", Scene.GameCamera.Pos);
                s.SetMatrix4("view", Scene.GameCamera.GetViewMatrix());
                s.SetMatrix4("projection", Scene.GameCamera.GetProjectionMatrix());
            };
            sky.Meshes.Add(MeshBuilder.GetNcSphereMesh(skyShader, 10, 90));
            Scene.GameObjects.Add(sky);
            
            // Add sun
            var sun = new GameObject();
            var sunShader = Shader.GenShader("Shaders/basic.vert", "Shaders/sun.frag");
            sunShader.OnUse = s =>
            {
                s.SetMatrix4("view", Scene.GameCamera.GetViewMatrix());
                s.SetMatrix4("projection", Scene.GameCamera.GetProjectionMatrix());
            };
            var sunMesh = MeshBuilder.GetNcSphereMesh(sunShader, 10, 2);
            sunMesh.Move(new Vector3d(60, -3, 18));
            sun.Meshes.Add(sunMesh);
            Scene.GameObjects.Add(sun);
            
            // Add planet
            var planet = new Planet();
            
            Scene.GameObjects.Add(planet);
            
            // Setup postprocessor
            var shader = new Shader(Postprocessor.BaseVtxShaderSrc, File.ReadAllText("Shaders/postprocess.frag"));
            shader.Use();
            shader.SetFloat("atmRadius", planet.PlanetRadius*1.25f);
            shader.SetFloat("cloudRadius", planet.PlanetRadius*1.2f);
            shader.SetFloat("oceanRadius", planet.PlanetRadius);
            shader.SetInt("atmDepthSteps", 10);
            shader.SetFloat("scatterStrength", 5f);
            shader.SetFloat("cloudScatterStr", 3f);
            shader.OnUse = s =>
            {
                tex.Use(TextureUnit.Texture4);
                s.SetInt("cloudTex", 4);
                s.SetVector3("center", planet.Pos);
                s.SetVector3("viewPos", Scene.GameCamera.Pos);
                s.SetVector3("oceanCol", new Vector3(0, 0.2f, 1));
                s.SetVector3("rgbScatterFactors", new Vector3(0.04165f, 0.1425f, 0.2772f));
                s.SetVector3("sunPos", new Vector3(100, -5, 30));
            };
            Scene.ScenePostprocessor = new Postprocessor(shader);
            
            // Add controllers
            app.RootElements.Add(new ObjectController(app, Vector2i.Zero, planet));
            app.RootElements.Add(new ShaderController(app, (0, 500), shader));
            app.Run();
        }
    }
}