using OpenTK.Mathematics;
using TGameToolkit;
using TGameToolkit.Attributes;
using TGameToolkit.Graphics;
using TGameToolkit.Objects;
using TGameToolkit.Utils;

namespace TSpaceSim;

public class Planet : GameObject
{
    public float PlanetRadius = 10;
    public int TerrainRes = 200;

    // Mountains
    public float MtnAmp = 3f;
    public float MtnFreq = 24f;
    public float HillAmp = 2f;
    public float HillFreq = 8f;
    public float MtnRangeFreq = 3f;
    public bool Mountains = true;
    public bool Hills = true;
    
    // Ridges
    public float RidgeAmp = 0.015f;
    public float RidgeFreq = 3f;
    public bool Ridges = true;
    
    // Continents
    public float ContFreq = 2.3f;
    public float ContAmp = 0.09f;
    public bool Continents = true;
    

    private RenderMesh _surfaceMesh;
    
    public Planet()
    {
        var shader = Shader.GenShader("Shaders/planet.vert", "Shaders/planet.frag");

        shader.OnUse = s =>
        {
            s.SetMatrix4("view", Scene.GameCamera.GetViewMatrix());
            s.SetMatrix4("projection", Scene.GameCamera.GetProjectionMatrix());
            s.SetVector3("viewPos", Scene.GameCamera.Pos);
            s.SetInt("numLights", Scene.Lights.Count);
            for (int i = 0; i < Scene.Lights.Count; i++)
            {
                Scene.Lights[i].Use(s, i);
            }

            Scene.GlobalLight?.Use(s);
        };
        
        _surfaceMesh = MeshBuilder.GetNcSphereMesh(
            shader, TerrainRes, PlanetRadius, Quaterniond.FromEulerAngles(90,0,0));
        
        //_surfaceMesh.Materials.Add("land", new Material{ SpecularStrength = 1, DiffuseStrength = 2, AmbientStrength = 0});
        
        UpdateSurface();
        Meshes.Add(_surfaceMesh);
    }

    protected override void OnUpdate(double deltaTime)
    {
        var vertices = _surfaceMesh.GetVertices();
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = Quaterniond.FromEulerAngles(0.001, 0, 0) * vertices[i];
        }
        _surfaceMesh.SetVertices(vertices);
    }

    public override void OnModify()
    {
        Meshes.Clear();
        _surfaceMesh = MeshBuilder.GetNcSphereMesh(
            _surfaceMesh.Shader, TerrainRes, PlanetRadius, Quaterniond.FromEulerAngles(90,0,0));
        UpdateSurface();
        Meshes.Add(_surfaceMesh);
    }

    private void UpdateSurface()
    {
        var vertices = _surfaceMesh.GetVertices();
        for (int i = 0; i < vertices.Length; i++)
        {
            var p = (Vector3)vertices[i];
            
            var contNoise = Continents ? Noise.Fbr(p * ContFreq / PlanetRadius) * ContAmp : 0;
            
            var mtnRangeNoise = Noise.GetWarpedNoise(p * MtnRangeFreq  / PlanetRadius, 1);
            var mtnNoise = Mountains ? (1 - Math.Abs(Noise.Fbr(p * MtnFreq  / PlanetRadius, 1))) * MtnAmp * Math.Max(mtnRangeNoise, 0) * contNoise : 0;
            var ridgeNoise = Ridges ? (0.5 - Math.Abs(Noise.GetWarpedNoise(p * RidgeFreq  / PlanetRadius, 1))) * RidgeAmp : 0;
            var hillNoise = Hills ? -Math.Max(-mtnRangeNoise, 0) * Noise.Fbr(p * HillFreq  / PlanetRadius) * HillAmp * contNoise : 0;
            var elevation = contNoise + mtnNoise + hillNoise + ridgeNoise;
            
            vertices[i] += elevation * (vertices[i] - Pos).Normalized() * PlanetRadius;
            
            _surfaceMesh.Shader.SetFloat("oceanRadius", PlanetRadius);
        }
        _surfaceMesh.SetVertices(vertices);
    }
}