using System;
using System.Collections.Generic;
using System.Text;

namespace LuaSettings
{
    public class LuaSettingsNotFoundException : Exception
    {
        public string SettingsSectionKey { get; }

        public LuaSettingsNotFoundException() : base()
        {
            
        }

        public LuaSettingsNotFoundException(string message) : base(message)
        {
            
        }

        public LuaSettingsNotFoundException(string message, Exception inner) : base(message, inner)
        {

        }

        public LuaSettingsNotFoundException(string settingsSectionKey, string message) : base(message)
        {
            SettingsSectionKey = settingsSectionKey;
        }

        public LuaSettingsNotFoundException(string settingsSectionKey, string message, Exception inner) : base(message, inner)
        {
            SettingsSectionKey = settingsSectionKey;
        }
    }
}
