using OpenTK.Graphics.ES20;
using OpenTK.Mathematics;
using TGameToolkit.Drawing;

namespace TSpaceSim;

public static class Noise
{
    private static Random _random = new Random(0);
    private static int _tileSize = 8;
    private static Vector3[,,] _grid = new Vector3[_tileSize,_tileSize, _tileSize];
    
    static Noise()
    {
        for (int i = 0; i < _tileSize; i++)
        {
            for (int j = 0; j < _tileSize; j++)
            {
                for (int k = 0; k < _tileSize; k++)
                {
                    _grid[i, j, k] = new Vector3((_random.NextSingle() - 0.5f) * 2, (_random.NextSingle() - 0.5f) * 2, (_random.NextSingle() - 0.5f) * 2);
                }
            }
        }
    }

    private static float Interpolate(float a, float b, float w)
    {
        return (b - a) * w * w * w * (w * (6.0f * w - 15.0f) + 10.0f) + a;
    }

    public static float PerlinNoise(Vector3 p) => PerlinNoise(p.X, p.Y, p.Z);
    public static float PerlinNoise(float x, float y, float z)
    {
        x %= _tileSize;
        y %= _tileSize;
        z %= _tileSize;
        
        // Ensure number is positive with consistent wrapping
        x = x < 0 ? x + _tileSize : x; 
        y = y < 0 ? y + _tileSize : y; 
        z = z < 0 ? z + _tileSize : z; 
        
        x = x >= _tileSize ? x - 0.1f : x;
        y = y >= _tileSize ? y - 0.1f : y;
        z = z >= _tileSize ? z - 0.1f : z;
        
        int x0 = (int)MathF.Floor(x);
        int y0 = (int)MathF.Floor(y);
        int z0 = (int)MathF.Floor(z);
        int x1 = x0+1;
        int y1 = y0+1;
        int z1 = z0+1;
        
        var dx0 = x - x0;
        var dx1 = x - x1;
        
        var dy0 = y - y0;
        var dy1 = y - y1;
        
        var dz0 = z - z0;
        var dz1 = z - z1;

        x1 = x1 >= _tileSize ? 0 : x1;
        y1 = y1 >= _tileSize ? 0 : y1;
        z1 = z1 >= _tileSize ? 0 : z1;
        
        var v1 = _grid[x0, y0, z0];
        var v2 = _grid[x1, y0, z0];
        var v3 = _grid[x0, y1, z0];
        var v4 = _grid[x1, y1, z0];
        
        var v5 = _grid[x0, y0, z1];
        var v6 = _grid[x1, y0, z1];
        var v7 = _grid[x0, y1, z1];
        var v8 = _grid[x1, y1, z1];

        var dot1 = dx0 * v1.X + dy0 * v1.Y + dz0 * v1.Z;
        var dot2 = dx1 * v2.X + dy0 * v2.Y + dz0 * v2.Z;
        var dot3 = dx0 * v3.X + dy1 * v3.Y + dz0 * v3.Z;
        var dot4 = dx1 * v4.X + dy1 * v4.Y + dz0 * v4.Z;
        
        var dot5 = dx0 * v5.X + dy0 * v5.Y + dz1 * v5.Z;
        var dot6 = dx1 * v6.X + dy0 * v6.Y + dz1 * v6.Z;
        var dot7 = dx0 * v7.X + dy1 * v7.Y + dz1 * v7.Z;
        var dot8 = dx1 * v8.X + dy1 * v8.Y + dz1 * v8.Z;

        var ix0 = Interpolate(dot1, dot2, dx0);
        var ix1 = Interpolate(dot3, dot4, dx0);
        
        var ix2 = Interpolate(dot5, dot6, dx0);
        var ix3 = Interpolate(dot7, dot8, dx0);
        

        var iy0 = Interpolate(ix0, ix1, dy0);
        var iy1 = Interpolate(ix2, ix3, dy0);
        
        var result = Interpolate(iy0, iy1, dz0);
        
        // Scale to -1, 1 range
        return result / 0.866f;
    }

    public static float Fbr(Vector3 p, int octaves = 2)
    {
        var result = 0f;
        for (int o = 1; o <= octaves; o++)
        {
            result += PerlinNoise(p * o) / o;
        }
        return result;
    }

    public static float GetWarpedNoise(Vector3d pos, int noiseOctaves = 2)
    {
        var p = (Vector3)pos;
        
        var q = new Vector3(
            Fbr(p , noiseOctaves),
            Fbr(p + new Vector3(5.2f, 1.3f, 9.5f), noiseOctaves),
            Fbr(p + new Vector3(4.1f, 7.4f, 1.2f), noiseOctaves));
        var r = new Vector3(
            Fbr(p + q * 4 + new Vector3(1.7f, 9.2f, 6.8f), noiseOctaves),
            Fbr(p + q * 4 + new Vector3(8.3f, 2.8f, 4.1f), noiseOctaves),
            Fbr(p + q * 4 + new Vector3(7.8f, 3.4f, 0.1f), noiseOctaves));
        
        return Fbr(p + q * 4);
    }
    
    public static Texture GetNoiseTex(int tiles, int res)
    {
        var width = res;
        var height = res;
        float fWidth = width;
        float fHeight = height;
        
        byte[] data = new byte[width * height * 4];
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                float noiseVal = 0;
                noiseVal += 
                    1 - Math.Abs(PerlinNoise((j / fWidth * tiles * _tileSize - 4, i / fHeight * tiles * _tileSize - 4, 0)));
                byte noiseByte = (byte)(noiseVal * 255);
                var index = (i * width + j) * 4;
                data[index] = noiseByte;
                data[index+1] = noiseByte;
                data[index+2] = noiseByte;
                data[index+3] = 255;
            }
        }
    
        return new Texture(data, width, height);
    }

}