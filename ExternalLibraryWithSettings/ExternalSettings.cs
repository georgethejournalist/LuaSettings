using System;
using LuaSettings;

namespace ExternalLibraryWithSettings
{
    [Settings("ExternalSettings")]
    public class ExternalSettings
    {
        public double SomeValue { get; set; }
    }
}
