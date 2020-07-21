using System;
using LuaSettings;

namespace ExternalLibraryWithSettings
{
    /// <summary>
    /// Another example class for settings. This one has a property with default value, this will be retained if not set in the lua files!
    /// </summary>
    [Settings("ExternalSettings")]
    public class ExternalSettings
    {
        public double SomeValue { get; set; } = 256.0;
    }
}
