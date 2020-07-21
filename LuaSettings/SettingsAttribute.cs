using System;

namespace LuaSettings
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class SettingsAttribute : Attribute
    {
        private readonly string _key;

        public string Key
        {
            get => _key;
        }

        public SettingsAttribute(string key)
        {
            _key = key;
        }
    }
}
