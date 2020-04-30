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
    public class TerrainFlat : Microsoft.Xna.Framework.DrawableGameComponent
    {
        PlanetNoise terrainNoise;
        GraphicsDevice device;
        VertexPositionNormalTexture[] vertices;
        float[,] HeightMap;
        Texture2D Texture;
        Camera camera;

        Effect effect;

        int terrainWidth;
        int terrainHeight;
        int[] indices;

        Matrix localMatrix;
        IndexBuffer indexBuffer;
        VertexBuffer vertexBuffer;

        bool isVisable = true;
        public NoiseConstruct noise;

        public TerrainFlat(Game game, Camera cam, int Width, int Height)
            : base(game)
        {
            device = (GraphicsDevice)game.Services.GetService(typeof(GraphicsDevice));
            localMatrix = Matrix.Identity;
            camera = cam;

            terrainWidth = Width;
            terrainHeight = Height;
            //HeightMap = new float[Width, Height];
            noise = new NoiseConstruct(game, Width, Height, Color.White);

            //HeightMap = noise.GeneratePerlin(8, Width, Height);

            Texture = game.Content.Load<Texture2D>("grass");
            effect = game.Content.Load<Effect>("Series4Effects");

            terrainNoise = new PlanetNoise(12, 1);
            //VertexApplicatorXZ();
            //SetUpIndices();
            //CopyToBuffers();
            
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

        public void GenerateTerrain(int octaves, int width, int height)
        {
            terrainWidth = width;
            terrainHeight = height;
            noise.InitNoise(terrainWidth, terrainHeight, Color.White);
            HeightMap = noise.GeneratePerlin(octaves, terrainWidth, terrainHeight);
            VertexApplicatorXZ();
            
            //VertexApplicatorXY();
            SetUpIndices();
            CopyToBuffers();
        }
        public void VertexApplicatorXZ()
        {
            vertices = new VertexPositionNormalTexture[terrainWidth * terrainHeight];
            float halfW = terrainWidth / 2;
            float halfH = terrainHeight / 2;
            float size = 5f;
            for (int z = 0; z < terrainHeight; z++)
            {
                for (int x = 0; x < terrainWidth; x++)
                {                                                                           //(HeightMap[x,z] -1)
                    vertices[x + z * terrainWidth].Position = new Vector3((x - halfW) / terrainWidth * size * 2, 0.1f, (halfH - z) / terrainHeight * size);
                    float height = terrainNoise.fractal.fBm(vertices[x + z * terrainWidth].Position, 0.5f, 2.0f, 13f) / 30;
                    //height += terrainNoise.MultifBm(vertices[x + z * terrainWidth].Position, 0.5f, 2.0f, 13f, 0.4f) ;
                    vertices[x + z * terrainWidth].Position.Y = height;
                    vertices[x + z * terrainWidth].TextureCoordinate.X = (float)x / 30.0f;
                    vertices[x + z * terrainWidth].TextureCoordinate.Y = (float)z / 30.0f;
                }
            } 
         
        }
        public void Disable()
        {
            isVisable = false;
        }
        public void Active()
        {
            isVisable = true;
        }
        public void VertexApplicatorXY()    
        {
            vertices = new VertexPositionNormalTexture[terrainWidth * terrainHeight];
            int halfW = terrainWidth / 2;
            int halfH = terrainHeight / 2;
            float size = 0.2f;
            for (int x = 0; x < terrainWidth; x++)
            {
                for (int y = 0; y < terrainHeight; y++)
                {
                    vertices[x + y * terrainWidth].Position = new Vector3((x - halfW) * size, (halfH - y) * size, (HeightMap[x, y] * 5));
                    vertices[x + y * terrainWidth].TextureCoordinate.X = (float)x / 30.0f;
                    vertices[x + y * terrainWidth].TextureCoordinate.Y = (float)y / 30.0f;
                }
            }
        }
        private void SetUpIndices()
        {
            indices = new int[(terrainWidth - 1) * (terrainHeight - 1) * 6];
            int counter = 0;
            for (int y = 0; y < terrainHeight - 1; y++)
            {
                for (int x = 0; x < terrainWidth - 1; x++)
                {
                    //This is for making two triangels out of a four courners
                    int lowerLeft = x + y * terrainWidth;
                    int lowerRight = (x + 1) + y * terrainWidth;
                    int topLeft = x + (y + 1) * terrainWidth;
                    int topRight = (x + 1) + (y + 1) * terrainWidth;

                    //Always make it clockwise, since computers cut away the rest with culling
                    indices[counter++] = topLeft;
                    indices[counter++] = lowerLeft;
                    indices[counter++] = lowerRight;

                    indices[counter++] = topLeft;
                    indices[counter++] = lowerRight;
                    indices[counter++] = topRight;
                }
            }
        }
        private void CalculateNormals()
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Normal = new Vector3(0, 0, 0);

            for (int i = 0; i < indices.Length / 3; i++)
            {
                int index1 = indices[i * 3];
                int index2 = indices[i * 3 + 1];
                int index3 = indices[i * 3 + 2];

                Vector3 side1 = vertices[index1].Position - vertices[index3].Position;
                Vector3 side2 = vertices[index1].Position - vertices[index2].Position;
                Vector3 normal = Vector3.Cross(side1, side2);

                vertices[index1].Normal += normal;
                vertices[index2].Normal += normal;
                vertices[index3].Normal += normal;
            }

            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Normal.Normalize();
        }
        public void CopyToBuffers()
        {
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
            if (vertexBuffer != null && isVisable)
            {
                effect.CurrentTechnique = effect.Techniques["Textured"];
                effect.Parameters["xTexture"].SetValue(Texture);

                effect.Parameters["xWorld"].SetValue(localMatrix);
                effect.Parameters["xView"].SetValue(camera.viewMatrix);
                effect.Parameters["xProjection"].SetValue(camera.projectionMatrix);

                effect.Parameters["xEnableLighting"].SetValue(true);
                effect.Parameters["xAmbient"].SetValue(0.4f);
                effect.Parameters["xLightDirection"].SetValue(new Vector3(-0.5f, -1, -0.5f));



                device.BlendState = BlendState.AlphaBlend;
                device.DepthStencilState = DepthStencilState.Default;
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    device.Indices = indexBuffer;
                    device.SetVertexBuffer(vertexBuffer);
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices.Length, 0, indices.Length / 3);

                }
            }
            base.Draw(gameTime);
        }



    }
}
