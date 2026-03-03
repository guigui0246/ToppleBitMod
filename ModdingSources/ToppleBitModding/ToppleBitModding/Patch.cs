using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace ToppleBitModding
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PatchAttribute : Attribute
    {
        public Type TargetType { get; }
        public int Order { get; }
        public PatchAttribute(Type targetType, int order) {
            TargetType = targetType;
            Order = order;
        }

        public PatchAttribute(Type targetType)
        {
            TargetType = targetType;
            Order = 0;
        }

    }

    public static class PatchEngine
    {
        private static List<PatchAttribute> PatchedTypes = new List<PatchAttribute>();

        public static void PatchAll()
        {
            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;

                    try
                    {
                        types = assembly.GetTypes();
                    }
                    catch
                    {
                        // Mono-safe fallback: skip broken assemblies
                        Loader.Log($"[PatchEngine] Skipping types from {assembly.FullName}");
                        continue;
                    }

                    foreach (var type in types)
                    {
                        var attr = type.GetCustomAttribute<PatchAttribute>();
                        if (attr == null) continue;

                        PatchedTypes.Add(attr);
                        var targetType = attr.TargetType;
                        PatchType(type, targetType);
                    }
                }
                ForceUnityReload();

                Loader.Log("[PatchEngine] All patches applied!");
            }
            catch (Exception ex)
            {
                Loader.Log("[PatchEngine] Error: " + ex.Message + "\nat: " + ex.StackTrace.ToString());
            }
        }

        public static void ForceUnityReload()
        {
            try
            {
                var sceneManagerType = Type.GetType("UnityEngine.SceneManagement.SceneManager, UnityEngine.CoreModule") ?? throw new MissingReferenceException("UnityEngine.SceneManagement.SceneManager cannot be found");

                var loadSceneMethod = sceneManagerType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(m => {
                    var parameters = m.GetParameters();
                    Loader.Log($"method {m.Name} found with {parameters.Length} params");
                    if (m.Name != "LoadScene") return false;
                    return parameters.Length == 2 &&
                           parameters[0].ParameterType == typeof(string);
                }) ?? throw new MissingMethodException("SceneManager", "LoadScene");

                var getActiveSceneMethod = sceneManagerType.GetMethod("GetActiveScene", BindingFlags.Public | BindingFlags.Static);
                var activeScene = getActiveSceneMethod.Invoke(null, null);
                var nameProperty = activeScene.GetType().GetProperty("name");
                string sceneName = (string)nameProperty.GetValue(activeScene);

                loadSceneMethod.Invoke(null, new object[] { sceneName, LoadSceneMode.Single });
                Loader.Log("Scene reloaded");
                return;
            } catch (Exception e) {
                Loader.Log($"WARNING error reloading scene, trying to catch by running all new awake methods:\n{e.Message}\n{e.StackTrace}");
            }
            UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(MonoBehaviour));

            foreach (PatchAttribute PatchedType in PatchedTypes.OrderBy((p) => p.Order))
            {
                foreach (var obj in allObjects)
                {
                    var mb = obj as MonoBehaviour;
                    if (mb == null) continue;

                    if (!PatchedType.TargetType.IsAssignableFrom(mb.GetType())) continue;
                    
                    // Call Awake via reflection
                    var awake = PatchedType.TargetType.GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    try
                    {
                        awake?.Invoke(mb, null);
                    }
                    catch (Exception ex)
                    {
                        Loader.Log($"[PatchEngine] Error while running Awake on {PatchedType.TargetType}:\n{ex.Message} at {ex.StackTrace}");
                    }
                }
            }
        }

        private static MethodInfo ResolveTargetMethod(MethodInfo patch, Type target)
        {
            var patchParams = patch.GetParameters()
                .Where(p =>
                    p.Name != "__instance" &&
                    !typeof(Delegate).IsAssignableFrom(p.ParameterType))
                .Select(p => p.ParameterType)
                .ToArray();

            return target.GetMethods(BindingFlags.Public |
                                     BindingFlags.NonPublic |
                                     BindingFlags.Instance |
                                     BindingFlags.Static)
                .FirstOrDefault(m =>
                {
                    if (m.Name != patch.Name) return false;
                    var mp = m.GetParameters().Select(p => p.ParameterType).ToArray();
                    return mp.SequenceEqual(patchParams);
                });
        }


        private static ConstructorInfo ResolveTargetConstructor(MethodInfo patch, Type target)
        {
            var patchParams = patch.GetParameters()
                .Where(p =>
                    p.Name != "__instance" &&
                    !typeof(Delegate).IsAssignableFrom(p.ParameterType))
                .Select(p => p.ParameterType)
                .ToArray();

            return target.GetConstructors(BindingFlags.Public |
                                     BindingFlags.NonPublic |
                                     BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    var mp = m.GetParameters().Select(p => p.ParameterType).ToArray();
                    return mp.SequenceEqual(patchParams);
                });
        }
        private static void PatchType(Type patchType, Type targetType)
        {
            try
            {
                var methods = patchType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach (var patchMethod in methods)
                {
                    if (patchMethod.Name == "Constructor")
                        continue;
                    var targetMethod = ResolveTargetMethod(patchMethod, targetType);
                    if (targetMethod == null) continue;

                    Detour(targetType, patchMethod.Name, targetMethod, patchMethod);
                    Loader.Log($"[PatchEngine] Patched {targetType.Name}.{targetMethod.Name}");
                }
                foreach (var patchConstructor in methods)
                {
                    if (patchConstructor.Name != "Constructor")
                        continue;
                    var targetConstructor = ResolveTargetConstructor(patchConstructor, targetType);
                    if (targetConstructor == null) continue;

                    Detour(targetType, "Constructor", targetConstructor, patchConstructor);
                    Loader.Log($"[PatchEngine] Patched constructor {targetConstructor.Name} of {targetType.Name}");
                }
            }
            catch (Exception ex)
            {
                Loader.Log($"[PatchEngine] Failed to patch: {ex.Message}\nat: {ex.StackTrace}");
            }
        }

        private static Dictionary<Tuple<Type, string>, DetourMaker.DetourInfo> trampolines = new Dictionary<Tuple<Type, string>, DetourMaker.DetourInfo>();

        public static void Detour(Type type, string name, MethodBase original, MethodBase replacement)
        {
            DetourMaker.DetourInfo info = DetourMaker.Detour(original, replacement);
            trampolines.Add(new Tuple<Type, string>(type, name), info);
        }

        public static T GetOriginalMethod<T>(object instance, string name)
            where T: Delegate
        {
            if (trampolines.TryGetValue(new Tuple<Type, string>(instance.GetType(), name), out var info)) {
                return Marshal.GetDelegateForFunctionPointer<T>(info.TrampolinePtr);
            }
            return FieldAccess.Get<T>(instance, name);
        }
    }
}