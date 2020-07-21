using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Neo.IronLua;

namespace LuaSettings
{
    public class SettingsManager
    {
        private string _pathToConfigFolder;
        private string _mainSettingsFileName;

        private List<Type> _attributedTypes = new List<Type>();
        private Dictionary<Type, SettingsAttribute> _typesWithAttributes = new Dictionary<Type, SettingsAttribute>();
        private Dictionary<string, object> _settingsInstances = new Dictionary<string, object>();

        public SettingsManager(string pathToConfigFolder, string mainSettingsFileName = "MainSettings.lua")
        {
            _pathToConfigFolder = pathToConfigFolder;
            _mainSettingsFileName = mainSettingsFileName;

            CollectAttributedSettingsClasses();
        }

        private void CollectAttributedSettingsClasses()
        {
            //var callingAssembly = Assembly.GetEntryAssembly()?.FullName;
            var attributeFinder = new AttributeFinder();
            var settingsClasses = attributeFinder.GetTypesWith<SettingsAttribute>(false);
            _attributedTypes = settingsClasses.ToList();

            var typesAttributePairs = attributeFinder.GetTypesAndAttributesWith<SettingsAttribute>(false);
            _typesWithAttributes = typesAttributePairs.ToDictionary(x => x.Key, x => x.Value);
        }

        public object GetSettingsSection(string key)
        {
            if (_settingsInstances.ContainsKey(key))
            {
                return _settingsInstances[key];
            }

            throw new LuaSettingsNotFoundException(key, $"Configuration section under the key {key} was not found");
        }

        public bool TryGetSettingsSection(string key, out object settingsSection)
        {
            if (_settingsInstances.ContainsKey(key))
            {
                settingsSection = _settingsInstances[key];
                return true;
            }

            settingsSection = null;

            return false;
        }

        public void LoadSettings()
        {
            if (!Directory.Exists(_pathToConfigFolder))
            {
                throw new DirectoryNotFoundException("Trying to load settings from a directory which was not found");
            }

            var mainFilePath = System.IO.Path.Combine(_pathToConfigFolder, _mainSettingsFileName);

            if (!File.Exists(mainFilePath))
            {
                throw new FileNotFoundException($"Could not find the main settings file at {mainFilePath}");
            }

            LoadLuaContext(mainFilePath);
        }

        protected void LoadLuaContext(string pathToMainFile)
        {
            var originalDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(_pathToConfigFolder);

            try
            {
                using (var lua = new Lua())
                {
                    dynamic g = lua.CreateEnvironment<LuaGlobal>();

                    // for each type that we found
                    // create a C# instance
                    // feed it into the global lua context
                    // then run the lua chunk
                    // afterwards retrieve?
                    foreach (var typeAttributePair in _typesWithAttributes)
                    {
                        var type = typeAttributePair.Key;
                        var instance = Activator.CreateInstance(type);
                        var attribute = typeAttributePair.Value;
                        var keyFromAttribute = attribute.Key;

                        g[keyFromAttribute] = instance;
                        _settingsInstances.Add(keyFromAttribute, instance);
                    }



                    // create C# instance
                    //var settingsInstance = new BSceneGUISettings();
                    // set global variable as ref to our C# instance
                    //g.BSceneGUISettings = settingsInstance;

                    LuaChunk chunk;
                    // compile the lua chunk
                    try
                    {
                        chunk = lua.CompileChunk(_mainSettingsFileName, new LuaCompileOptions() { DebugEngine = new LuaTraceLineDebugger() });
                    }
                    catch (LuaParseException e)
                    {
                        Console.WriteLine($"Exception caught when parsing the lua file at line {e.Line}, column {e.Column}, source file {e.FileName}. Exception: {e}");
                        throw;
                    }


                    try
                    {
                        // actually run the chunk
                        g.dochunk(chunk);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception: {0}", e.Message);
                        var d = LuaExceptionData.GetData(e); // get stack trace
                        Console.WriteLine("StackTrace: {0}", d.FormatStackTrace(0, false));
                    }

                    foreach (var instance in _settingsInstances)
                    {
                        Console.WriteLine($"Found instance {instance.Key} with value {instance.Value}");
                    }
                    // the C# instance will be filled here
                    //Console.WriteLine($"BSceneGUISettings from LUA: Width: {settingsInstance.Width}, Height: {settingsInstance.Height}");
                }
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDirectory);
            }
            

            Console.WriteLine("Testing the collection");
        }


    }
}
