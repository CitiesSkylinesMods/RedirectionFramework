using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TrafficManager.RedirectionFramework {
	public static class HarmonyUtil {
		/// <summary>
		/// Finds the method that is represented by the given harmony method.
		/// </summary>
		/// <param name="method">queried harmony method</param>
		/// <returns>method information</returns>
		public static MethodBase GetOriginalMethod(HarmonyMethod method) {
			if (method.declaringType == null) return null;
			if (method.methodName == null)
				return AccessTools.Constructor(method.declaringType, method.argumentTypes);
			return AccessTools.Method(method.declaringType, method.methodName, method.argumentTypes);
		}
	}
}
