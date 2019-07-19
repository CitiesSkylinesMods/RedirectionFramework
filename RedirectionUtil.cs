using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CSUtil.Commons;
using TrafficManager.RedirectionFramework.Attributes;
using System.Text.RegularExpressions;

namespace TrafficManager.RedirectionFramework {
	public static class RedirectionUtil {

		private const string methodPattern = @"^Custom";
		private readonly static Regex detourRegex = new Regex(methodPattern);

		public static Dictionary<MethodInfo, RedirectCallsState> RedirectAssembly() {
			var redirects = new Dictionary<MethodInfo, RedirectCallsState>();
			foreach (Type type in Assembly.GetExecutingAssembly().GetTypes()) {
				redirects.AddRange(RedirectType(type));
			}
			return redirects;
		}

		public static void RevertRedirects(Dictionary<MethodInfo, RedirectCallsState> redirects) {
			if (redirects == null) {
				return;
			}
			foreach (KeyValuePair<MethodInfo, RedirectCallsState> kvp in redirects) {
				RedirectionHelper.RevertRedirect(kvp.Key, kvp.Value);
			}
		}

		public static void AddRange<T>(this ICollection<T> target, IEnumerable<T> source) {
			if (target == null)
				throw new ArgumentNullException(nameof(target));
			if (source == null) {
				return;
			}
			foreach (T element in source)
				target.Add(element);
		}

		public static Dictionary<MethodInfo, RedirectCallsState> RedirectType(Type type, bool onCreated = false) {
			var redirects = new Dictionary<MethodInfo, RedirectCallsState>();

			object[] customAttributes = type.GetCustomAttributes(typeof(TargetTypeAttribute), false);
			if (customAttributes.Length != 1) {
				throw new Exception($"No target type specified for {type.FullName}!");
			}
			if (!GetRedirectedMethods<RedirectMethodAttribute>(type).Any() && !GetRedirectedMethods<RedirectReverseAttribute>(type).Any()) {
				Log.Info($"No detoured methods in type {type.FullName}.");
				return redirects;
			}
			object[] customAttributes2 = type.GetCustomAttributes(typeof(IgnoreConditionAttribute), false);
			if (customAttributes2.Any(a => ((IgnoreConditionAttribute)a).IsIgnored(type))) {
				Log.Info($"Ignoring detours for type {type.FullName}.");
				return redirects;
			}
            Type targetType = ((TargetTypeAttribute)customAttributes[0]).Type;
			RedirectMethods(type, targetType, redirects, onCreated);
			RedirectReverse(type, targetType, redirects, onCreated);
			return redirects;
		}

		private static void RedirectMethods(Type type, Type targetType, Dictionary<MethodInfo, RedirectCallsState> redirects, bool onCreated) {
			foreach (MethodInfo method in GetRedirectedMethods<RedirectMethodAttribute>(type, onCreated)) {
				Log.Info($"Redirecting {targetType.FullName}.{method.Name} with {method.GetParameters()?.Length} parameters -> {type.FullName}.{method.Name}");
				RedirectMethod(targetType, method, redirects);
			}
		}

		private static void RedirectReverse(Type type, Type targetType, Dictionary<MethodInfo, RedirectCallsState> redirects, bool onCreated) {
			foreach (MethodInfo method in GetRedirectedMethods<RedirectReverseAttribute>(type, onCreated)) {
				Log.Info($"Reverse-redirecting {type.FullName}.{method.Name} -> {targetType.FullName}.{method.Name}");
				RedirectMethod(targetType, method, redirects, true);
			}
		}

		private static IEnumerable<MethodInfo> GetRedirectedMethods<T>(Type type, bool onCreated) where T : RedirectAttribute {
			return GetRedirectedMethods<T>(type).Where(method => {
				object[] redirectAttributes = method.GetCustomAttributes(typeof(T), false);
				return ((T)redirectAttributes[0]).OnCreated == onCreated;
			});
		}

		private static IEnumerable<MethodInfo> GetRedirectedMethods<T>(Type type) where T : RedirectAttribute {
			return type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic)
				.Where(method => {
					object[] redirectAttributes = method.GetCustomAttributes(typeof(T), false);
					return redirectAttributes.Length == 1;
				}).Where(method => {
					object[] ignoreAttributes = method.GetCustomAttributes(typeof(IgnoreConditionAttribute), false);
					bool isIgnored = ignoreAttributes.Any(attribute => ((IgnoreConditionAttribute)attribute).IsIgnored(method));
					if (isIgnored) {
						Log.Info($"Ignoring method detour {type.FullName}.{method.Name}.");
					}
					return !isIgnored;
				});
		}

		private static void RedirectMethod(Type targetType, MethodInfo method, Dictionary<MethodInfo, RedirectCallsState> redirects, bool reverse = false) {
            Tuple<MethodInfo, RedirectCallsState> tuple = RedirectMethod(targetType, method, reverse);
			redirects.Add(tuple.First, tuple.Second);
		}


		private static Tuple<MethodInfo, RedirectCallsState> RedirectMethod(Type targetType, MethodInfo detour, bool reverse) {
			String originalMethodName = detourRegex.Replace(detour.Name, "");
			try {
				BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
                ParameterInfo[] parameters = detour.GetParameters();
				Type[] types;
				if (parameters.Length > 0 && (
					(!targetType.IsValueType && parameters[0].ParameterType.Equals(targetType)) ||
					(targetType.IsValueType && parameters[0].ParameterType.Equals(targetType.MakeByRefType())))) {
					types = parameters.Skip(1).Select(p => p.ParameterType).ToArray();
				} else {
					types = parameters.Select(p => p.ParameterType).ToArray();
				}
                MethodInfo originalMethod = targetType.GetMethod(originalMethodName, bindingFlags, null, types, null);
				Log._Debug($"before redirect: originalMethod: {originalMethod.MethodHandle.GetFunctionPointer()} detour: {detour.MethodHandle.GetFunctionPointer()}");
                RedirectCallsState redirectCallsState =
					reverse ? RedirectionHelper.RedirectCalls(detour, originalMethod) : RedirectionHelper.RedirectCalls(originalMethod, detour);
				Log._Debug($"after redirect: originalMethod: {originalMethod.MethodHandle.GetFunctionPointer()} detour: {detour.MethodHandle.GetFunctionPointer()}");
				return Tuple.New(reverse ? detour : originalMethod, redirectCallsState);
			} catch (Exception e) {
				Log.Error($"An error occurred while trying to {(reverse ? "reverse-" : "")}detour original method {targetType.FullName}.{originalMethodName} {(reverse ? "from" : "to")} {detour.ReflectedType.FullName}.{detour.Name}: {e.ToString()}");
				Log.Info(e.StackTrace);
				throw e;
			}
		}
	}
}