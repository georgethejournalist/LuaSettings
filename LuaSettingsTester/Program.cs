using System;
using ExternalLibraryWithSettings;
using LuaSettings;
using Microsoft.Scripting.Runtime;
using Neo.IronLua;

namespace NeoLuaTester
{
    /// <summary>
    /// An example class for storing some application-relevant settings. All you need is the SettingsAttribute with a specified key.
    /// </summary>
    [Settings("ExampleGUISettings")]
    public class ExampleGUISettings
    {
        public double Height { get; set; }

        public double Width { get; set; }

        public int Size { get; set; }
    }

    class Program
    {
        static void Main()
        {
            var settingsManager = new SettingsManager("../../../../Config");
            settingsManager.LoadSettings();

            // this is how you can get a settings section from the manager - if no such section is found, it will throw
            // notice the safe cast, the manager only handles the types dynamically and stores them internally as an object ref
            var externalSettings = settingsManager.GetSettingsSection("ExternalSettings") as ExternalSettings;

            // non-throwing way of getting a settings section - the section in this example does not exist and will not be found
            bool foundSettings = settingsManager.TryGetSettingsSection("NonExistentSettings", out object thisWillBeNull);

            // another settings section - see the relevant config file for more details on how this example is different
            var renderSettings = settingsManager.GetSettingsSection("RenderSettings") as RenderSettings;

            Console.ReadLine();
        }
    }
}
