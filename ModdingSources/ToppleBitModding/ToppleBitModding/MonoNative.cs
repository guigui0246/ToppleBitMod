using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ToppleBitModding
{
    static class MonoNative
    {
        private const string MONO = "mono-2.0-bdwgc"; // Unity loads this
        [DllImport(MONO, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr mono_method_get_unmanaged_thunk(IntPtr method);

        [DllImport(MONO)]
        public static extern IntPtr mono_domain_get();

        [DllImport(MONO)]
        public static extern IntPtr mono_domain_assembly_open(
            IntPtr domain, string name);

        [DllImport(MONO)]
        public static extern IntPtr mono_assembly_get_image(
            IntPtr assembly);

        [DllImport(MONO)]
        public static extern IntPtr mono_class_from_name(
            IntPtr image, string @namespace, string name);

        [DllImport(MONO)]
        public static extern IntPtr mono_class_get_method_from_name(
            IntPtr klass, string name, int paramCount);

        [DllImport(MONO)]
        public static extern IntPtr mono_compile_method(
            IntPtr method);

        public static IntPtr GetNativeMethodPointer(
            string assemblyName,
            string @namespace,
            string className,
            string methodName,
            int paramCount)
        {
            IntPtr domain = MonoNative.mono_domain_get();
            if (domain == IntPtr.Zero)
                throw new Exception("Mono domain not found");

            IntPtr asm = MonoNative.mono_domain_assembly_open(domain, assemblyName);
            if (asm == IntPtr.Zero)
                throw new Exception("Assembly not found");

            IntPtr image = MonoNative.mono_assembly_get_image(asm);
            IntPtr klass = MonoNative.mono_class_from_name(image, @namespace, className);
            if (klass == IntPtr.Zero)
                throw new Exception("Class not found");

            IntPtr method = MonoNative.mono_class_get_method_from_name(
                klass, methodName, paramCount);

            if (method == IntPtr.Zero)
                throw new Exception("Method not found");

            // Force JIT + get native pointer
            return MonoNative.mono_compile_method(method);
        }
        public static IntPtr GetNativePtr(MethodBase method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            // Unity Mono stores MonoMethod* inside MethodHandle.Value
            var handle = method.MethodHandle;

            // MethodHandle.Value is actually a pointer in Unity Mono
            IntPtr monoMethodPtr = handle.Value;

            // Compile JIT and get native pointer
            return MonoNative.mono_compile_method(monoMethodPtr);
        }
    }
}
