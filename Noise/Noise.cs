using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace Testing3D.Noise
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class NoiseConstruct : Microsoft.Xna.Framework.DrawableGameComponent
    {

        GraphicsDevice device;
        Random random;
        public Color[] color;
        public Texture2D texture;
        public Rectangle noiseRectangle; 
        public float[,] noise;


        int noiseWidth;
        int noiseHeight;


        public NoiseConstruct(Game game, int w, int h, Color noiseColor)
            : base(game)
        {
            device = (GraphicsDevice)game.Services.GetService(typeof(GraphicsDevice));

            random = new Random(1);

            InitNoise(w, h, noiseColor); 
            
            // TODO: Construct any child components here
        }
        public void InitNoise(int w, int h, Color noiseColor)
        {
            noiseWidth = w;
            noiseHeight = h;

            noiseRectangle = new Rectangle(0,0,w,h);
        }

        #region old perlin noice
        public float perlinNoise(int x)
        {
            x = (int)Math.Pow((x|13), x);
            float ran = (float)(1.0 - ((x * (x * x * 15731 + 789221) + 1386312589) & 72354523) / 1073741824.0);
            return ran;
        }

        public float SmoothPerlinNoise1D(float x)
        {
            float ran = perlinNoise((int)x) / 2 + perlinNoise((int)x - 1) / 4 + perlinNoise((int)x + 1) / 4;
            return ran;
        }
        public float Interpolate(float a, float b, float x)
        {
            float ft = x * (float)3.1415927;
            float f = (float)(1 - Math.Cos(ft)) * (float)0.5;
            
            float ran = a*(1-f) + b*f;
            return ran;
        }
        public float InterPolatedNoise(float x)
        {
            int integer_X = (int)x;
            float fractional_X = x - integer_X;

            float v1 = SmoothPerlinNoise1D(integer_X);
            float v2 = SmoothPerlinNoise1D(integer_X + 1);


            return Interpolate(v1, v2, fractional_X);
        }

        public void PerlinNiose(float x)
        {
            int persistence = 4;
            int octaves = 3;

            for (int i = 0; i < octaves; i++)
            {
                int frequency = (int)Math.Pow(2, i);
                float amplitude = (float)Math.Pow(persistence, i);
            }
        }
        #endregion

        public float[,] GenerateUniformNoise(int width, int height)
        {
            float[,] baseNoise = new float[width, height];
                
            for (int i=0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    baseNoise[i, j] = (float)random.NextDouble();
                }
            }
            //noise = baseNoise;
            return baseNoise;
        }
        public float[,] GenerateSmoothNoise(float[,] bNoise, int k)
        {
            int samplePeriod = 1 << k; //Calculates 2 ^ k
            float sampleFrequency = 1f / samplePeriod;
            float[,] smooth = new float[noiseWidth, noiseHeight];


            for (int i = 0; i < noiseWidth; i++)
            {
                //Calculate the horizontal sampling indices
                int sample_i0 = (i / samplePeriod) * samplePeriod;
                int sample_i1 = (sample_i0 + samplePeriod) % noiseWidth;
                float horizontal_blend = (i - sample_i0) * sampleFrequency;

                for (int j = 0; j < noiseHeight; j++)
                {
                    //Calculate the vertical sampling indices
                    int sample_j0 = (j / samplePeriod) * samplePeriod;
                    int sample_j1 = (sample_j0 + samplePeriod) % noiseHeight;
                    float vertical_blend = (j - sample_j0) * sampleFrequency;
                    //blend the top two corners
                    float top = CosineInterpolate(bNoise[sample_i0, sample_j0], bNoise[sample_i1, sample_j0], horizontal_blend);

                    float bottom = CosineInterpolate(bNoise[sample_i0, sample_j1], bNoise[sample_i1, sample_j1], horizontal_blend);
                    smooth[i, j] = CosineInterpolate(top, bottom, vertical_blend);
                }
            }
            return smooth;
        }
        struct Smooth
        {
            public float[,] octaveNoise;
        }
        public float[,] GeneratePerlin( int octaveCount, int width, int height)
        {
            float[,] bNoise = GenerateUniformNoise(width, height);
            Smooth[] smooth = new Smooth[octaveCount];
            float[,] perlinNoise = new float[width,height];

            float persistance = 0.54f;
            float amplitude = 1.0f;
            float totalAmplitude = 0.0f;

            //Generate smooth noise
            for (int i = 0; i < octaveCount; i++)
            {
                smooth[i].octaveNoise = GenerateSmoothNoise(bNoise, i);
            }

            
            //Blend noise together
            for (int k = octaveCount-1; k >= 0 ; k--)
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
        public void TextureApplicator()
        {
            color = new Color[noiseWidth * noiseHeight];
            for (int i = 0; i < noiseWidth; i++)
            {
                for (int j = 0; j < noiseHeight; j++)
                {
                    float r = 0.8f;
                    float g = 0.7f;
                    float b = 1f;
                    
                    if (noise[i, j] > 0.997)
                    {   
                        r = 1;
                        g = 1;
                        b = 1;
                    }
                    else if (noise[i, j] < 0.995 && noise[i, j] > 0.85)
                    {
                        r = noise[i, j];
                        g = noise[i, j] / (1 * 1.14f / noise[i, j]);
                        b = noise[i, j] / 1000;
                    }
                    else if (noise[i, j] <= 0.85 && noise[i, j] >= 0.82)
                    {
                        r = noise[i, j] += (1 - noise[i, j]) / 1.5f;
                        g = noise[i, j] / (1 * 2.0f / noise[i, j]);
                        b = noise[i, j] / 1000;
                    }

                    else if (noise[i, j] <= 0.82)
                    {
                        r = noise[i, j] += (1 - noise[i, j]) / 2f;
                        g = noise[i, j] / (1 * 4f / noise[i, j]);
                        b = noise[i, j] / 1000;
                        noise[i, j] /=  1.2f;
                    }
                    if (noise[i, j] > 0.78)
                    {
                        noise[i, j] += (1 - noise[i, j]) / 1.2f;
                    }
                    
 
                    color[i + j * noiseWidth] = Color.Multiply(new Color(r, g, b), noise[i, j]);
                }
            }
            texture = new Texture2D(device, noiseWidth, noiseHeight);
            texture.SetData<Color>(color);
        }

        public Texture2D TextureApp()
        {
            color = new Color[noiseWidth * noiseHeight];
            for (int i = 0; i < noiseWidth; i++)
            {
                for (int j = 0; j < noiseHeight; j++)
                {
                    float r = 0.8f;
                    float g = 0.7f;
                    float b = 1f;

                    if (noise[i, j] > 0.997)
                    {
                        r = 1;
                        g = 1;
                        b = 1;
                    }
                    else if (noise[i, j] < 0.995 && noise[i, j] > 0.85)
                    {
                        r = noise[i, j];
                        g = noise[i, j] / (1 * 1.14f / noise[i, j]);
                        b = noise[i, j] / 1000;
                    }
                    else if (noise[i, j] <= 0.85 && noise[i, j] >= 0.82)
                    {
                        r = noise[i, j] += (1 - noise[i, j]) / 1.5f;
                        g = noise[i, j] / (1 * 2.0f / noise[i, j]);
                        b = noise[i, j] / 1000;
                    }

                    else if (noise[i, j] <= 0.82)
                    {
                        r = noise[i, j] += (1 - noise[i, j]) / 2f;
                        g = noise[i, j] / (1 * 4f / noise[i, j]);
                        b = noise[i, j] / 1000;
                        noise[i, j] /= 1.2f;
                    }
                    if (noise[i, j] > 0.78)
                    {
                        noise[i, j] += (1 - noise[i, j]) / 1.2f;
                    }


                    color[i + j * noiseWidth] = Color.Multiply(new Color(r, g, b), noise[i, j]);
                }
            }
            ;
            texture = new Texture2D(device, noiseWidth, noiseHeight);
            texture.SetData<Color>(color);

            return texture;
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

        public override void Initialize()
        {
            // TODO: Add your initialization code here

            base.Initialize();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // TODO: Add your update code here

            base.Update(gameTime);
        }
    }
}
