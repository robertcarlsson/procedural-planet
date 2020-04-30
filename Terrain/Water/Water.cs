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


namespace Testing3D
{

    //delegate that the water component to call to render the objects in the scene
    public delegate void RenderObjects(Matrix reflectionMatrix);

    /// <summary>
    /// Options that must be passed to the water component before Initialization
    /// </summary>
    public class WaterOptions
    {
        //width and height must be of the form 2^n + 1 public int Width = 257;
        public int Height = 257;
        public float CellSpacing = .5f;

        public float WaveMapScale = 1.0f;

        public int RenderTargetSize = 512;

        //offsets for the texcoords of the wave maps updated every frame
        public Vector2 WaveMapOffset0;
        public Vector2 WaveMapOffset1;

        //the direction to offset the texcoords of the wave maps
        public Vector2 WaveMapVelocity0;
        public Vector2 WaveMapVelocity1;

        //asset names for the normal/wave maps
        public string WaveMapAsset0;
        public string WaveMapAsset1;

        public Vector4 WaterColor;
        public Vector4 SunColor;
        public Vector3 SunDirection;
        public float SunFactor;
        public float SunPower;
    }



    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class Water : Microsoft.Xna.Framework.DrawableGameComponent
    {
        GraphicsDevice device;
        Camera camera;
        Effect waterEffect;
        int width;
        int height;

        VertexPositionNormalTexture[] vertices;
        int[] indices;

        VertexBuffer vertexBuffer;
        IndexBuffer indexBuffer;

        const float waterHeight = 5.0f;

        RenderTarget2D refractionRenderTarget;

        Texture2D refractionMap;

        #region Fields
        private RenderObjects mDrawFunc;

        //vertex and index buffers for the water plane
        private VertexBuffer mVertexBuffer;
        private IndexBuffer mIndexBuffer;
        private VertexDeclaration mDecl;

        //water shader
        private Effect mEffect;
        private string mEffectAsset;

        //camera properties
        private Vector3 mViewPos;
        private Matrix mViewProj;
        private Matrix mWorld;

        //maps to render the refraction/reflection to
        private RenderTarget2D mRefractionMap;
        private RenderTarget2D mReflectionMap;

        //scrolling normal maps that we will use as a
        //a normal for the water plane in the shader
        private Texture mWaveMap0;
        private Texture mWaveMap1;

        //user specified options to configure the water object
        private WaterOptions mOptions;

        //tells the water object if it needs to update the refraction
        //map itself or not. Since refraction just needs the scene drawn
        //regularly, we can:
        // --Draw the objects we want refracted
        // --Resolve the back buffer and send it to the water
        // --Skip computing the refraction map in the water object
        private bool mGrabRefractionFromFB = false;

        private int mNumVertices;
        private int mNumTris;
        #endregion

        #region Properties

        public RenderObjects RenderObjects
        {
            set { mDrawFunc = value; }
        }

        /// <summary>
        /// Name of the asset for the Effect.
        /// </summary>
        public string EffectAsset
        {
            get { return mEffectAsset; }
            set { mEffectAsset = value; }
        }

        /// <summary>
        /// The render target that the refraction is rendered to.
        /// </summary>
        public RenderTarget2D RefractionMap
        {
            get { return mRefractionMap; }
            set { mRefractionMap = value; }
        }

        /// <summary>
        /// The render target that the reflection is rendered to.
        /// </summary>
        public RenderTarget2D ReflectionMap
        {
            get { return mReflectionMap; }
            set { mReflectionMap = value; }
        }

        /// <summary>
        /// Options to configure the water. Must be set before
        /// the water is initialized. Should be set immediately
        /// following the instantiation of the object.
        /// </summary>
        public WaterOptions Options
        {
            get { return mOptions; }
            set { mOptions = value; }
        }

        /// <summary>
        /// The world matrix of the water.
        /// </summary>
        public Matrix World
        {
            get { return mWorld; }
            set { mWorld = value; }
        }

        #endregion

        public Water(Game game)
            : base(game)
        {

        }

        public Water(Game game, Camera cam)
            : base(game)
        {
            device = (GraphicsDevice)game.Services.GetService(typeof(GraphicsDevice));
            camera = cam;


            PresentationParameters pp = device.PresentationParameters;
            refractionRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, SurfaceFormat.Alpha8, DepthFormat.Depth16);


            width = 33;
            height = 33;
            BuildMesh(new Vector3(0f,0f,0f), new Vector3(1f,0f,0f), new Vector3(0f,0f,1f));
            indices = CreateIndices(vertices);
            CopytoBuffers(vertices, indices);

            //System.IO.Stream stream;

            //refractionMap.SaveAsJpeg(stream, refractionMap.Width, refractionMap.Height);
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
        private void CopytoBuffers(VertexPositionNormalTexture[] vertices, int[] indices)
        {
            vertexBuffer = new VertexBuffer(device, VertexPositionNormalTexture.VertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices);

            indexBuffer = new IndexBuffer(device, typeof(int), indices.Length, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);
        }
        private void BuildMesh(Vector3 centerPos, Vector3 axisX, Vector3 axisZ)
        {

            // temp array for demonstraion
            VertexPositionNormalTexture[] tempVertices = new VertexPositionNormalTexture[width * height];

            int index = 0;
            for (int u = 0; u < height; u++)
            {
                for (int v = 0; v < width; v++)
                {
                    Vector3 tempVector = centerPos + (axisX / width) * (v - width / 2) + (axisZ / height) * (u - height / 2);
                    tempVertices[index].Position = tempVector;

                    index++;
                }
            }
            vertices = tempVertices;
        }
        private int[] CreateIndices(VertexPositionNormalTexture[] vertices)
        {
            int[] indices = new int[6];

            int counter = 0;
            for (int y = 0; y < 1; y++)
            {
                for (int x = 0; x < 1; x++)
                {
                    //This is for making two triangels out of a four courners
                    int lowerLeft = x + y * 2;
                    int lowerRight = (x + 1) + y * 2;
                    int topLeft = x + (y + 1) * 2;
                    int topRight = (x + 1) + (y + 1) * 2;

                    //Always make it clockwise, since computers cut away the rest with culling
                    indices[counter++] = topLeft;
                    indices[counter++] = lowerRight;
                    indices[counter++] = lowerLeft;

                    indices[counter++] = topLeft;
                    indices[counter++] = topRight;
                    indices[counter++] = lowerRight;
                }
            }

            return indices;
        }

        private Plane CreatePlane(float height, Vector3 planeNormalDirection, bool clipSide)
        {
            planeNormalDirection.Normalize();
            Vector4 planeCoeffs = new Vector4(planeNormalDirection, height);
            if (clipSide)
                planeCoeffs *= -1;

            Matrix worldViewProjection = camera.viewMatrix * camera.projectionMatrix;
            Matrix inverseWorldViewProjection = Matrix.Invert(worldViewProjection);
            inverseWorldViewProjection = Matrix.Transpose(inverseWorldViewProjection);

            planeCoeffs = Vector4.Transform(planeCoeffs, inverseWorldViewProjection);
            Plane finalPlane = new Plane(planeCoeffs);

            return finalPlane;
        }
        

        /*
        public void UpdateWaterMaps(GameTime gameTime)
        {

            // Render to the Reflection Map

            //clip objects below the water line, and render the scene upside down
            device.RasterizerState.CullMode = CullMode.CullClockwiseFace;

            device.SetRenderTarget(mReflectionMap);
            
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, mOptions.WaterColor, 1.0f, 0);

            //reflection plane in local space
            Vector4 waterPlaneL = new Vector4(0.0f, -1.0f, 0.0f, 0.0f);

            Matrix wInvTrans = Matrix.Invert(mWorld);
            wInvTrans = Matrix.Transpose(wInvTrans);

            //reflection plane in world space
            Vector4 waterPlaneW = Vector4.Transform(waterPlaneL, wInvTrans);

            Matrix wvpInvTrans = Matrix.Invert(mWorld * mViewProj);
            wvpInvTrans = Matrix.Transpose(wvpInvTrans);

            //reflection plane in homogeneous space
            Vector4 waterPlaneH = Vector4.Transform(waterPlaneL, wvpInvTrans);

            
            GraphicsDevice.ClipPlanes[0].IsEnabled = true;
            GraphicsDevice.ClipPlanes[0].Plane = new Plane(waterPlaneH);

            Matrix reflectionMatrix = Matrix.CreateReflection(new Plane(waterPlaneW));

            if (mDrawFunc != null)
              mDrawFunc(reflectionMatrix);

            GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            GraphicsDevice.SetRenderTarget(0, null);


            
            //Render to the Refraction Map


            //if the application is going to send us the refraction map
            //exit early. The refraction map must be given to the water component
            //before it renders
            if (mGrabRefractionFromFB)
            {
              GraphicsDevice.ClipPlanes[0].IsEnabled = false;
              return;
            }

            //update the refraction map, clip objects above the water line
            //so we don't get artifacts
            GraphicsDevice.SetRenderTarget(0, mRefractionMap);
            GraphicsDevice.Clear(ClearOptions.Target  ClearOptions.DepthBuffer, mOptions.WaterColor, 1.0f, 1);

            //reflection plane in local space
            waterPlaneL.W = 2.5f;

            //if we're below the water line, don't perform clipping.
            //this allows us to see the distorted objects from under the water
            if (mViewPos.Y < mWorld.Translation.Y)
            {
              GraphicsDevice.ClipPlanes[0].IsEnabled = false;
            }

            if (mDrawFunc != null)
              mDrawFunc(Matrix.Identity);

            GraphicsDevice.ClipPlanes[0].IsEnabled = false;

            GraphicsDevice.SetRenderTarget(0, null);
        }
        
        */


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
            Plane refractionPlane = CreatePlane(waterHeight + 1.5f, new Vector3(0, -1, 0), false);

            base.Draw(gameTime);
        }
    }
}
