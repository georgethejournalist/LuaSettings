using System;
using ExternalLibraryWithSettings;
using LuaSettings;
using Microsoft.Scripting.Runtime;
using Neo.IronLua;

namespace NeoLuaTester
{
    [Settings("ExampleGUISettings")]
    public class ExampleGUISettings
    {
        [LuaMember("Height")]
        public double Height { get; set; }

        [LuaMember(nameof(Width))]
        public double Width { get; set; }

        // the lua environment is dynamic, so this member does not even need an attribute
        public int Size { get; set; }
    }

    class Program
    {
        static void Main()
        {
            var settingsManager = new SettingsManager("../../../../Config");
            settingsManager.LoadSettings();

            var externalSettings = settingsManager.GetSettingsSection("ExternalSettings") as ExternalSettings;
            bool foundSettings = settingsManager.TryGetSettingsSection("NonExistentSettings", out object thisWillBeNull);


            //using (var lua = new Lua())
            //{
            //    dynamic g = lua.CreateEnvironment<LuaGlobal>();

            //    // create C# instance
            //    var settingsInstance = new BSceneGUISettings();
            //    // set global variable as ref to our C# instance
            //    g.BSceneGUISettings = settingsInstance;

            //    // compile the lua chunk
            //    var chunk = lua.CompileChunk(ConfigTest, "test.lua",
            //        new LuaCompileOptions() {DebugEngine = new LuaTraceLineDebugger()});

            //    try
            //    {
            //        // actually run the chunk
            //        g.dochunk(chunk);
            //    }
            //    catch (Exception e)
            //    {
            //        Console.WriteLine("Expception: {0}", e.Message);
            //        var d = LuaExceptionData.GetData(e); // get stack trace
            //        Console.WriteLine("StackTrace: {0}", d.FormatStackTrace(0, false));
            //    }

            //    // the C# instance will be filled here
            //    Console.WriteLine($"BSceneGUISettings from LUA: Width: {settingsInstance.Width}, Height: {settingsInstance.Height}");
            //}


            Console.ReadLine();
        }

        private static void Print(object[] texts)
        {
            foreach (object o in texts)
                Console.Write(o);
            Console.WriteLine();
        } // proc Print

        private static string Read(string sLabel)
        {
            Console.Write(sLabel);
            Console.Write(": ");
            return Console.ReadLine();
        } // func Read	
    }
}
