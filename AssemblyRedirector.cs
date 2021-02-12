using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using CSUtil.Commons;
using TrafficManager.RedirectionFramework.Attributes;
using TrafficManager.RedirectionFramework.Extensions;

namespace TrafficManager.RedirectionFramework {

	public class AssemblyRedirector {
		private static Type[] _types;

		public static IDictionary<MethodInfo, RedirectCallsState> Deploy() {
			IDictionary<MethodInfo, RedirectCallsState> ret = new Dictionary<MethodInfo, RedirectCallsState>();
			_types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttributes(typeof(TargetTypeAttribute), false).Length > 0).ToArray();
			Log.Info($"Found {_types.Count()} types for redirection.");
			foreach (var type in _types) {
				Log.Info($"Processing redirectes in type {type.FullName}.");
				ret.AddRange(type.Redirect());
			}
			return ret;
		}

		public static void Revert() {
			if (_types == null) {
				return;
			}
			foreach (var type in _types) {
				type.Revert();
			}
			_types = null;
		}

	}
}
