using System;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
namespace project_nes
{
    public class IODevice : Drawable
    {

        private Color initColor = Color.Yellow; // init color
        private VertexArray vertices;
        private Vector2u screenSize;
        private RenderWindow window;

        public IODevice(uint width, uint height, uint pixel_size)
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

                    vertices[i + 0] = new Vertex(coord2d, initColor);
                    vertices[i + 1] = new Vertex(coord2d + new Vector2f(pixel_size, 0), initColor);
                    vertices[i + 2] = new Vertex(coord2d + new Vector2f(pixel_size, pixel_size), initColor);
                    vertices[i + 3] = new Vertex(coord2d + new Vector2f(pixel_size, pixel_size), initColor);
                    vertices[i + 4] = new Vertex(coord2d + new Vector2f(0, pixel_size), initColor);
                    vertices[i + 5] = new Vertex(coord2d, initColor);
                }
            }
        }

        public void SetPixel(int x, int y, Color color)
        {
            uint i = (uint)((x * screenSize.Y + y) * 6);
            if (i >= vertices.VertexCount)
                return;

            Vertex v;
            for (uint j = 0; j < 6; ++j)
            {
                v = vertices[i + j];
                v.Color = color;
                vertices[i + j] = v;
            }
        }

        public void Draw(RenderTarget target, RenderStates states) => vertices.Draw(target, states);

        public bool WindowIsOpen => window.IsOpen;

        public void Close() => window.Close();

        public void DrawToWindow() => window.Draw(this);

        public void Display() => window.Display();

        public void AddKeyPressEvent(EventHandler<KeyEventArgs> action) => window.KeyPressed += action;

        public void AddClosedEvent(EventHandler action) => window.Closed += action;

        public void Clear() => window.Clear();

        public void Clear(Color color) => window.Clear(color);

        public void DispatchEvents() => window.DispatchEvents();

    }
}
