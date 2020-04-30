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

    public struct BlendInfo
    {
        public FractalInfo fBm;
        public FractalInfo Turbulence;
        public FractalInfo Perturbing;
        public FractalInfo MultifBm;
        public FractalInfo HeteroTerrain;
        public FractalInfo RidgedMF;
        public float lacunarity;
        public int MaxOctaves;
        public BlendInfo(float lacun, int octaves)
        {
            lacunarity = 2.0f;
            MaxOctaves = octaves;
            fBm = new FractalInfo(false);
            Turbulence = new FractalInfo(false);
            Perturbing = new FractalInfo(false);
            MultifBm = new FractalInfo(false);
            HeteroTerrain = new FractalInfo(false);
            RidgedMF = new FractalInfo(false);
        }
    }
    public struct FractalInfo
    {
        public bool inUse;
        public int octaves;
        public float gain;
        public float offset;
        public float amplitude;
        public float frequency;
        //Sign makes it possible for the different fractals to add or subtract to the value
        public float signLevel;
      
        public FractalInfo(bool use)
        {
            inUse = use;
            octaves = 8;
            gain = 0.5f;
            offset = 0.1f;
            amplitude = 0.5f;
            frequency = 1.0f;
            //Sign makes it possible for the different fractals to add or subtract to the value
            signLevel = 1.0f;
        }
    }



    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class Fractal
    {
        #region Fields

        Random random;

        bool first = true;
        float[] exponentArray;

        bool blendFirst = true;
        float[] blendExponentArray;

        List<int> numberMap = new List<int>();
        float[,] floatBuffer;
        int[] perm;
        int tabSize;
        int tabMask;

        const int MaxDimensions = 4;
        int dimensions = 1;
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
        public Fractal(int power, int seed)
        {
            SetUpLattice(power, seed);

        }
        #endregion


        #region Methods

        public void SetUpLattice(int power, int seed)
        {
            // Seed makes u control the random function
            random = new Random(seed);

            // tabSize becomes a number of the 2^n series making the  (integer & tabMask) = (integer % tabMask)
            tabSize = 1 << power;
            tabMask = tabSize - 1;

            perm = new int[tabSize];
            floatBuffer = new float[tabSize, MaxDimensions];

            int i;
            for (i = 0; i < tabSize; i++)
            {
                perm[i] = i;

                for (int j = 0; j < MaxDimensions; j++)
                {
                    floatBuffer[i, j] = (float)((random.NextDouble() - 0.5f)); // NextDouble() gives 0.0 to 1.0 : Minus 0.5 gives -0.5 to 0.5
                }
                Normalize3(ref floatBuffer, i);
            }
            i--;
            for (; i >= 0; i--)
            {
                int j = random.Next(0, tabMask);
                int temp = perm[i];
                perm[i] = perm[j];
                perm[j] = temp;
            }

        }
        private void Normalize3(ref float[,] random, int i)
        {
            float s;

            s = (float)Math.Sqrt(random[i, 0] * random[i, 0] + random[i, 1] * random[i, 1] + random[i, 2] * random[i, 2]);

            s = 1.0f / s;

            random[i, 0] *= s;
            random[i, 1] *= s;
            random[i, 2] *= s;
        }
        public float Lattice(int ix, float fx, int iy = 0, float fy = 0, int iz = 0, float fz = 0, int iw = 0, float fw = 0)
        {
            int[] Numbers = new int[4] { ix, iy, iz, iw };
            float[] Floats = new float[4] { fx, fy, fz, fw };

            int i;
            int nIndex = 0;

            // This for loops through the perm[] array and gets 
            for (i = 0; i < dimensions; i++)
                nIndex = perm[(nIndex + Numbers[i]) & tabMask];

            float fValue = 0;


            for (i = 0; i < dimensions; i++)
                fValue += floatBuffer[nIndex, i] * Floats[i];


            // floatBuffer[,] has values from -1 to 1, 
            // we give fValue a randomnumber and multiply a float remainder 0.0 to 0.9999999,
            // We do this "dimensions" number of times so divide by dimensions also
            //fValue /= dimensions;
            return fValue;
        }
        public float Noise3(Vector3 position)
        {
            // This is the perlins Noise3 function. This method uses the corners of a INTEGER cube (Corners have integer values)
            // were the vector3 point is inside, using those values to Lerp (linear interpolation) values for making a smooth noise
            // This smoothness is esential for fractals, withouth it the noise function would look like whitenoise
            // making things just look chaotic.
            int i;
            float[] Floats = new float[3];
            Floats[0] = position.X;
            Floats[1] = position.Y;
            Floats[2] = position.Z;

            int[] I = new int[3]; //Indexes
            float[] R = new float[3]; //Remainders
            float[] C = new float[3]; // Cubics

            for (i = 0; i < dimensions; i++)
            {
                I[i] = (int)Math.Floor(Floats[i]);
                R[i] = Floats[i] - (float)Math.Floor(Floats[i]);
                C[i] = (float)Math.Pow(R[i], 3);
            }


            float fValue;

            //Some fancy Linear Interpolation action.... LERP-ING!!!!
            //This is the lerping method that creates a pseudo-random value depending on the "cube" that the vertex is inside
            //This is the bottleneck of the program, as I have come to believe
            //Since all the fractals usually have many octaves making the Noise3() call it would surely be best
            //to fit this call together with other fractals, these fractal would have to share the same lacunarity
            //wich wouldnt be so much of a problem (I usually dont change it) becouse we can still change the 
            // "H" , octaves, amplitude, 
            fValue = MathHelper.Lerp(MathHelper.Lerp(MathHelper.Lerp(Lattice(I[0], R[0], I[1], R[1], I[2], R[2]),
                                                                     Lattice(I[0] + 1, R[0] - 1, I[1], R[1], I[2], R[2]),
                                                                     C[0])
                                                     ,
                                                     MathHelper.Lerp(Lattice(I[0], R[0], I[1] + 1, R[1] - 1, I[2], R[2]),
                                                                     Lattice(I[0] + 1, R[0] - 1, I[1] + 1, R[1] - 1, I[2], R[2]),
                                                                     C[0])
                                                     , C[1])
                                    ,
                                    MathHelper.Lerp(MathHelper.Lerp(Lattice(I[0], R[0], I[1], R[1], I[2] + 1, R[2] - 1),
                                                                     Lattice(I[0] + 1, R[0] - 1, I[1], R[1], I[2] + 1, R[2] - 1),
                                                                     C[0])
                                                     ,
                                                     MathHelper.Lerp(Lattice(I[0], R[0], I[1] + 1, R[1] - 1, I[2] + 1, R[2] - 1),
                                                                     Lattice(I[0] + 1, R[0] - 1, I[1] + 1, R[1] - 1, I[2] + 1, R[2] - 1),
                                                                     C[0])
                                                     , C[1])
                                    , C[2]);

            fValue *= 2;
            return MathHelper.Clamp(fValue, -1f, 1f);
        }
        public float ridge(float h, float offset)
        {
            h = Math.Abs(h);
            h = offset - h;
            h = h * h;
            return h;
        }

        

        public float BlendFractal(Vector3 point, BlendInfo info)
        {
            // BlendFractal using the same 
            float value = 0.0f;
            float multifBmValue = 1.0f;
            float fBmvalue;
            float turbValue;
            float perValue;
            float heteroValue;
            float ridgedMFValue;
            float previusValue = 1.0f;
            float ridgedFreq = info.RidgedMF.frequency;
            float ridgedAmp = info.RidgedMF.amplitude;


            float pointValue;
            float X = point.X;
            #region HeteroTerrain
            if (blendFirst && info.HeteroTerrain.inUse)
            {
                blendExponentArray = new float[info.HeteroTerrain.octaves + 1];
                for (int i = 0; i <= info.HeteroTerrain.octaves; i++)
                {
                    blendExponentArray[i] = (float)Math.Pow(info.HeteroTerrain.frequency, -info.HeteroTerrain.amplitude);
                    info.HeteroTerrain.frequency *= info.lacunarity;
                }
                blendFirst = false;
            }

            if (info.HeteroTerrain.inUse)
            {
                value += info.HeteroTerrain.offset + Noise3(point);
            }
            #endregion

            for (int i = 0; i < info.MaxOctaves; i++)
            {
                pointValue = Noise3(point);

                if (info.fBm.inUse && i < info.fBm.octaves)
                {
                    fBmvalue = pointValue * (float)Math.Pow(info.lacunarity, -info.fBm.amplitude * i);
                    value += fBmvalue * info.fBm.signLevel;
                }

                if (info.Turbulence.inUse && i < info.Turbulence.octaves)
                {
                    turbValue = (float)(Math.Abs(pointValue) * Math.Pow(info.lacunarity, -info.Turbulence.amplitude * i));
                    value += turbValue * info.Turbulence.signLevel;
                }

                if (info.Perturbing.inUse && i < info.Perturbing.octaves)
                {
                    perValue = (float)(Math.Abs(pointValue) * Math.Pow(info.lacunarity, -info.Perturbing.amplitude * i));
                    value += perValue * info.Perturbing.amplitude;
                }

                if (info.MultifBm.inUse && i < info.MultifBm.octaves)
                {
                    multifBmValue *= (float)((pointValue + info.MultifBm.offset) * Math.Pow(info.lacunarity, -info.MultifBm.amplitude * i));
                }

                if (info.HeteroTerrain.inUse && i < info.HeteroTerrain.octaves)
                {
                    heteroValue = (pointValue + info.HeteroTerrain.offset) * blendExponentArray[i] * value;
                    value += heteroValue * info.HeteroTerrain.signLevel;
                }

                if (info.RidgedMF.inUse && i < info.RidgedMF.octaves)
                {
                    ridgedMFValue = ridge(pointValue * ridgedFreq, info.RidgedMF.offset);
                    value += ridgedMFValue * info.RidgedMF.signLevel * info.RidgedMF.amplitude * previusValue;
                    previusValue = ridgedMFValue;
                    ridgedFreq *= info.lacunarity;
                    ridgedAmp *= info.RidgedMF.gain;
                }

                point *= info.lacunarity;
            }


            if (info.MultifBm.inUse)
                value += multifBmValue;
            if (info.Perturbing.inUse)
                return (float)Math.Sin(X + value);

            return value;
        }
        public float fBm(Vector3 point, float H, float lacunarity, float octaves)
        {
            float value = 0.0f;
            //float remainder;

            for (int i = 0; i < octaves; i++)
            {
                value += Noise3(point) * (float)Math.Pow(lacunarity, -H * i);
                point *= lacunarity;
            }
            return value;
        }
        public float Turbulence(Vector3 point, float H, float lacunarity, float octaves)
        {
            float value = 0.0f;
            float Abs;
            for (int i = 0; i < octaves; i++)
            {
                Abs = (float)Math.Abs(Noise3(point)) * (float)Math.Pow(lacunarity, -H * i);
                value += Abs;
                point *= lacunarity;
            }
            return value;
        }
        public float Perturbing(Vector3 point, float X, float H, float lacunarity, float octaves)
        {
            float value = 0.0f;
            float Abs;
            for (int i = 0; i < octaves; i++)
            {
                Abs = (float)Math.Abs(Noise3(point)) * (float)Math.Pow(lacunarity, -H * i);
                value += Abs;
                point *= lacunarity;
            }

            return (float)Math.Sin(X + value);
        }    
        public float MultifBm(Vector3 point, float H, float lacunarity, float octaves, float offset)
        {
            float value = 1.0f;
            //float remainder;

            for (int i = 0; i < octaves; i++)
            {
                value *= (Noise3(point) + offset) * (float)Math.Pow(lacunarity, -H * i);
                point *= lacunarity;
            }


            return value;
        }
        public float HeteroTerrain(Vector3 point, float H, float lacunarity, float octaves, float offset)
        {
            float value;
            float increment;
            float frequency = 1.0f;
            float remainder;
            int i;
            if (first)
            {
                exponentArray = new float[(int)octaves + 1];
                for (i = 0; i <= octaves; i++)
                {
                    exponentArray[i] = (float)Math.Pow(frequency, -H);
                    frequency *= lacunarity;
                }
                first = false;
            }


            value = offset + Noise3(point);
            point *= lacunarity;

            for (i = 1; i < octaves; i++)
            {
                increment = (Noise3(point) + offset) * exponentArray[i] * value; ;

                value += increment;

                point *= lacunarity;
            }

            remainder = octaves - (int)octaves;

            if (remainder != 0)
            {
                increment = (Noise3(point) + offset) * exponentArray[i];
                value += remainder * increment * value;
            }

            if (value < 1 && value > -1)
                value /= 5;
            return value;
        }
        public float RidgedMF(Vector3 point, float H, float gain, float lacunarity, float octaves, float offset, float freq = 1.0f)
        {
            float sum = 0;
            float amplitude = H;
            float frequency = freq;
            float prev = 1.0f;

            for (int i = 0; i < octaves; i++)
            {
                float n = ridge(Noise3(point) * frequency, offset);
                sum += n * amplitude * prev;
                prev = n;
                frequency *= lacunarity;
                amplitude *= gain;
            }

            return sum;
        }       
        public float KrusFractal(Vector3 point, float H, float gain, float lacunarity, float octaves, float offset)
        {
            float value = 1.0f;
            //float remainder;

            for (int i = 0; i < octaves; i++)
            {
                value *= Noise3(point);
                point *= lacunarity;
            }

            return value;
        }

        #endregion
    }
}
