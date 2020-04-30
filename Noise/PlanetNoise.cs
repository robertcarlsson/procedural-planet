using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace Testing3D.Noise
{
    public class PlanetNoise
    {
        #region Fields
        Random random;
        NoiseConstruct randomNoise;
        public Fractal fractal;

        bool first = true;
        float[] exponentArray;

        List<int> numberMap = new List<int>();
        float[,] floatBuffer;
        int[] perm;
        int tabSize;
        int tabMask;

        const int MaxDimensions = 4;

        int dimensions = 3;

        struct Smooth
        {
            public float[,] octaveNoise;
        }
        #endregion

        #region Properties
        public int SeedRandom
        {
            set { random = new Random(value); }
        }
        public int Dimensions
        {
            get { return dimensions; }
            set { dimensions = value; }
        }

        #endregion

        #region Constructor
        public PlanetNoise(int power, int seed)
        {
            fractal = new Fractal(power, seed);
            fractal.Dimensions = 3;
        }
        #endregion

        #region Methods
        
        public void SphereMapping(ref Vector3 position)
        {
            float x = position.X;
            float y = position.Y;
            float z = position.Z;

            position.X = (float)(x * Math.Sqrt(1 - (y * y / 2) - (z * z / 2) + (y * y * z * z / 3)));
            position.Y = (float)(y * Math.Sqrt(1 - (z * z / 2) - (x * x / 2) + (x * x * z * z / 3)));
            position.Z = (float)(z * Math.Sqrt(1 - (y * y / 2) - (x * x / 2) + (y * y * x * x / 3)));
        }

        public float[,] GenerateUniformNoise(int width, int height)
        {
            float[,] baseNoise = new float[width, height];

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    baseNoise[i, j] = (float)random.NextDouble();
                }
            }
            //noise = baseNoise;
            return baseNoise;
        }
        public float[,] GenerateSmoothNoise(float[,] bNoise, int k, int width, int height)
        {
            int samplePeriod = 1 << k; //Calculates 2 ^ k
            float sampleFrequency = 1f / samplePeriod;
            float[,] smooth = new float[width, height];


            for (int i = 0; i < width; i++)
            {
                //Calculate the horizontal sampling indices
                int sample_i0 = (i / samplePeriod) * samplePeriod;
                int sample_i1 = (sample_i0 + samplePeriod) % width;
                float horizontal_blend = (i - sample_i0) * sampleFrequency;

                for (int j = 0; j < height; j++)
                {
                    //Calculate the vertical sampling indices
                    int sample_j0 = (j / samplePeriod) * samplePeriod;
                    int sample_j1 = (sample_j0 + samplePeriod) % height;
                    float vertical_blend = (j - sample_j0) * sampleFrequency;
                    //blend the top two corners
                    float top = CosineInterpolate(bNoise[sample_i0, sample_j0], bNoise[sample_i1, sample_j0], horizontal_blend);

                    float bottom = CosineInterpolate(bNoise[sample_i0, sample_j1], bNoise[sample_i1, sample_j1], horizontal_blend);
                    smooth[i, j] = CosineInterpolate(top, bottom, vertical_blend);
                }
            }
            return smooth;
        }      
        public float[,] GeneratePerlin(int octaveCount, int width, int height)
        {
            float[,] bNoise = GenerateUniformNoise(width,height);
            Smooth[] smooth = new Smooth[octaveCount];
            float[,] perlinNoise = new float[width, height];

            float persistance = 0.5f;
            float amplitude = 1.0f;
            float totalAmplitude = 0.0f;

            //Generate smooth noise
            for (int i = 0; i < octaveCount; i++)
            {
                smooth[i].octaveNoise = GenerateSmoothNoise(bNoise, i, width, height);
            }


            //Blend noise together
            for (int k = octaveCount - 1; k >= 0; k--)
            {
                amplitude *= persistance;
                totalAmplitude += amplitude;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        perlinNoise[x, y] += smooth[k].octaveNoise[x, y] * amplitude;
                    }
                }
            }

            //Normalization
            for (int n = 0; n < octaveCount; n++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        //perlinNoise[width - 1, height - 1] /= totalAmplitude;
                    }
                }

            }

            return perlinNoise;
        }
        public float[,] GeneratePerlin(int octaveCount, int width, int height, float[,] noise)
        {
            float[,] bNoise = noise;
            Smooth[] smooth = new Smooth[octaveCount];
            float[,] perlinNoise = new float[width, height];

            float persistance = 0.5f;
            float amplitude = 1.0f;
            float totalAmplitude = 0.0f;

            //Generate smooth noise
            for (int i = 0; i < octaveCount; i++)
            {
                smooth[i].octaveNoise = GenerateSmoothNoise(bNoise, i, width, height);
            }


            //Blend noise together
            for (int k = octaveCount - 1; k >= 0; k--)
            {
                amplitude *= persistance;
                totalAmplitude += amplitude;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        perlinNoise[x, y] += smooth[k].octaveNoise[x, y] * amplitude;
                    }
                }
            }

            //Normalization
            for (int n = 0; n < octaveCount; n++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        //perlinNoise[width - 1, height - 1] /= totalAmplitude;
                    }
                }

            }

            return perlinNoise;
        }
        public float LinearInterpolate(float a, float b, float alpha)
        {
            return ((1 - alpha) * a + alpha * b);
        }
        public float CosineInterpolate(float a, float b, float alpha)
        {
            float ft = (float)Math.PI * alpha;
            float f = (float)(1 - Math.Cos(ft)) * 0.5f;

            return a * (1 - f) + b * f;
        }
        public float CubicInterpolate(float v0, float v1, float v2, float v3, float x)
        {
            float P = (v3 - v2) - (v0 - v1);
            float Q = (v0 - v1) - P;
            float R = v2 - v0;
            float S = v1;

            return ((P * x * x * x) + (Q * x * x) + (R * x) + S);
        }

        #endregion
    }
}
