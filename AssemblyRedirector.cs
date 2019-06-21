using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OtherCSMods.RedirectionFramework;
using RedirectionFramework.Extensions;
using RedirectionFramework.Attributes;

namespace RedirectionFramework
{

    public class AssemblyRedirector
    {
        private static Type[] _types;

        public static IDictionary<MethodInfo, RedirectCallsState> Deploy()
        {
            IDictionary<MethodInfo, RedirectCallsState> ret = new Dictionary<MethodInfo, RedirectCallsState>();
            _types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttributes(typeof(TargetTypeAttribute), false).Length > 0).ToArray();
            foreach (var type in _types) {
                ret.AddRange(type.Redirect());
            }
            return ret;
        }

        public static void Revert()
        {
            if (_types == null)
            {
                return;
            }
            foreach (var type in _types)
            {
                type.Revert();
            }
            _types = null;
        }

    }


}
