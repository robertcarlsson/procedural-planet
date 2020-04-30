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

namespace Testing3D.Terrain.Quadtree
{
    enum Edge
    {
        TopEdge = 0,
        RightEdge = 1,
        BottomEdge = 2,
        LeftEdge = 3
    }
    enum Quad
    {
        TopLeft = 0,
        TopRight = 1,
        BottomLeft = 2,
        BottomRight = 3
    }

    enum Face
    {
        FrontFace = 0,
        RightFace = 1,
        LeftFace = 2,
        BackFace = 3,
        TopFace = 4,
        BottomFace = 5
    }

    public class CelestialQuadtree : DrawableGameComponent
    {

        #region Enumeration
        
        //Since im noob and dont understand the enumeration in c#
        public byte FrontFace = 0;
        public byte RightFace = 1;
        public byte LeftFace = 2;
        public byte BackFace = 3;
        public byte TopFace = 4;
        public byte BottomFace = 5;

        public byte TopLeft = 0;
        public byte TopRight = 1;
        public byte BottomLeft = 2;
        public byte BottomRight = 3;

        public byte TopEdge = 0;
        public byte RightEdge = 1;
        public byte BottomEdge = 2;
        public byte LeftEdge = 3;

        public static int[,] FaceNeighbor = new int[6, 4];

        #endregion


        #region Fields

        


        //The seed that will say what planet it is
        int seed;
        public PlanetNoise celestialNoise;
        public Random random;

        Camera camera;
        BasicEffect effect;
        SpriteBatch sBatch;
        TextCordinates textCoord;

        public BlendInfo blendFractal;
        // Root Nodes for the celestial
        CelestialNode[] cubeRoot;
        byte[] faceOrder;

        public List<CelestialNode> processingNodes = new List<CelestialNode>();

        
        // Important information for the nodes
        int MaxDepth;
        int StartDepth;
        int NodeWidth;
        
        float MaxHeight;
        float frustumRadius;        
        float celestialRadius;
        float atmosphereRadius;

        float LODPower;
        float LODFactor;
        float numberOfSplits;
        float MaxSplits;
    
        int nodesDrawn;
        int trianglesDrawn;
        int verticesDrawn;
        int objectsDrawn;

        int count = 0;
        int amount = 1;
        #endregion

        #region Properties

        public float Radius
        {
            get { return celestialRadius; }
            set { celestialRadius = value; }
        }

        public CelestialNode[] CubeRoot
        {
            get { return cubeRoot; }
        }

        #endregion

        #region Constructor

        public CelestialQuadtree(Game game, Camera camera, TextCordinates textCoord, int seed , int MaxDepth, int NodeWidth)
            : base(game)
        {
            this.textCoord = textCoord;
            this.camera = camera;
            sBatch = (SpriteBatch)game.Services.GetService(typeof(SpriteBatch));
            effect = new BasicEffect(Game.GraphicsDevice);
            StartDepth = 0;
            celestialRadius = 1f;
            this.MaxDepth = MaxDepth;
            this.NodeWidth = NodeWidth;
            SetUpCelestial(seed);
            SetUpCubeNodes(game, camera);
            // TODO: Construct any child components her
        }

        public void InitFaceLookUp()
        {
            // Set up the N / E / S / W neighbors of the front face
            FaceNeighbor[FrontFace, TopEdge] = TopFace;
            FaceNeighbor[FrontFace, RightEdge] = RightFace;
            FaceNeighbor[FrontFace, BottomEdge] = BottomFace;
            FaceNeighbor[FrontFace, LeftEdge] = LeftFace;

            // Set up the N / E / S / W neighbors of the right face
            FaceNeighbor[RightFace, TopEdge] = TopFace;
            FaceNeighbor[RightFace, RightEdge] = BackFace;
            FaceNeighbor[RightFace, BottomEdge] = BottomFace;
            FaceNeighbor[RightFace, LeftEdge] = FrontFace;

            // Set up the N / E / S / W neighbors of the left face
            FaceNeighbor[LeftFace, TopEdge] = TopFace;
            FaceNeighbor[LeftFace, RightEdge] = FrontFace;
            FaceNeighbor[LeftFace, BottomEdge] = BottomFace;
            FaceNeighbor[LeftFace, LeftEdge] = BackFace;

            // Set up the N / E / S / W neighbors of the back face
            FaceNeighbor[BackFace, TopEdge] = TopFace;
            FaceNeighbor[BackFace, RightEdge] = LeftFace;
            FaceNeighbor[BackFace, BottomEdge] = BottomFace;
            FaceNeighbor[BackFace, LeftEdge] = RightFace;

            // Set up the N / E / S / W neighbors of the top face
            FaceNeighbor[TopFace, TopEdge] = BackFace;
            FaceNeighbor[TopFace, RightEdge] = RightFace;
            FaceNeighbor[TopFace, BottomEdge] = FrontFace;
            FaceNeighbor[TopFace, LeftEdge] = LeftFace;

            // Set up the N / E / S / W neighbors of the bottom face
            FaceNeighbor[BottomFace, TopEdge] = FrontFace;
            FaceNeighbor[BottomFace, RightEdge] = RightFace;
            FaceNeighbor[BottomFace, BottomEdge] = BackFace;
            FaceNeighbor[BottomFace, LeftEdge] = LeftFace;
        }
        #endregion
        #region Methods

        
        private void SetUpCelestial(int seed)
        {
            celestialNoise = new PlanetNoise(12, seed);

            blendFractal = new BlendInfo(2.0f, 20);
            blendFractal.fBm.inUse = true;
            blendFractal.fBm.octaves = 20;
            blendFractal.fBm.signLevel = 0.05f;
            blendFractal.Perturbing.inUse = true;
            blendFractal.Perturbing.octaves = 8;
            blendFractal.Perturbing.signLevel = 4.0f;
            //blendFractal.HeteroTerrain.inUse = true;
            //blendFractal.MultifBm.inUse = true;
            blendFractal.Turbulence.inUse = true;
            blendFractal.Turbulence.octaves = 16;
            blendFractal.Turbulence.signLevel = 0.05f;
            //blendFractal.RidgedMF.inUse = true;
            //blendFractal.RidgedMF.signLevel = 0.0002f;
        }
        private void SetUpCubeNodes(Game game, Camera camera)
        {
            cubeRoot = new CelestialNode[6];
            cubeRoot[FrontFace] = new CelestialNode(game, camera, effect, this, new Vector3(0, 0, 1), new Vector3(2, 0, 0), new Vector3(0, 2, 0), MaxDepth, NodeWidth, StartDepth, FrontFace);
            cubeRoot[RightFace] = new CelestialNode(game, camera, effect, this, new Vector3(1, 0, 0), new Vector3(0, 0, -2), new Vector3(0, 2, 0), MaxDepth, NodeWidth, StartDepth, RightFace);
            cubeRoot[LeftFace] = new CelestialNode(game, camera, effect, this, new Vector3(-1, 0, 0), new Vector3(0, 0, 2), new Vector3(0, 2, 0), MaxDepth, NodeWidth, StartDepth, LeftFace);
            cubeRoot[BackFace] = new CelestialNode(game, camera, effect, this, new Vector3(0, 0, -1), new Vector3(-2, 0, 0), new Vector3(0, 2, 0), MaxDepth, NodeWidth, StartDepth, BackFace);
            cubeRoot[TopFace] = new CelestialNode(game, camera, effect, this, new Vector3(0, 1, 0), new Vector3(2, 0, 0), new Vector3(0, 0, -2), MaxDepth, NodeWidth, StartDepth, TopFace);
            cubeRoot[BottomFace] = new CelestialNode(game, camera, effect, this, new Vector3(0, -1, 0), new Vector3(2, 0, 0), new Vector3(0, 0, 2), MaxDepth, NodeWidth, StartDepth, BottomFace);         
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

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // TODO: Add your update code here
            foreach (CelestialNode node in cubeRoot)
            {
                node.Update();
            }
            ProcessNodes();
            
            base.Update(gameTime);
        }

        private void ProcessNodes()
        {
            int index = 0;
            int nSplit = 0;

            for (int i = 0; i < processingNodes.Count; i++)
            {
                if (nSplit < processingNodes[i].numberOfSplits)
                {
                    nSplit = processingNodes[i].numberOfSplits;
                    index = i;
                }
            }

            if (processingNodes.Count > 0)
            {
                if (amount > processingNodes.Count)
                    amount = processingNodes.Count;

                int i;
                for (i = 0; i < amount; i++)
                {
                    if (processingNodes[index].SplitTest())
                    {
                        processingNodes[index].SplitNode();
                    }
                    else
                        processingNodes.RemoveAt(index);
                }
                amount--;

                if (processingNodes.Count > 0)
                    for (i = processingNodes.Count - 1; i >= 0; i--)
                        processingNodes.RemoveAt(i);

            }
        }
        public override void Draw(GameTime gameTime)
        {
            BoundingFrustum cameraFrustum = new BoundingFrustum(camera.viewMatrix * camera.projectionMatrix);
            foreach (CelestialNode node in cubeRoot)
            {
                node.Draw(cameraFrustum);
            }
            if (textCoord != null)
            {
                textCoord.DrawString("Vertices : " + Node.numberOfVertices.ToString(), Color.Black);
                textCoord.DrawString("HighestSplit : " + Node.highestSplit.ToString(), Color.Black);
            }
            if (count % 1 == 0)
                amount = 1;
            count++;
            base.Draw(gameTime);
        }
        #endregion


    }


    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class Quadtree : Microsoft.Xna.Framework.DrawableGameComponent
    {
        public List<Node> processingNodes = new List<Node>();
        SpriteBatch sBatch;
        TextCordinates texCord;
        NoiseConstruct noise;
        Node[] cubeNodes;
        Camera camera;
        BasicEffect effect;

        int amount = 1;

        public Quadtree(Game game, Camera camera, TextCordinates texCord)
            : base(game)
        {
            this.texCord = texCord;
            this.camera = camera;
            sBatch = (SpriteBatch)game.Services.GetService(typeof(SpriteBatch));
            effect = new BasicEffect(Game.GraphicsDevice);
            //SetUpCubeNodes(game, camera);
            // TODO: Construct any child components here
        }

        public void SetUpCubeNodes(Game game, Camera camera)
        {
            Node.random = new Random();
            Node.numberOfVertices = 0;
            Node.highestSplit = 0;
            Node.planetNoise = new PlanetNoise(12, 3);
            Node.Planet = this;

            cubeNodes = new Node[1];
            cubeNodes[0] = new Node(game, camera, effect, new Vector3(0, 0, 1), new Vector3(2, 0, 0), new Vector3(0, 2, 0), 0);
            //cubeNodes[1] = new Node(game, camera, effect, new Vector3(1, 0, 0), new Vector3(0, 0, -2), new Vector3(0, 2, 0), 0);
            //cubeNodes[2] = new Node(game, camera, effect, new Vector3(-1, 0, 0), new Vector3(0, 0, 2), new Vector3(0, 2, 0), 0);
            //cubeNodes[3] = new Node(game, camera, effect, new Vector3(0, 0, -1), new Vector3(-2, 0, 0), new Vector3(0, 2, 0), 0);
            //cubeNodes[4] = new Node(game, camera, effect, new Vector3(0, 1, 0), new Vector3(2, 0, 0), new Vector3(0, 0, -2), 0);
            //cubeNodes[5] = new Node(game, camera, effect, new Vector3(0, -1, 0), new Vector3(2, 0, 0), new Vector3(0, 0, 2), 0);         
        }
        public void SetUpXZNode(Game game, Camera camera)
        {
            Node.random = new Random();
            Node.numberOfVertices = 0;
            Node.highestSplit = 0;
            Node.planetNoise = new PlanetNoise(12, 4);
            Node.Planet = this;

            cubeNodes = new Node[1];
            cubeNodes[0] = new Node(game, camera, effect, new Vector3(0, 1, 0), new Vector3(2, 0, 0), new Vector3(0, 0, -2), 0, "planeNode");
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

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // TODO: Add your update code here
            foreach (Node node in cubeNodes)
            {
                node.Update();
            }
            ProcessNodes();
            
            base.Update(gameTime);
        }

        private void ProcessNodes()
        {
            int index = 0;
            int nSplit = 0;

            for (int i = 0; i < processingNodes.Count; i++)
            {
                if (nSplit < processingNodes[i].numberOfSplits)
                {
                    nSplit = processingNodes[i].numberOfSplits;
                    index = i;
                }
            }

            if (processingNodes.Count > 0)
            {
                if (amount > processingNodes.Count)
                    amount = processingNodes.Count;

                int i;
                for (i = 0; i < amount; i++)
                {
                    if (processingNodes[index].SplitTest())
                    {
                        if (processingNodes[index].isPlane)
                            processingNodes[index].SplitNodePlane();
                        else
                            processingNodes[index].SplitNode();
                    }
                    else
                        processingNodes.RemoveAt(index);
                }
                amount--;

                if (processingNodes.Count > 0)
                    for (i = processingNodes.Count - 1; i >= 0; i--)
                        processingNodes.RemoveAt(i);

            }
        }
        int count = 0;
        public override void Draw(GameTime gameTime)
        {
            BoundingFrustum cameraFrustum = new BoundingFrustum(camera.viewMatrix * camera.projectionMatrix);
            foreach (Node node in cubeNodes)
            {
                node.Draw(cameraFrustum);
            }
            if (texCord != null)
            {
                texCord.DrawString("Vertices : " + Node.numberOfVertices.ToString(), Color.Black);
                texCord.DrawString("HighestSplit : " + Node.highestSplit.ToString(), Color.Black);
            }
            if (count % 2 == 0)
                amount = 1;
            count++;
            base.Draw(gameTime);
        }
    }
 
}
