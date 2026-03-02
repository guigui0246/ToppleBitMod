using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ToppleBitModding
{
    public static class FieldAccess
    {
        private static readonly Dictionary<(Type, string), FieldInfo> _fieldCache
            = new Dictionary<(Type, string), FieldInfo>();

        private static readonly Dictionary<(Type, string), MethodInfo> _methodCache
            = new Dictionary<(Type, string), MethodInfo>();

        private static FieldInfo GetField(Type type, string fieldName)
        {
            var key = (type, fieldName);

            if (_fieldCache.TryGetValue(key, out var field))
                return field;

            field = type.GetField(
                fieldName,
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (field == null)
                throw new MissingFieldException(type.FullName, fieldName);

            _fieldCache[key] = field;
            return field;
        }

        private static MethodInfo GetMethod(Type type, string methodName)
        {
            var key = (type, methodName);

            if (_methodCache.TryGetValue(key, out var method))
                return method;

            method = type.GetMethod(
                methodName,
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (method == null)
                throw new MissingMethodException(type.FullName, methodName);

            _methodCache[key] = method;
            return method;
        }

        public static T Get<T>(object instance, string fieldName)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            Type type = instance.GetType();
            try
            {
                FieldInfo field = GetField(type, fieldName);
                return (T)field.GetValue(instance);
            }
            catch (MissingFieldException)
            {
                try
                {
                    MethodInfo method = GetMethod(type, fieldName);
                    
                    if (!typeof(Delegate).IsAssignableFrom(typeof(T)))
                        throw new InvalidOperationException("T must be a delegate type when retrieving a method.");

                    if (!method.IsStatic)
                        return (T)(object)method.CreateDelegate(typeof(T), instance);

                    return (T)(object)method.CreateDelegate(typeof(T));
                }
                catch (MissingMethodException)
                {
                }
            }
            throw new MissingMemberException(type.FullName, fieldName);
        }

        public static void Set<T>(object instance, string fieldName, T value)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var field = GetField(instance.GetType(), fieldName);
            field.SetValue(instance, value);
        }
    }
}
