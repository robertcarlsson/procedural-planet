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

using Testing3D.Terrain.Quadtree;
using Testing3D.Noise;


namespace Testing3D
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        GraphicsDevice device;
        SpriteBatch spriteBatch;
        RasterizerState wireFrameState;
        Raket raket;

        Camera gameCamera;

        KeyboardState newKeyState;
        KeyboardState oldKeyState;

        SampleGrid grid;

        TerrainFlat ground;

        Texture2D grassTexture;

        NoiseConstruct noise;

        Effect effect;

        TextCordinates textCoord;


        WaterI water;

        TexturePlane texPlane;

        Quadtree testTree;
        Quadtree waterTree;


        CelestialQuadtree Planet;

        PlanetNoise planetNoise;
        Random random;
        string noiseText;
        string fBmText;
        string multifBmText;

        int width = 128;
        int height = 128;

        bool isWireFrame = false;
        bool isGround = false;
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            graphics.PreferredBackBufferWidth = 1500;
            graphics.PreferredBackBufferHeight = 900;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();
            Window.Title = "CRUSE - Testing 3D";

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            wireFrameState = new RasterizerState()
            {
                FillMode = FillMode.WireFrame,
                CullMode = CullMode.CullClockwiseFace,
            };

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            device = GraphicsDevice;
            Services.AddService(typeof(SpriteBatch), spriteBatch);
            Services.AddService(typeof(GraphicsDevice), device);
            gameCamera = new Camera(this);

            oldKeyState = Keyboard.GetState();

            grid = new SampleGrid();
            grid.GridColor = Color.LimeGreen;
            grid.GridScale = 1.0f;
            grid.GridSize = 32;
            // Set the grid to draw on the x/z plane around the origin
            grid.WorldMatrix = Matrix.Identity;
            grid.ProjectionMatrix = gameCamera.projectionMatrix;
            grid.ViewMatrix = gameCamera.viewMatrix;

            grid.LoadGraphicsContent(device);


            

            ground = new TerrainFlat(this, gameCamera, 20, 20);
            isGround = true;
            Components.Add(ground);
            

            grassTexture = Content.Load<Texture2D>("grass");
            effect = Content.Load<Effect>("Series4Effects");


            int width = 10;
            int height = 10;
            /*
            noise = new Noise(this, width, height, Color.Black);

            float[,] heightData = noise.GeneratePerlin(8, width, height);

            VertexPositionNormalTexture[] vertices = TerrainUtils.CreateTerrainVertices(heightData);
            int[] indices = TerrainUtils.CreateTerrainIndices(width, height);
            vertices = TerrainUtils.GenerateNormalsForTriangleStrip(vertices, indices);
            VertexPositionNormalTexture[,] vertexArray = Reshape1Dto2D<VertexPositionNormalTexture>(vertices, width, height);

            rootNode = new QTNode(vertexArray, device, grassTexture, 32, effect);
            */
            width = 1;
            height = 1;
            Vector2[] corners = new Vector2[4];
            corners[0] = new Vector2(0, 0);
            corners[1] = new Vector2(width, 0);
            corners[2] = new Vector2(0, height);
            corners[3] = new Vector2(width, height);


            //LoadWater();
            

           
            // TODO: use this.Content to load your game content here


            raket = new Raket(this, gameCamera);
            //Components.Add(raket);

            textCoord = new TextCordinates(this, gameCamera);

            //LoadCubetree();
            //LoadWatertree();
            //LoadQuadtree();

            LoadPlanet();
            LoadPlanetNoise();

            texPlane = new TexturePlane(this, gameCamera, grassTexture);
            //Components.Add(texPlane);

            Components.Add(textCoord);

            
        }
        private void LoadPlanet()
        {
            Planet = new CelestialQuadtree(this, gameCamera, textCoord, 1, 10, 17);
            Components.Add(Planet);
        }
        private void LoadWater()
        {
            water = new WaterI(this, "Textures/cubeMap", gameCamera);
            water.SetDefault();
            Components.Add(water);
        }
        private void LoadQuadtree()
        {
            testTree = new Quadtree(this, gameCamera, textCoord);
            testTree.SetUpXZNode(this, gameCamera);
            Components.Add(testTree);
        }
        private void LoadCubetree()
        {
            testTree = new Quadtree(this, gameCamera, textCoord);
            testTree.SetUpCubeNodes(this, gameCamera);
            Components.Add(testTree);
        }
        private void LoadPlanetNoise()
        {
            planetNoise = new PlanetNoise(8, 1);
            random = new Random();

            noiseText = "";
            fBmText = "";
            multifBmText = "";
        }
        private T[,] Reshape1Dto2D<T>(T[] vertices, int width, int height)
        {
            T[,] vertexArray = new T[width, height];
            int i = 0;
            for (int h = 0; h < height; h++)
                for (int w = 0; w < width; w++)
                    vertexArray[w, h] = vertices[i++];
            return vertexArray;
        }
        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        bool turnDisable = true;
        protected void Input()
        {
            newKeyState = Keyboard.GetState();

            if (newKeyState.IsKeyDown(Keys.Escape))
            {
                Exit();
            }
            if (newKeyState.IsKeyDown(Keys.G) && oldKeyState.IsKeyUp(Keys.G) && isGround)
            {
                ground.GenerateTerrain(7, 20, 20);
            }
            if (newKeyState.IsKeyDown(Keys.H) && oldKeyState.IsKeyUp(Keys.H) && isGround)
            {
                if (turnDisable)
                {
                    ground.Disable();
                    turnDisable = false;
                }
                else
                {
                    ground.Active();
                    turnDisable = true;
                }
            }
            if (newKeyState.IsKeyDown(Keys.C))
            {
                isWireFrame = true;
            }
            if (newKeyState.IsKeyDown(Keys.V))
            {
                isWireFrame = false;
            }

            if (newKeyState.IsKeyDown(Keys.N) && oldKeyState.IsKeyUp(Keys.N))
            {
                float x = (float)random.NextDouble();
                float y = (float)random.NextDouble();
                float z = (float)random.NextDouble();
                Vector3 NoiseVector = new Vector3(x, y, z);

                noiseText = "Noise : " + planetNoise.fractal.Noise3(NoiseVector).ToString();

                fBmText = "fBm : " + planetNoise.fractal.fBm(NoiseVector, 0.5f, 2f, 10f);

                multifBmText = "MultifBM : " + planetNoise.fractal.MultifBm(NoiseVector, 0.5f, 2f, 10f, 0.2f) * 10000000000000;
            }

            if (newKeyState.IsKeyDown(Keys.M) && oldKeyState.IsKeyUp(Keys.M))
            {
                texPlane.RandomTexture();
            }

            oldKeyState = newKeyState;
        }
            


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            Input();
            float timeDifference = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 2000.0f;
            gameCamera.ProcessInput(timeDifference);
            // TODO: Add your update logic here
            grid.ViewMatrix = gameCamera.viewMatrix;


            //rtNode.Update();
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            RasterizerState rs = new RasterizerState();
            if (isWireFrame)
            {
                rs = wireFrameState;
            }
            else
            {
                rs.FillMode = FillMode.Solid;
                rs.CullMode = CullMode.CullClockwiseFace;
            }

            device.RasterizerState = rs;

            grid.Draw();
            // TODO: Add your drawing code here

            /*
            QTNode.NodesRendered = 0;
            Matrix.CreateTranslation(-(width / 2), 2, -(height / 2));
            BoundingFrustum cameraFrustum = new BoundingFrustum(gameCamera.viewMatrix * gameCamera.projectionMatrix);
            rootNode.Draw(Matrix.Identity, gameCamera.viewMatrix, gameCamera.projectionMatrix, cameraFrustum);
            Window.Title = string.Format("{0} nodes rendered", QTNode.NodesRendered);
            */

            //rtNode.Draw(Matrix.Identity, gameCamera.viewMatrix, gameCamera.projectionMatrix);
            //testNode.Draw();


            base.Draw(gameTime);
            textCoord.DrawString(noiseText, Color.Black);
            textCoord.DrawString(fBmText, Color.Black);
            textCoord.DrawString(multifBmText, Color.Black);
        }
    }
}
