using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ToppleBitModding;

class DetourMaker {

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAlloc(
        IntPtr lpAddress,
        IntPtr dwSize,
        uint flAllocationType,
        uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualProtect(
        IntPtr lpAddress,
        UIntPtr dwSize,
        uint flNewProtect,
        out uint lpflOldProtect);

    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint PAGE_EXECUTE_READWRITE = 0x40;
    
    public class DetourInfo
    {
        public IntPtr OriginalPtr;
        public IntPtr TrampolinePtr;
        public byte[] OriginalBytes;
    }

    public static unsafe DetourInfo Detour(MethodBase original, MethodBase replacement)
    {
        RuntimeHelpers.PrepareMethod(original.MethodHandle);
        RuntimeHelpers.PrepareMethod(replacement.MethodHandle);

        IntPtr oriPtr = MonoNative.GetNativePtr(original);
        IntPtr repPtr = MonoNative.GetNativePtr(replacement);

        const int patchSize = 12; // x64 absolute jump size

        byte* pOriginal = (byte*)oriPtr;

        // Save original bytes
        byte[] stolenBytes = new byte[patchSize];
        for (int i = 0; i < patchSize; i++)
            stolenBytes[i] = pOriginal[i];

        // Allocate trampoline
        IntPtr trampPtr = VirtualAlloc(
            IntPtr.Zero,
            new IntPtr(patchSize + 12),
            MEM_COMMIT | MEM_RESERVE,
            PAGE_EXECUTE_READWRITE);

        byte* pTramp = (byte*)trampPtr;

        // Copy stolen bytes into trampoline
        for (int i = 0; i < patchSize; i++)
            pTramp[i] = stolenBytes[i];

        // Append jump back to original + patchSize
        byte* jumpBack = pTramp + patchSize;

        jumpBack[0] = 0x48;
        jumpBack[1] = 0xB8;
        *(ulong*)(jumpBack + 2) = (ulong)(oriPtr.ToInt64() + patchSize);
        jumpBack[10] = 0xFF;
        jumpBack[11] = 0xE0;

        // Now patch original to jump to replacement
        ProtectRWX(oriPtr, patchSize);

        pOriginal[0] = 0x48;
        pOriginal[1] = 0xB8;
        *(ulong*)(pOriginal + 2) = (ulong)repPtr.ToInt64();
        pOriginal[10] = 0xFF;
        pOriginal[11] = 0xE0;

        return new DetourInfo
        {
            OriginalPtr = oriPtr,
            TrampolinePtr = trampPtr,
            OriginalBytes = stolenBytes
        };
    }

    private static void ProtectRWX(IntPtr addr, int size)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            VirtualProtect(addr, (UIntPtr)size, 0x40, out _);
        }
    }

}