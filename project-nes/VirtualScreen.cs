using System;
using SFML.Graphics;
using SFML.System;
namespace project_nes
{
    public class VirtualScreen : Drawable
    {

        private float pixelSize;
        private VertexArray vertices;
        private Vector2u screenSize;


        // From https://github.com/amhndu/SimpleNES/blob/master/src/VirtualScreen.cpp
        public VirtualScreen(uint width, uint height, float pixel_size, Color color)
        {
            vertices = new VertexArray(PrimitiveType.Triangles, width * height * 6);
            screenSize = new Vector2u(width, height);

            pixelSize = pixel_size;

            for (uint x = 0; x < width; ++x)
            {
                for (uint y = 0; y < height; ++y)
                {
                    uint i = (x * screenSize.Y + y) * 6;
                    Vector2f coord2d = new Vector2f(x * pixelSize, y * pixelSize);

                    vertices[i + 0] = new Vertex(coord2d, color);
                    vertices[i + 1] = new Vertex(coord2d + new Vector2f(pixelSize, 0), color);
                    vertices[i + 2] = new Vertex(coord2d + new Vector2f(pixelSize, pixelSize), color);
                    vertices[i + 3] = new Vertex(coord2d + new Vector2f(pixelSize, pixelSize), color);
                    vertices[i + 4] = new Vertex(coord2d + new Vector2f(0, pixelSize), color);
                    vertices[i + 5] = new Vertex(coord2d, color);
                }
            }
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            //target.Draw(vertices, states);
            vertices.Draw(target, states);
        }

        public void SetPixel(int x, int y, Color color)
        {
            uint i = (uint)((x * screenSize.Y + y) * 6);
            if (i >= vertices.VertexCount)
                return;

            for (uint j = 0; j < 6; ++j)
            {
                vertices[i + j] = new Vertex(vertices[i + j].Position, color);
            }
        }
    }
}
