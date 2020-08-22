using System;
using SFML.Window;
using SFML.System;
using SFML.Graphics;

namespace project_nes
{
    public class SFML_Loop
    {
        public const int fps = 60;
        public const float time_until_update = 1f / fps;   

        public SFML_Loop(uint width, uint height, string title, Color clearColor)
        {
            this.WindowClearColor = clearColor;
            this.Window = new RenderWindow(new VideoMode(width, height), title);
        }

        public RenderWindow Window { get; protected set; }

        public GameTime Game_Time { get; protected set; }

        public Color WindowClearColor { get; protected set; }

        public void Run()
        {
            LoadContent();
            Initialize();

            float totalTimeBeforeUpdate = 0f;
            float previousTimeElpased = 0f;
            float deltaTime = 0f;
            float totalTimeElapsed = 0f;

            Clock clock = new Clock();

            while (Window.IsOpen)
            {
                Window.DispatchEvents();

                totalTimeElapsed = clock.ElapsedTime.AsSeconds();
                deltaTime = totalTimeElapsed - previousTimeElpased;
                previousTimeElpased = totalTimeElapsed;
                totalTimeBeforeUpdate += deltaTime;

                if(totalTimeBeforeUpdate >= time_until_update)
                {
                    Game_Time.Update(totalTimeBeforeUpdate, clock.ElapsedTime.AsSeconds());
                    totalTimeBeforeUpdate = 0f;

                    Update(Game_Time);

                    Window.Clear(WindowClearColor);
                    Draw(Game_Time);
                    Window.Display();
                }
            }
        }

        public void LoadContent()
        {

        }


        public void Initialize()
        {

        }

        public void Update(GameTime gameTime)
        {

        }

        public void Draw(GameTime gameTime)
        {

        }



        public class GameTime
        {
            private float deltaTime;
            private float timeScale;

            public GameTime()
            {
                deltaTime = 0f;
                timeScale = 1f;
            }


            public float TimeScale
            {
                get => timeScale;
                set => timeScale = value;
            }

            public float DeltaTime
            {
                get => deltaTime * timeScale;
                set => deltaTime = value;
            }

            public float DeltaTimeUnscaled
            {
                get => deltaTime;
                set => deltaTime = value;
            }

            public float TotalTimeElapsed
            {
                get;
                private set;
            }

            public void Update(float deltaTime, float totalTimeElapsed)
            {
                this.deltaTime = deltaTime;
                TotalTimeElapsed = totalTimeElapsed;
            }
        }
    }
}
