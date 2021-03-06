using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using SFML.System;
using System.IO;

class MainClass
{
    public static bool Running = true;

    //The actual window of 'MainThread'
    public static Canvas Window;

    //The amount of ticks since the engine started, can also be returned by 'Engine.Ticks'.
    public static Int64 TickCount = 0;

    // Ticks per second.
    public static int TPS = 0;

    // Frames per second.
    public static int FPS = 0;

    public static List<int> CurrentKeys;

    //entity list below!

    public static List<Entity> Entities;

    private static List<RenderEntity> DrawableEntities;

    public static void Main(string[] args)
    {
        for (int i = 0; i< args.Length; ++i)
        {
            if (args[i] == "doListing")
            {
                EntityTypeList.ListAll();
                Console.WriteLine("\nEntity instancer class complete.");

                GC.Collect();
                return;
            }
        }

        Window = new Canvas(512, 512, "default");

        Entities = new List<Entity>();
        DrawableEntities = new List<RenderEntity>();

        #region TEST AREA

        // Sin of 3.31982421875
        Console.WriteLine(DeterministicMath.SinD(FInt.Create(13598, false)).ToString());
        
        Vector2 a = new Vector2(20, 210);
        Vector2 b = new Vector2(40, 230);

        for (int i = 0; i< 100; ++i)
        {
            var lp = new DTestLineProvider();

            lp.line = new Vector2[] {a, b};

            EntityCommand.Instance(lp);

            b = new Vector2(b.x + 1, b.y);
        }

        /*EntityCommand.Instance(new RTestSpriteProvider
        {
            Position = new Vector2(40, 40),
            BoundriesSet = false,
            TexturePath = @".\assets\Generic.png"
        });*/

        for(int y = 3; y< 4; ++y)
        {
            for(int x = 3; x < 5; ++x)
            {
                EntityCommand.Instance(new RTestSpriteProvider
                {
                    Position = new Vector2(51 * x, y*51),
                    BoundriesSet = false,
                    TexturePath = @".\assets\Generic.png",
                    //Rotation = (FInt)(x *12),
                    Rotate = true,
                });
            }
        }

        #endregion TEST AREA


        //This starts the Tick, Render and OnSecond loop.
        //How often they are executed depends on certain
        //values found on the Engine class...
        //Except OnSecond wich happens every second.
        Loop();
    }

    public static void Loop ()
    {
        double ticksPerSecond = Engine.MaxTPS;

        double rendersPerSecond = Engine.MaxFPS;

        Int64 swFrequency = Stopwatch.Frequency;

        double tickExecute = (double)swFrequency/ticksPerSecond;

        double renderExecute = (double)swFrequency/rendersPerSecond;

        double delta = 0.0;

        double renderDelta = 0.0;

        Int64 LastTick = 0L;

        Int64 secondTickCount = 0L;

        Stopwatch stopwatch = Stopwatch.StartNew();

        while (true)
        {
            Int64 currTick = stopwatch.ElapsedTicks;

            double elapsedTicks = currTick - LastTick;

            delta += elapsedTicks/tickExecute;

            renderDelta += elapsedTicks/renderExecute;

            secondTickCount += currTick - LastTick;

            LastTick = currTick;

            bool operationExecuted = false;

            if (delta >= 1.0)
            {
                --delta;
                //Sets 'TPSSlowdown' to the amount of ticks that will ocurr in this moment to compensate for a slowdown
                //if it's 0, it means there's no slowdown and everything is fine.
                Engine.TPSSlowdown = (int) delta;
                Tick();
                ++TickCount;

                operationExecuted = true;
            }

            if(Engine.FPSLimiter)
            {
                if(renderDelta >= 1.0)
                {
                    if(Window.IsClosed) break;

                    Render(delta);

                    //If there's a slowdown, the pending 'Render' calls are set to 0 to not overwelm the program
                    if (renderDelta >=2.0) renderDelta-= (int) renderDelta;
                    else --renderDelta;

                    operationExecuted = true;
                }
            }
            else
            {
                if(Window.IsClosed) break;

                renderDelta = 0.0;
                Render(delta);
            }

            if (secondTickCount >= swFrequency)
            {
                OnSecond();
                secondTickCount-= swFrequency;

                GC.Collect(0, GCCollectionMode.Forced, false);

                operationExecuted = true;
            }

            if (!operationExecuted && delta < 1.0 && renderDelta < 1.0 && secondTickCount < swFrequency)
            {
                double tTime = delta != 0.0 ? ((1.0 - delta) * tickExecute)/ swFrequency : 0.0;
                double rTime = renderDelta != 0.0 ? ((1.0 - renderDelta) * renderExecute)/ swFrequency : 0.0;
                double sTime = secondTickCount != 0 ?(double) (swFrequency - secondTickCount) / swFrequency : 0.0;

                char chosenTime;

                double sleepTime;

                sleepTime = tTime;
                chosenTime = 't';
                if(Engine.FPSLimiter && rTime < sleepTime)
                {
                    sleepTime = rTime;

                    chosenTime = 'r';
                }

                if (sTime < sleepTime)
                {
                    sleepTime = sTime;

                    chosenTime = 's';
                }

                int sleepMsec = (int)(sleepTime * 1000.0); 

                if (sleepMsec > 0)
                {
                    if(AntiConsoleSpam.antiConsoleSpam.CanWriteLine(23, 200 * (Engine.FPSLimiter ? 1 : 10)))
                    {
                        string chosenMsg;

                        if (chosenTime == 't') chosenMsg = $"of tick time with inverse delta {1.0 - delta}, wich is {sleepTime * 1000.0}MS";
                        else if (chosenTime == 'r') chosenMsg = $"of render time with inverse delta {1.0 - renderDelta}, wich is {sleepTime * 1000.0}MS";
                        else chosenMsg = $"of render time with inverse ticks {swFrequency - secondTickCount}, wich is {sleepTime * 1000.0}MS";

                        Console.WriteLine($"Slept for {sleepMsec} miliseconds {chosenMsg}!");
                    }

                    if(!Engine.FPSLimiter) sleepMsec = 1;

                    Thread.Sleep(sleepMsec);
                }
            }
        }
        
        stopwatch.Stop();

        OnEndProcess();
    }

    ///<summary>
    ///Gets inputs as int, can be cast back to SFML.Window.Keyboard.Key
    ///to further clarify what inputs these are, it is not cast now
    ///because i will probably make a dictionary on a input class later
    ///on.
    ///</summary>
    static void GetInputs()
    {
        CurrentKeys = new List<int>(Window.GetKeys());
    }

    ///<summary>
    ///Processes gets the inputs pressend and ticks.
    ///</summary>
    static void Tick ()
    {
        GetInputs();

        if(CurrentKeys.Count!=0)
        {
            /*Console.Write("Controls being pressed: ");
            foreach(int i in CurrentKeys)
            {
                var key = (SFML.Window.Keyboard.Key)i;
                Console.Write(key.ToString());
            }
            Console.WriteLine();
            */
        }

        if(CurrentKeys.Contains((int)SFML.Window.Keyboard.Key.Escape))
        {
            Window.Close();
        }

        ProcessEntities();
    }

    ///<summary>
    ///Ticks entities, what did you expect?
    ///</summary>
    public static void ProcessEntities ()
    {
        foreach (Entity e in Entities)
        {
            if (e.CanProcess) e.Tick();
        }
    }

    //Small value for a necessary processing.
    //But you never need to know what it does,
    //so don't even ask.
    private static long LastTickCount = -1;

    public static void Render(double lerp)
    {
        //If the window is still rendering something else, just give up on the operation
        if(Window.Updating) return;

        //If the window is still rendering something else, WAIT!
        //if(Window.Updating) Window.WaitRendering.WaitOne();

        //This 'if' prevents the program from updating the array of things to render
        //multiple times between ticks, wich isn't necessary and would be too
        //costly.
        if (LastTickCount != TickCount)
        {
            //Creates buffer for rendering
            DrawableObject[] dObjects = new DrawableObject[RenderEntity.VisibleEntityCount];

            //the count of objects that aren't null on the array
            int count = 0;

            //populates buffer with output from the entities that can be rendered
            for (int i = 0; i< DrawableEntities.Count; ++i)
            {
                var entity = DrawableEntities[i];

                if (entity.IsVisible)
                {
                    var drawable = entity.GetDrawable();

                    if (drawable != null)
                    {
                        dObjects[count] = drawable;
                        ++count;
                    }
                }
            }
            
            //Sends the things that must be rendered to the screen
            Window.SetDraw(dObjects, count);

            LastTickCount = TickCount;
        }

        //Sets lerp for the screen itself to interpolate between last tick and current tick
        //just in case the screen can render more than the amount of TPS
        Window.SetLerp(lerp < 1.0? lerp : 1.0);

        //Asks politely for the screen to actually draw those things
        // I ASKED POLITELY, SCREEN
        Window.Refresh();
    }

    public static TickMeasurer measurer = new TickMeasurer();

    ///<summary>
    ///Runs every second mostly ro record performance.
    ///Not that important.
    ///</summary>
    public static void OnSecond ()
    {
        measurer.Update();

        TPS = measurer.GetTicksThisSecond();

        FPS = Window.FPS;

        Window.FPS = 0;

        Console.WriteLine($"TPS: {TPS}");

        Console.WriteLine($"FPS: {FPS}");
    }

    ///<summary>
    ///Executes when process ends.
    ///</summary>
    static void OnEndProcess()
    {
        Running = false;

        //Closes window and it's rendering thread. (if it isn't already closed)
        Window.Close();

        //Closes texture loader thread.
        TextureHolder.Close();

        //Finalizes all entities.
        for(int i = Entities.Count-1; i<=0; --i)
        {
            RemoveEntity(Entities[i]);
        }
        for(int i = DrawableEntities.Count-1; i<=0; --i)
        {
            RemoveEntity(DrawableEntities[i]);
        }

        //Nice message to finalize :)
        Console.WriteLine("Ended main thread.");
    }

    ///<summary>
    ///Adds entity to processing, duh.
    ///Same method can be executed from the EntityCommand class at Misc/EntityCommand,
    ///it's name there is 'Instance'.
    ///</summary>
    public static void AddEntity (Entity entity)
    {
        if (entity.IsTickable) Entities.Add(entity);
        if (entity.IsDrawable) DrawableEntities.Add((RenderEntity)entity);

        entity.EnterTree();
    }

    ///<summary>
    ///Removes entity from processing, duh duh.
    ///Same method can be executed from the EntityCommand class at Misc/EntityCommand,
    ///it's name there is 'Destroy'.
    ///</summary>
    public static void RemoveEntity (Entity entity)
    {
        if (entity.IsDestroyed) return;

        entity.LeaveTree();

        if (entity.IsTickable) Entities.Remove(entity);

        if(entity.IsDrawable) DrawableEntities.Remove((RenderEntity)entity);

        entity.IsDestroyed = true;
    }
}
