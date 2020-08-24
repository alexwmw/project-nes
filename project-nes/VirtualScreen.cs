using System;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
namespace project_nes
{
    public class VirtualScreen : Drawable
    {

        private Color color = Color.Black; // init color
        private VertexArray vertices;
        private Vector2u screenSize;
        private RenderWindow window;

        // From https://github.com/amhndu/SimpleNES/blob/master/src/VirtualScreen.cpp
        public VirtualScreen(uint width, uint height, uint pixel_size)
        {
            width *=  pixel_size;
            height *= pixel_size;
            vertices = new VertexArray(PrimitiveType.Triangles, width * height * 6);
            screenSize = new Vector2u(width, height);
            window = new RenderWindow(new VideoMode(width, height), "ProjectNES");



            for (uint x = 0; x < width; ++x)
            {
                for (uint y = 0; y < height; ++y)
                {
                    uint i = (x * screenSize.Y + y) * 6;
                    Vector2f coord2d = new Vector2f(x * pixel_size, y * pixel_size);

                    vertices[i + 0] = new Vertex(coord2d, color);
                    vertices[i + 1] = new Vertex(coord2d + new Vector2f(pixel_size, 0), color);
                    vertices[i + 2] = new Vertex(coord2d + new Vector2f(pixel_size, pixel_size), color);
                    vertices[i + 3] = new Vertex(coord2d + new Vector2f(pixel_size, pixel_size), color);
                    vertices[i + 4] = new Vertex(coord2d + new Vector2f(0, pixel_size), color);
                    vertices[i + 5] = new Vertex(coord2d, color);
                }
            }
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

        public void Draw(RenderTarget target, RenderStates states) => vertices.Draw(target, states);

        public bool IsOpen => window.IsOpen;

        public void Close() => window.Close();

        public void DrawToWindow() => window.Draw(this);

        public void Display() => window.Display();

        public void AddKeyPressEvent(EventHandler<KeyEventArgs> action)
        {
            window.KeyPressed += action;
        }

        public void Clear() => window.Clear();

        public void Clear(Color color) => window.Clear(color);

        public void DispatchEvents() => window.DispatchEvents();
    }
}
