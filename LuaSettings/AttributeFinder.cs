using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LuaSettings
{
    internal class AttributeFinder
    {
        internal IEnumerable<Type> GetTypesWith<TAttribute>(bool inherit)
            where TAttribute : System.Attribute
        {
            var appDomain = AppDomain.CurrentDomain;
            var currentAssemblies = appDomain.GetAssemblies();
            var types = currentAssemblies
                .SelectMany(ass => ass
                    .GetTypes()
                    .Where(t => t.IsDefined(typeof(TAttribute), inherit)));

            return types;
        }

        internal IEnumerable<KeyValuePair<Type, TAttribute>> GetTypesAndAttributesWith<TAttribute>(bool inherit)
            where TAttribute : System.Attribute
        {
            var appDomain = AppDomain.CurrentDomain;
            var currentAssemblies = appDomain.GetAssemblies();
            var kv = currentAssemblies.SelectMany(ass =>
                ass.GetTypes().Where(t => t.IsDefined(typeof(TAttribute), inherit)).Select(t => new KeyValuePair<Type,TAttribute>(t, t.GetCustomAttribute(typeof(TAttribute)) as TAttribute)));
            return kv;
        }
    }
}
