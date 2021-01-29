using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Neo.IronLua;

namespace LuaSettings
{
    public class SettingsManager
    {
        private string _pathToConfigFolder;
        private string _mainSettingsFileName;

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
            var attributeFinder = new AttributeFinder();
            var typesAttributePairs = attributeFinder.GetTypesAndAttributesWith<SettingsAttribute>(false);
            _typesWithAttributes = typesAttributePairs.ToDictionary(x => x.Key, x => x.Value);
        }

        public bool TryGetSettingsSection<T>(string key, out T settingsSection) where T: class
        {
	        settingsSection = null;
	        if (_settingsInstances.ContainsKey(key))
	        {
		        settingsSection = _settingsInstances[key] as T;
		        return true;
	        }

	        return false;
        }

        /// <summary>
        /// Gets the setting section with the specified key, if available, and safe casts to the generic parameter. Throws if the settings section was not found.
        /// </summary>
        /// <param name="key">The key of the settings section - the name of the lua table.</param>
        /// <returns></returns>
        /// <exception cref="LuaSettingsNotFoundException">Throws if the settings not found.</exception>
        public T GetSettingsSection<T>(string key) where T: class
        {
	        if (_settingsInstances.ContainsKey(key))
	        {
		        return _settingsInstances[key] as T;
	        }

	        throw new LuaSettingsNotFoundException(key, $"Configuration section under the key {key} was not found");
        }

        /// <summary>
        /// Gets the setting section with the specified key, if available. Throws if the settings section was not found.
        /// </summary>
        /// <param name="key">The key of the settings section - the name of the lua table.</param>
        /// <returns></returns>
        /// <exception cref="LuaSettingsNotFoundException">Throws if the settings not found.</exception>
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

            bool success = true;

            try
            {
                using (var lua = new Lua())
                {
                    dynamic g = lua.CreateEnvironment<LuaGlobal>();

                    // each found type has its instance created in lua context as a table - this populates the tables with default values, if any are set
                    foreach (var typeAttributePair in _typesWithAttributes)
                    {
                        var type = typeAttributePair.Key;
                        var instance = Activator.CreateInstance(type);
                        var attribute = typeAttributePair.Value;
                        var keyFromAttribute = attribute.Key;

                        var json = JsonSerializer.Serialize(instance, type);
                        var table = LuaTable.FromJson(json);

                        g[keyFromAttribute] = table;
                        _settingsInstances.Add(keyFromAttribute, instance);
                    }


                    LuaChunk chunk;
                    // compile the lua chunk
                    try
                    {
                        chunk = lua.CompileChunk(_mainSettingsFileName, new LuaCompileOptions() { DebugEngine = new LuaTraceLineDebugger() });
                    }
                    catch (LuaParseException e)
                    {
                        success = false;
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
                        success = false;
                        Console.WriteLine($"Exception {e}");
                        var d = LuaExceptionData.GetData(e); // get stack trace
                        Console.WriteLine($"StackTrace: {d.FormatStackTrace(0, false)}");
                        throw;
                    }

                    // getting actual C# object representations back from the lua tables
                    var count = _settingsInstances.Count;
                    for (int index = 0; index < count; index++)
                    {
                        var instancePair = _settingsInstances.ElementAt(index);
                        
                        // key under which the settings section has been registered
                        var key = instancePair.Key;
                        // the table filled with data from lua
                        var dynamicTable = g[key];
                        // the type of the C# object representing the settings section (needed for deserialization)
                        var typeToCreate = _typesWithAttributes.FirstOrDefault(p => p.Value.Key == key).Key;
                        // convert table to json
                        string instanceAsJson = LuaTable.ToJson(dynamicTable);
                        // deserialize json to our type
                        var deserializedInstance = JsonSerializer.Deserialize(instanceAsJson, typeToCreate);
                        // store this instance
                        _settingsInstances[key] = deserializedInstance;
                    }
                }
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDirectory);
                Console.WriteLine(success ? "Settings loaded successfully" : "Problem occurred when loading settings");
            }
        }


    }
}
