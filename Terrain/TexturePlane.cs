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

using Testing3D.Noise;

namespace Testing3D
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class TexturePlane : Microsoft.Xna.Framework.DrawableGameComponent
    {
        PlanetNoise planetNoise;
        GraphicsDevice device;
        Camera camera;
        VertexBuffer vertexBuffer;
        IndexBuffer indexBuffer;

        Vector3[] vertexPositions;


        Texture2D texture;
        BasicEffect effect;
        Random random;
        int width;
        int height;

        int textureWidth = 128;
        int textureHeight = 128;


        public TexturePlane(Game game, Camera cam, Texture2D texture)
            : base(game)
        {
            device = (GraphicsDevice)game.Services.GetService(typeof(GraphicsDevice));
            camera = cam;
            this.texture = texture;

            width = 10;
            height = 10;
            effect = new BasicEffect(device);

            planetNoise = new PlanetNoise(12, 1);
            random = new Random();

            SetUpPlane();
            // TODO: Construct any child components here
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here

            base.Initialize();
        }
        public void RandomTexture()
        {
            //textureWidth = textureWidth << 1;
            //textureHeight = textureHeight << 1;

            vertexPositions = new Vector3[textureWidth * textureHeight];
            

            float halfW = textureWidth / 2;
            float halfH = textureHeight / 2;
            float size = 2f;
            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                { //(float)random.NextDouble()
                    vertexPositions[x + y * textureWidth] = new Vector3(x + 0.2f, (float)random.NextDouble(), y + 0.4f);
                }
            }

            Color[] colorTexture = new Color[textureWidth * textureHeight];
            int index = 0;


            
            foreach (Vector3 v in vertexPositions)
            {
                //float noise = planetNoise.Noise(v);
                float noise = planetNoise.fractal.fBm(v, 0.9f, 2f, 10f);
                //noise = (noise + 1) / 2;
                //noise += planetNoise.MultifBm(v, 0.5f, 2f, 10f, 0.3f) * 10000000000;
                //noise += planetNoise.HeteroTerrain(v, 0.7f, 2f, 10f, 0.2f);

                //float noise2 = planetNoise.MultifBm(v, 0.5f, 2f, 10f, 0.3f) * 100000000000;

                //float noiseR = planetNoise.fBm(v, 0.6f, 2f, 5f);
                //float noiseG = planetNoise.fBm(v, 0.4f, 2f, 10f);
                //float noiseB = planetNoise.fBm(v, 0.5f, 2f, 12f);
                noise = (noise + 2f) / 4;
                float noiseR = noise + 0.1f;
                float noiseG = noise + 0.1f;
                float noiseB = noise + 0.1f;


                // betong ((noise + noise2 +2) / 4)

                colorTexture[index] = new Color(noiseR, noiseG, noiseB);
                //colorTexture[index] = new Color(new Vector3(noise));
                index++;
            }
            


            texture = new Texture2D(device, textureWidth, textureHeight);
            texture.SetData(colorTexture);
        }
        private void SetUpPlane()
        {
            VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[width * height];

            int halfW = width / 2;
            int halfH = height / 2;
            float size = 2f;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    vertices[x + y * width].Position = new Vector3((x - halfW) * size, 0.5f, (halfH - y) * size);
                    vertices[x + y * width].TextureCoordinate.X = (float)x / (width );
                    vertices[x + y * width].TextureCoordinate.Y = (float)y / (height );
                }
            }

            int[] indices = new int[(width - 1) * (height - 1) * 6];
            int counter = 0;
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    //This is for making two triangels out of a four courners
                    int lowerLeft = x + y * width;
                    int lowerRight = (x + 1) + y * width;
                    int topLeft = x + (y + 1) * width;
                    int topRight = (x + 1) + (y + 1) * width;

                    //Always make it clockwise, since computers cut away the rest with culling
                    indices[counter++] = topLeft;
                    indices[counter++] = lowerLeft;
                    indices[counter++] = lowerRight;

                    indices[counter++] = topLeft;
                    indices[counter++] = lowerRight;
                    indices[counter++] = topRight;
                }
            }

            vertexBuffer = new VertexBuffer(device, VertexPositionNormalTexture.VertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices);

            indexBuffer = new IndexBuffer(device, typeof(int), indices.Length, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);
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
        public override void Draw(GameTime gameTime)
        {
            effect.World = Matrix.Identity;
            effect.View = camera.viewMatrix;
            effect.Projection = camera.projectionMatrix;

            
            effect.Texture = texture;
            effect.TextureEnabled = true;
            effect.EnableDefaultLighting();
            effect.AmbientLightColor = new Vector3(0.7f, 0.7f, 0.7f);

            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.Default;
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                device.Indices = indexBuffer;
                device.SetVertexBuffer(vertexBuffer);
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertexBuffer.VertexCount, 0, indexBuffer.IndexCount / 3);

            }

            base.Draw(gameTime);
        }
    }
}
