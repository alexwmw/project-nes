using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace project_nes
{
    class ScreenManager
    {
        public uint Height { get; set; }
        public uint Width { get; set; }

        public ScreenManager(uint w, uint h)
        {
            Height = h;
            Width = w;
        }

        /*public VertexArray GetFrame(Color[,] array, int pixelSize)
        {
            VertexArray vertices = new VertexArray();
            vertices.Resize(Width * Height * 6);


            for(uint y = 0; y < Height; ++y)
            {
                for(uint x = 0; x < Width; ++x)
                {
                    uint i = (Width * y * 6) + (x * 6);

                    //Triangle 1
                    vertices[i + 0] = new Vertex();
                    vertices[i + 1] = new Vertex();
                    vertices[i + 2] = new Vertex();

                    //Triangle 2
                    vertices[i + 3] = new Vertex();
                    vertices[i + 4] = new Vertex();
                    vertices[i + 5] = new Vertex();
                }
            }
        }*/



    }



}
