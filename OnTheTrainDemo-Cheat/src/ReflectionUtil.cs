using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace OnTheTrainDemoCheat
{
    /// <summary>
    /// Reflection helpers that locate game types/members at runtime.
    /// The mod never references Assembly-CSharp at compile time, so it stays resilient
    /// to small game updates and avoids pulling in the full game dependency graph.
    /// </summary>
    internal static class ReflectionUtil
    {
        private static readonly Dictionary<string, Type> TypeCache = new Dictionary<string, Type>();

        private const BindingFlags InstanceFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private const BindingFlags AllFlags =
            InstanceFlags | BindingFlags.Static;

        /// <summary>Locate a type by simple name or full name across all loaded assemblies.</summary>
        public static Type FindType(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (TypeCache.TryGetValue(name, out var cached)) return cached;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Fast path: exact full-name match.
                try
                {
                    var direct = asm.GetType(name);
                    if (direct != null) { TypeCache[name] = direct; return direct; }
                }
                catch { }

                // Slow path: scan all (partially loadable) types, matching by simple name.
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types ?? Type.EmptyTypes; }
                catch { continue; }

                foreach (var ty in types)
                {
                    if (ty == null) continue;
                    if (ty.Name == name || ty.FullName == name)
                    {
                        TypeCache[name] = ty;
                        return ty;
                    }
                }
            }

            return null; // do NOT cache null — the type may load later.
        }

        /// <summary>Find the first type whose full name contains the given fragment.</summary>
        public static Type FindTypeContains(string fragment)
        {
            if (string.IsNullOrEmpty(fragment)) return null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types ?? Type.EmptyTypes; }
                catch { continue; }

                foreach (var ty in types)
                {
                    if (ty == null) continue;
                    if (ty.FullName != null && ty.FullName.Contains(fragment))
                        return ty;
                }
            }
            return null;
        }

        /// <summary>Find the first active instance of a component type by name.</summary>
        public static T FindComponent<T>(string typeName) where T : class
        {
            var t = FindType(typeName);
            if (t == null) return null;
            var arr = UnityEngine.Object.FindObjectsOfType(t);
            if (arr == null || arr.Length == 0) return null;
            return arr[0] as T;
        }

        /// <summary>Invoke the first existing no-arg method among the candidate names.</summary>
        public static object InvokeMethod(object obj, params string[] names)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            foreach (var n in names)
            {
                var m = t.GetMethod(n, AllFlags);
                if (m != null)
                {
                    try { return m.Invoke(obj, null); }
                    catch { /* overload mismatch — try next candidate */ }
                }
            }
            return null;
        }

        /// <summary>Set the first settable property/field among the candidate names.</summary>
        public static void SetMemberValue(object obj, object value, params string[] names)
        {
            if (obj == null) return;
            var t = obj.GetType();
            foreach (var n in names)
            {
                var p = t.GetProperty(n, InstanceFlags);
                if (p != null && p.CanWrite)
                {
                    try { p.SetValue(obj, ConvertValue(p.PropertyType, value), null); return; }
                    catch { }
                }
                var f = t.GetField(n, InstanceFlags);
                if (f != null)
                {
                    try { f.SetValue(obj, ConvertValue(f.FieldType, value)); return; }
                    catch { }
                }
            }
        }

        /// <summary>Read the first readable property/field among the candidate names.</summary>
        public static object GetMemberValue(object obj, params string[] names)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            foreach (var n in names)
            {
                var p = t.GetProperty(n, InstanceFlags);
                if (p != null && p.CanRead)
                {
                    try { return p.GetValue(obj, null); }
                    catch { }
                }
                var f = t.GetField(n, InstanceFlags);
                if (f != null)
                {
                    try { return f.GetValue(obj); }
                    catch { }
                }
            }
            return null;
        }

        private static object ConvertValue(Type target, object value)
        {
            if (value == null) return null;
            var src = value.GetType();
            if (target == src || target.IsAssignableFrom(src)) return value;

            // Common numeric coercions for when the member type differs from the value type.
            try
            {
                if (target == typeof(float))  return Convert.ToSingle(value);
                if (target == typeof(double)) return Convert.ToDouble(value);
                if (target == typeof(int))    return Convert.ToInt32(value);
                if (target == typeof(long))   return Convert.ToInt64(value);
                if (target == typeof(bool))   return Convert.ToBoolean(value);
            }
            catch { }
            return value;
        }
    }
}
