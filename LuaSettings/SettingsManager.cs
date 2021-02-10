using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LuaSettings.LuaExtensionPackages;
using Neo.IronLua;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LuaSettings
{
    public class SettingsManager
    {
        private string _pathToConfigFolder;
        private string _mainSettingsFileName;

        protected internal Dictionary<Type, SettingsAttribute> _typesWithAttributes = new Dictionary<Type, SettingsAttribute>();
        private Dictionary<string, object> _settingsInstances = new Dictionary<string, object>();
        private JObject _jsonCache = null;
        private LuaTable _tableCache = null;

        private Dictionary<string, Type> _userProvidedPackages = new Dictionary<string, Type>();
        private List<Type> _userProvidedExtensionMethodClasses = new List<Type>();
        private Dictionary<string, object> _userProvidedGlobals = new Dictionary<string, object>();
        private Dictionary<string, LuaTable> _userProvidedTables = new Dictionary<string, LuaTable>();
        private Dictionary<string, Delegate> _userProvidedDelegate = new Dictionary<string, Delegate>();

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

        /// <summary>
        /// Loads the lua settings from the file provided in the Manager's constructor. Respects valid lua.
        /// If loadChildrenLuaLinks is specified, it can also recursively load 'links' to other lua scripts if these are specified as a value in a table (e.g. SomeSettings = { "Network.lua"} would also run the Network.lua script).
        /// Recursive loading of children links can be done for all tables or only for specific ones (control this through <see cref="specificChildrenOnly"/> and <see cref="specificChildrenKeys"/>).
        /// </summary>
        /// <param name="loadChildrenLuaLinks">Controls whether the loader should load links to other lua scripts from table values - not vanilla lua behavior.</param>
        /// <param name="specificChildrenOnly">Controls whether the loading of 'children lua links' is restricted to only children specified in the <see cref="specificChildrenKeys"/></param>
        /// <param name="specificChildrenKeys">Allows the user to specify the keys for which the recursive loading of children lua links should be done.</param>
        public void LoadSettings(bool loadChildrenLuaLinks = false, bool specificChildrenOnly = false, IReadOnlyCollection<string> specificChildrenKeys = null)
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

            LoadLuaContext(mainFilePath, loadChildrenLuaLinks, specificChildrenOnly, specificChildrenKeys);
        }

        /// <summary>
        /// Will add the public static methods from the static class to the Lua context - the methods will be callable from lua scripts themselves. Needs to be called before LoadSettings. DOES NOT WORK FOR EXTENSION METHODS, use <see cref="AddExtensionMethodPackage"/> for that.
        /// </summary>
        /// <param name="key">The name of the class that will be used as a global for calling the methods. E.g. adding package with method Log(string text) under name "os" will mean os.Log(someText) will be callable from lua.</param>
        /// <param name="staticClass">Type of static class that implements public static methods that we want to call from a Lua context.</param>
        public void AddMethodPackage(string key, Type staticClass)
        {
			_userProvidedPackages.Add(key, staticClass);
        }

        /// <summary>
        /// Will add the extension methods contained in the provided class to the Lua context.
        /// The methods will be callable from lua scripts through the ':' notation, e.g. a method DoSomethingSpecific(this string text) would be callable as string:DoSomethingSpecific() from lua.
        /// Needs to be called before LoadSettings.
        /// </summary>
        /// <param name="extensionMethodClass">The class containing the extension methods.</param>
        public void AddExtensionMethodPackage(Type extensionMethodClass)
        {
			_userProvidedExtensionMethodClasses.Add(extensionMethodClass);
        }

        /// <summary>
        /// Allows the user to register a delegate as a function into the lua context under the provided key/name.
        /// </summary>
        /// <param name="key">The name of the function to register the function as. Will be used to call the function from the lua context.</param>
        /// <param name="function">The delegate to call when the lua context calls the <see cref="key"/>.</param>
        public void AddFunctionDelegate(string key, Delegate function)
        {
			_userProvidedDelegate.Add(key, function);
        }

        /// <summary>
        /// Allows the user to specify a custom table that will be added to the lua context when it is initialized. Needs to be called before the <see cref="LoadSettings"/> method.
        /// </summary>
        /// <param name="table">The lua table to add to the context.</param>
        public void AddCustomTable(string key, LuaTable table)
        {
             _userProvidedTables.Add(key, table);
        }

        /// <summary>
        /// Clears the cache for user provided packages - when the LoadSettings will be next called, they will not be added to the context.
        /// </summary>
        /// <remarks>Could be useful if reloading settings without user packages.</remarks>
        public void ClearUserPackages()
        {
            _userProvidedPackages.Clear();
        }

        /// <summary>
        /// Clears the cache for user provided extension methods - when the LoadSettings will be next called, they will not be added to the context.
        /// </summary>
        /// <remarks>Could be useful if we want to reload the settings without the extension methods.</remarks>
        public void ClearUserExtensionMethods()
        {
            _userProvidedExtensionMethodClasses.Clear();
        }

        /// <summary>
        /// Returns a cached version of the lua table stripped of method/function members.
        /// </summary>
        /// <returns>A raw LuaTable representation of the lua configuration that has been loaded through <see cref="LoadSettings"/>.</returns>
        public LuaTable GetRawTableLua()
        {
	        return _tableCache;
        }

        /// <summary>
        /// Returns a cached version of the lua table stripped of method/function members, converted to JSON.
        /// </summary>
        /// <returns>A raw LuaTable representation of the lua configuration that has been loaded through <see cref="LoadSettings"/>.</returns>
        public JObject GetRawTableJson()
        {
	        return _jsonCache;
        }

        /// <summary>
        /// Allows the user to access members/tables that were defined in lua but have no registered C# class for auto-deserialization. 
        /// </summary>
        /// <param name="key">Key to the table - e.g. a name of a global variable.</param>
        /// <returns>The 'raw' lua table representation of the desired member. Null if none found under this key.</returns>
        public LuaTable GetKeyRawTable(string key)
        {
	        if (_tableCache?.ContainsKey(key) ?? false)
	        {
		        var test = _tableCache[key];
		        var cast = test as LuaTable;
		        return cast;
	        }

	        return null;
        }

        /// <summary>
        /// Allows the user to access members/tables that were defined in lua but have no registered C# class for auto-deserialization. 
        /// </summary>
        /// <param name="key">Key to the table - e.g. a name of a global variable.</param>
        /// <returns>A JSON (JToken) representation of the desired member. Null if none found under this key.</returns>
        public JToken GetKeyJToken(string key)
        {
	        if (_jsonCache.TryGetValue(key, out var token))
	        {
		        return token;
	        }

            // needs Newtonsoft.JSON 12
	        //if (_jsonCache.ContainsKey(key))
	        //{
		       // var token = _jsonCache[key];
		       // return token;
	        //}

	        return null;
        }

        protected void LoadLuaContext(string pathToMainFile, bool loadChildrenLuaLinks, bool specificChildrenOnly, IReadOnlyCollection<string> specificChildrenKeys)
        {
            var originalDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(_pathToConfigFolder);

            bool success = true;

            try
            {
                using (var lua = new Lua())
                {
                    dynamic g = lua.CreateEnvironment<LuaGlobal>();

                    // add in optional helper methods etc.
                    ExtendLuaEnvironment(g);

                    int baseMemberCount = ((LuaGlobal)g).Members.Count;

                    AddUserDefinedGlobals(g);

                    // each found type has its instance created in lua context as a table - this populates the tables with default values, if any are set
                    foreach (var typeAttributePair in _typesWithAttributes)
                    {
                        var type = typeAttributePair.Key;
                        var instance = Activator.CreateInstance(type);
                        var attribute = typeAttributePair.Value;
                        var keyFromAttribute = attribute.Key;
                        
						var json = JsonConvert.SerializeObject(instance);
						var table = LuaTable.FromJson(json);

						g[keyFromAttribute] = table;
						_settingsInstances.Add(keyFromAttribute, instance);
                    }

                    LuaChunk chunk;
                    // compile the lua chunk
                    try
                    {
                        chunk = lua.CompileChunk(_mainSettingsFileName, new LuaCompileOptions() { DebugEngine = new LuaDebugger() });
                    }
                    catch (LuaParseException e)
                    {
                        success = false;
                        Console.WriteLine($"Exception caught when parsing the lua file at line {e.Line}, column {e.Column}, source file {e.FileName}. Exception: {e}");
                        Console.WriteLine($"Offending line: {TryToGetExceptionLineFromFile(e)}");
                        throw;
                    }

                    try
                    {
	                    // actually run the chunk
	                    g.dochunk(chunk);

	                    // lua tables can contain 'links' to other lua scripts - user can specify if they want to find those files and run those chunks as well
	                    if (loadChildrenLuaLinks)
	                    {
		                    var globals = ((LuaGlobal) g).Members;
		                    var addedGlobals = globals.Skip(baseMemberCount).ToList();
		                    var global = g as LuaGlobal;
		                    foreach (var member in addedGlobals)
		                    {
			                    var table = member.Value as LuaTable;
			                    if (table == null)
			                    {
				                    continue;
			                    }

			                    if (specificChildrenOnly && specificChildrenKeys.Contains(member.Key))
			                    {
				                    RunChildLuaScripts(lua, ref global, table);
			                    }
			                    else if (!specificChildrenOnly)
			                    {
				                    RunChildLuaScripts(lua, ref global, table);
			                    }
		                    }
	                    }
                    }
                    catch (LuaParseException e)
                    {
	                    success = false;
                        Console.WriteLine($"Could not parse lua exception: {e}. File {e.FileName}, Line {e.Line}, Index {e.Index}.");
                    }
                    catch (Exception e)
                    {
                        success = false;
                        Console.WriteLine($"Exception {e}");
                        // get stack trace if possible
                        var d = LuaExceptionData.GetData(e); 
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
                        //var deserializedInstance = JsonSerializer.Deserialize(instanceAsJson, typeToCreate);
                        var deserializedInstance = JsonConvert.DeserializeObject(instanceAsJson, typeToCreate);
                        // store this instance
                        _settingsInstances[key] = deserializedInstance;
                    }

                    var members = ((LuaGlobal)g).Members;
                    //var relevantMembers = members.Skip(baseMemberCount).ToList();
                    var relevantMembers = members.Skip(baseMemberCount).ToDictionary(kv => kv.Key, kv => kv.Value);

                    // cache the results in tables
                    _tableCache = new LuaTable();

                    // skip methods defined in lua from caching - two reasons for that:
                    // 1) Json generation explodes on delegates,
                    // 2) they would most likely not be safe to call after the lua context is disposed anyway
                    var scrubbed = ScrubDelegateMembersFromTable(relevantMembers);
                    foreach (var pair in scrubbed)
                    {
	                    _tableCache.Add(pair.Key, pair.Value);
                    }


                    var jsonCache = _tableCache.ToJson();
                    _jsonCache = JObject.Parse(jsonCache);

                }
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDirectory);
                Console.WriteLine(success ? "Settings loaded successfully" : "Problem occurred when loading settings");
            }
        }

        /// <summary>
        /// A recursive method for removing function delegates from the provided member table - using this, the configuration data can be cached while the functions are discarded.
        /// The function pointers would most likely not be valid after the lua context is disposed anyway.
        /// </summary>
        /// <param name="table">The member collection of the provided lua table.</param>
        /// <returns>The same member collection with the function delegates removed.</returns>
        private IDictionary<string, object> ScrubDelegateMembersFromTable(IDictionary<string, object> table)
        {
	        var keysToRemove = new List<string>();
	        var keys = table.Keys;

	        foreach (var key in keys)
	        {
		        var value = table[key];   
		        if (value.GetType().BaseType == typeof(MulticastDelegate))
		        {
			        keysToRemove.Add(key);
		        }

		        if (value is LuaTable luaTable)
		        {
                    ScrubDelegateMembersFromTable(luaTable.Members);
		        }
                else if (value is IDictionary<string, object> dict)
		        {
			        ScrubDelegateMembersFromTable(dict);
		        }
	        }

	        foreach (var key in keysToRemove)
	        {
		        table.Remove(key);
	        }

	        return table;
        }

        private void AddUserDefinedGlobals(LuaGlobal g)
        {
	        foreach (var global in _userProvidedGlobals)
	        {
		        g[global.Key] = global.Value;
	        }
        }

        /// <summary>
        /// Runs lua files that are 'referenced' as a member of lua tables, recursively.
        /// </summary>
        /// <param name="lua">The lua context to run the scripts in.</param>
        /// <param name="g">The global lua table.</param>
        /// <param name="table">The current lua table to search for lua links.</param>
        private void RunChildLuaScripts(Lua lua, ref LuaGlobal g, LuaTable table)
        {
	        var childrenWithLuaScripts =
		        table.Values.Where(val => val.Value is string filename && filename.EndsWith(".lua"));

	        foreach (var pair in childrenWithLuaScripts)
	        {
		        var filename = pair.Value.ToString();
		        if (File.Exists(filename))
		        {
			        try
			        {
				        var chunk = lua.CompileChunk(filename, new LuaCompileOptions() { DebugEngine = new LuaDebugger() });
				        g.DoChunk(chunk);
			        }
			        catch (LuaDebuggerException e)
			        {
				        Console.WriteLine(e);
				        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error while calling a child lua script from table {table} - script {filename}, line {e.LastFrameLine}");
                        Console.ForegroundColor = ConsoleColor.White;
				        throw;
			        }
			        
		        }
	        }

	        if (table.Members.Any())
	        {
		        foreach (var member in table.Members)
		        {
			        var memberTable = member.Value as LuaTable;
			        if (memberTable != null)
			        {
				        RunChildLuaScripts(lua, ref g, memberTable);
			        }
		        }
	        }
        }

        private string TryToGetExceptionLineFromFile(LuaParseException exception)
        {
            if (File.Exists(exception.FileName))
            {
                var position = exception.Line;
                var line = File.ReadLines(exception.FileName).Skip(position - 1).Take(1).FirstOrDefault();
                return line;
            }

            return String.Empty;
        }

        private void ExtendLuaEnvironment(LuaGlobal g)
        {
            g.RegisterPackage("os", typeof(OperationSystemPackage));
            g.RegisterPackage("log", typeof(ConsolePackage));

            foreach (var package in _userProvidedPackages)
            {
	            g.RegisterPackage(package.Key, package.Value);
            }

            // extension methods need to be registered separately through RegisterTypeExtension call)
            foreach (var extensionClass in _userProvidedExtensionMethodClasses)
            {
	            LuaType.RegisterTypeExtension(extensionClass);
            }

            // tables can be registered directly
            dynamic dg = g;
            foreach (var pair in _userProvidedTables)
            {
	            dg[pair.Key] = pair.Value;
            }

            // delegates need to be registered with the LuaGlobal
            foreach (var pair in _userProvidedDelegate)
            {
	            g.DefineFunction(pair.Key, pair.Value);
            }
        }

        private void OverrideBasicMethods(LuaGlobal g)
        {
            
        }

        public void AddGlobal(string key, object globalValue)
        {
	        _userProvidedGlobals.Add(key, globalValue);
        }
    }
}
