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
        public VirtualScreen(int width, int height, float pixel_size, Color color)
        {
            vertices = new VertexArray(PrimitiveType.Triangles, (uint)(width * height * 6));

            screenSize.X = (uint)width;
            screenSize.Y = (uint)height;

            pixelSize = pixel_size;

            for (uint x = 0; x < width; ++x)
            {
                for (uint y = 0; y < height; ++y)
                {
                    uint i = (x * screenSize.Y + y) * 6;
                    Vector2f coord2d = new Vector2f(x * pixelSize, y * pixelSize);

                    for(uint j = 0; j < 6; ++j)
                    {
                        var v = vertices[i + j];
                        v.Position = coord2d;
                        v.Color = color;
                        vertices[i + j] = v;
                    }
                }
            }
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            target.Draw(vertices, states);
        }

        public void SetPixel(int x, int y, Color color)
        {
            uint i = (uint)((x * screenSize.Y + y) * 6);
            if (i >= vertices.VertexCount)
                return;

            for (uint j = 0; j < 6; ++j)
            {
                var v = vertices[i + j];
                v.Color = color;
                vertices[i + j] = v;
            }
        }
    }
}
