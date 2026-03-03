#include <windows.h>
#include <thread>
#include <cstring>
#include <cstdio>
#include "./include/MinHook.h"

HMODULE mono = nullptr;

bool g_mod_loaded = false;
HMODULE realWinHttp = nullptr;
HMODULE thisModule = nullptr;  // Store our own module handle

bool g_monoHookInstalled = false;
typedef HMODULE(WINAPI* LoadLibraryW_t)(LPCWSTR);
LoadLibraryW_t LoadLibraryW_orig = nullptr;
typedef HMODULE(WINAPI* LoadLibraryExW_t)(LPCWSTR, HANDLE, DWORD);

LoadLibraryExW_t LoadLibraryExW_orig = nullptr;

void* mono_jit_init_addr = nullptr;
void* mono_jit_init_version_addr = nullptr;
typedef void* (*mono_jit_init_t)(const char* name);
mono_jit_init_t mono_jit_init_orig = nullptr;
typedef void* (*mono_jit_init_version_t)(const char* name, const char* version);
mono_jit_init_version_t mono_jit_init_version_orig = nullptr;
typedef void* (*mono_domain_assembly_open_t)(void*, const char*);
mono_domain_assembly_open_t mono_domain_assembly_open_orig = nullptr;
typedef void* (*mono_assembly_open_full_t)(const char* filename, void* status, int refonly);
mono_assembly_open_full_t mono_assembly_open_full_orig = nullptr;
typedef void* (*mono_runtime_invoke_t)(void*, void*, void**, void**);
mono_runtime_invoke_t mono_runtime_invoke_orig = nullptr;


// Forward declarations for WinHttp functions
extern "C" {
    __declspec(dllexport) void* __stdcall WinHttpOpen(void* a, int b, void* c, void* d, int e) {
        typedef void* (__stdcall *FuncType)(void*, int, void*, void*, int);
        auto func = (FuncType)GetProcAddress(realWinHttp, "WinHttpOpen");
        return func ? func(a, b, c, d, e) : nullptr;
    }

    __declspec(dllexport) void* __stdcall WinHttpConnect(void* a, void* b, int c, int d) {
        typedef void* (__stdcall *FuncType)(void*, void*, int, int);
        auto func = (FuncType)GetProcAddress(realWinHttp, "WinHttpConnect");
        return func ? func(a, b, c, d) : nullptr;
    }

    __declspec(dllexport) void* __stdcall WinHttpOpenRequest(void* a, void* b, void* c, void* d, void* e, void* f, int g) {
        typedef void* (__stdcall *FuncType)(void*, void*, void*, void*, void*, void*, int);
        auto func = (FuncType)GetProcAddress(realWinHttp, "WinHttpOpenRequest");
        return func ? func(a, b, c, d, e, f, g) : nullptr;
    }

    __declspec(dllexport) int __stdcall WinHttpSendRequest(void* a, void* b, int c, void* d, int e, int f, void* g) {
        typedef int (__stdcall *FuncType)(void*, void*, int, void*, int, int, void*);
        auto func = (FuncType)GetProcAddress(realWinHttp, "WinHttpSendRequest");
        return func ? func(a, b, c, d, e, f, g) : 0;
    }

    __declspec(dllexport) int __stdcall WinHttpReceiveResponse(void* a, void* b) {
        typedef int (__stdcall *FuncType)(void*, void*);
        auto func = (FuncType)GetProcAddress(realWinHttp, "WinHttpReceiveResponse");
        return func ? func(a, b) : 0;
    }

    __declspec(dllexport) int __stdcall WinHttpReadData(void* a, void* b, int c, void* d) {
        typedef int (__stdcall *FuncType)(void*, void*, int, void*);
        auto func = (FuncType)GetProcAddress(realWinHttp, "WinHttpReadData");
        return func ? func(a, b, c, d) : 0;
    }

    __declspec(dllexport) int __stdcall WinHttpCloseHandle(void* a) {
        typedef int (__stdcall *FuncType)(void*);
        auto func = (FuncType)GetProcAddress(realWinHttp, "WinHttpCloseHandle");
        return func ? func(a) : 0;
    }

    // Additional commonly used WinHttp functions - generic forwarder
    #define FORWARD_WINHTTP(name) \
    __declspec(dllexport) __declspec(naked) void __stdcall name() { \
        __asm { jmp dword ptr [realWinHttp + name##_offset] } \
    }

    __declspec(dllexport) int __stdcall WinHttpGetIEProxyConfigForCurrentUser(void* a) {
        typedef int (__stdcall *FuncType)(void*);
        auto func = (FuncType)GetProcAddress(realWinHttp, "WinHttpGetIEProxyConfigForCurrentUser");
        return func ? func(a) : 0;
    }

    __declspec(dllexport) int __stdcall WinHttpGetProxyForUrl(void* a, void* b, void* c, void* d) {
        typedef int (__stdcall *FuncType)(void*, void*, void*, void*);
        auto func = (FuncType)GetProcAddress(realWinHttp, "WinHttpGetProxyForUrl");
        return func ? func(a, b, c, d) : 0;
    }

    __declspec(dllexport) int __stdcall WinHttpSetOption(void* a, int b, void* c, int d) {
        typedef int (__stdcall *FuncType)(void*, int, void*, int);
        auto func = (FuncType)GetProcAddress(realWinHttp, "WinHttpSetOption");
        return func ? func(a, b, c, d) : 0;
    }

    __declspec(dllexport) int __stdcall WinHttpQueryOption(void* a, int b, void* c, void* d) {
        typedef int (__stdcall *FuncType)(void*, int, void*, void*);
        auto func = (FuncType)GetProcAddress(realWinHttp, "WinHttpQueryOption");
        return func ? func(a, b, c, d) : 0;
    }

    __declspec(dllexport) int __stdcall WinHttpQueryHeaders(void* a, int b, void* c, void* d, void* e, void* f) {
        typedef int (__stdcall *FuncType)(void*, int, void*, void*, void*, void*);
        auto func = (FuncType)GetProcAddress(realWinHttp, "WinHttpQueryHeaders");
        return func ? func(a, b, c, d, e, f) : 0;
    }

    __declspec(dllexport) int __stdcall WinHttpQueryDataAvailable(void* a, void* b) {
        typedef int (__stdcall *FuncType)(void*, void*);
        auto func = (FuncType)GetProcAddress(realWinHttp, "WinHttpQueryDataAvailable");
        return func ? func(a, b) : 0;
    }

    __declspec(dllexport) int __stdcall WinHttpAddRequestHeaders(void* a, void* b, int c, int d) {
        typedef int (__stdcall *FuncType)(void*, void*, int, int);
        auto func = (FuncType)GetProcAddress(realWinHttp, "WinHttpAddRequestHeaders");
        return func ? func(a, b, c, d) : 0;
    }

    __declspec(dllexport) int __stdcall WinHttpSetTimeouts(void* a, int b, int c, int d, int e) {
        typedef int (__stdcall *FuncType)(void*, int, int, int, int);
        auto func = (FuncType)GetProcAddress(realWinHttp, "WinHttpSetTimeouts");
        return func ? func(a, b, c, d, e) : 0;
    }
}

bool IsMonoDLL(LPCWSTR path)
{
    if (!path) return false;

    const wchar_t* name = wcsrchr(path, L'\\');
    name = name ? name + 1 : path;

    return _wcsicmp(name, L"mono-2.0-bdwgc.dll") == 0;
}

void LoadManagedMod(void* domain)
{
    // Get path
    wchar_t dllPath[MAX_PATH];
    GetModuleFileNameW(thisModule, dllPath, MAX_PATH);
    wchar_t dllDir[MAX_PATH];
    wcscpy_s(dllDir, dllPath);
    wchar_t* lastSlash = wcsrchr(dllDir, L'\\');
    if (lastSlash) *lastSlash = L'\0';

    wchar_t modFullPath[MAX_PATH];
    wcscpy_s(modFullPath, dllDir);
    wcscat_s(modFullPath, L"\\.\\ToppleBit.Modding.dll");

    char ansiModPath[MAX_PATH];
    WideCharToMultiByte(CP_ACP, 0, modFullPath, -1, ansiModPath, MAX_PATH, nullptr, nullptr);

    wchar_t modsDir[MAX_PATH];
    wcscpy_s(modsDir, dllDir);
    wcscat_s(modsDir, L"\\.");
    char ansiModsDir[MAX_PATH];
    WideCharToMultiByte(CP_ACP, 0, modsDir, -1, ansiModsDir, MAX_PATH, nullptr, nullptr);

    // Set Mono paths
    auto mono_set_assemblies_path = (void (*)(const char*))GetProcAddress(mono, "mono_set_assemblies_path");
    if (mono_set_assemblies_path) {
        char debugSetPath[512];
        sprintf_s(debugSetPath, sizeof(debugSetPath), "[Bootstrap] Setting assemblies path:\n%s", ansiModsDir);
        //MessageBoxA(NULL, debugSetPath, "Debug", MB_OK);
        mono_set_assemblies_path(ansiModsDir);
    }

    // Get Mono functions
    auto mono_thread_attach = (void* (*)(void*))GetProcAddress(mono, "mono_thread_attach");
    auto mono_domain_assembly_open = (void* (*)(void*, const char*))GetProcAddress(mono, "mono_domain_assembly_open");
    auto mono_assembly_get_image = (void* (*)(void*))GetProcAddress(mono, "mono_assembly_get_image");
    auto mono_class_from_name = (void* (*)(void*, const char*, const char*))GetProcAddress(mono, "mono_class_from_name");
    auto mono_class_get_method_from_name = (void* (*)(void*, const char*, int))GetProcAddress(mono, "mono_class_get_method_from_name");
    auto mono_runtime_invoke = (void* (*)(void*, void*, void**, void**))GetProcAddress(mono, "mono_runtime_invoke");
    auto mono_object_to_string = (void* (*)(void*, void**))GetProcAddress(mono, "mono_object_to_string");
    auto mono_string_to_utf8 = (char* (*)(void*))GetProcAddress(mono, "mono_string_to_utf8");
    auto mono_free = (void (*)(void*))GetProcAddress(mono, "mono_free");

    if (!mono_domain_assembly_open || !mono_thread_attach ||
        !mono_assembly_get_image || !mono_class_from_name || !mono_class_get_method_from_name || !mono_runtime_invoke ||
        !mono_object_to_string || !mono_string_to_utf8) {
        MessageBoxA(NULL, "[Bootstrap] Cannot get Mono functions!", "Error", MB_OK);
        return;
    }

    // Attach current thread to Mono domain to avoid access violations
    if (mono_thread_attach) {
        mono_thread_attach(domain);
    }

    // Debug: Check if file exists
    HANDLE fileHandle = CreateFileA(ansiModPath, GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (fileHandle == INVALID_HANDLE_VALUE) {
        char errMsg[512];
        sprintf_s(errMsg, sizeof(errMsg), "[Bootstrap] File NOT FOUND:\n%s\nError code: %d", ansiModPath, GetLastError());
        MessageBoxA(NULL, errMsg, "Error", MB_OK);
        return;
    }
    CloseHandle(fileHandle);

    char dbgMsg[512];
    sprintf_s(dbgMsg, sizeof(dbgMsg), "[Bootstrap] File found:\n%s\n\nMods folder:\n%s", ansiModPath, ansiModsDir);
    //MessageBoxA(NULL, dbgMsg, "Debug Info", MB_OK);

    MessageBoxA(NULL, "[Bootstrap] About to call mono_domain_assembly_open...", "Debug", MB_OK);

    void* assembly = nullptr;
    __try {
        assembly = mono_domain_assembly_open(domain, ansiModPath);
    }
    __except (EXCEPTION_EXECUTE_HANDLER) {
        char crashMsg[512];
        sprintf_s(crashMsg, sizeof(crashMsg), "[Bootstrap] EXCEPTION in mono_domain_assembly_open!\nException code: 0x%X", GetExceptionCode());
        MessageBoxA(NULL, crashMsg, "Error", MB_OK);
        return;
    }

    if (!assembly) {
        MessageBoxA(NULL, "[Bootstrap] mono_domain_assembly_open returned NULL!\nNo exception, but assembly is NULL.", "Error", MB_OK);
        return;
    }

    // Resolve image, class, method and invoke
    void* image = mono_assembly_get_image(assembly);
    if (!image) {
        MessageBoxA(NULL, "[Bootstrap] mono_assembly_get_image returned NULL!", "Error", MB_OK);
        return;
    }

    // Adjust namespace/class/method if you change the target assembly
    const char* targetNamespace = "ToppleBitModding";  // for ToppleBit.Modding.dll
    const char* targetClass = "Loader";                // Loader class
    const char* targetMethod = "Init";                 // static Init method

    void* klass = mono_class_from_name(image, targetNamespace, targetClass);
    if (!klass) {
        MessageBoxA(NULL, "[Bootstrap] mono_class_from_name returned NULL!", "Error", MB_OK);
        return;
    }

    void* method = mono_class_get_method_from_name(klass, targetMethod, 0);
    if (!method) {
        MessageBoxA(NULL, "[Bootstrap] mono_class_get_method_from_name returned NULL!", "Error", MB_OK);
        return;
    }

    void* exc = nullptr;
    mono_runtime_invoke(method, nullptr, nullptr, &exc);
    if (exc) {
        const char* detail = "(no details)";
        char* utf8 = nullptr;
        void* excStr = mono_object_to_string(exc, nullptr);
        if (excStr) {
            utf8 = mono_string_to_utf8(excStr);
            if (utf8) detail = utf8;
        }

        char errMsg[2048];
        int written = snprintf(errMsg, sizeof(errMsg), "[Bootstrap] mono_runtime_invoke threw managed exception!\nMessage: %s", detail);
        if (written < 0 || written >= static_cast<int>(sizeof(errMsg))) {
            // Truncate safely if too long
            const char* suffix = "\n[truncated]";
            size_t baseLen = sizeof(errMsg) - strlen(suffix) - 1;
            if (baseLen > 0) {
                memcpy(errMsg + baseLen, suffix, strlen(suffix) + 1);
            }
            else {
                strcpy_s(errMsg, sizeof(errMsg), "[Bootstrap] mono_runtime_invoke threw managed exception! [truncated]");
            }
        }

        MessageBoxA(NULL, errMsg, "Error", MB_OK);

        if (utf8 && mono_free) mono_free(utf8);
        return;
    }

    MessageBoxA(NULL, "[Bootstrap] Assembly loaded and Init() invoked successfully!", "Success", MB_OK);
}

void* hooked_mono_domain_assembly_open(void* domain, const char* name)
{
    MessageBoxA(NULL, "Running mono domain assembly open", "INFO", MB_OK);
    static bool injected = false;

    if (!injected && name)
    {
        if (strstr(name, "Assembly-CSharp.dll"))
        {
            injected = true;

            LoadManagedMod(domain);
        }
    }

    return mono_domain_assembly_open_orig(domain, name);
}

void* hooked_mono_jit_init(const char* name)
{
    MessageBoxA(NULL, "Running JIT", "INFO", MB_OK);
    void* domain = mono_jit_init_orig(name);

    if (!domain)
        MessageBoxA(NULL, "[Bootstrap] Cannot get root domain!", "Error", MB_OK);
        return nullptr;

    LoadManagedMod(domain);
    return domain;
}

void* hooked_mono_jit_init_version(const char* name, const char* version)
{
    MessageBoxA(NULL, "Running JIT version", "INFO", MB_OK);
    void* domain = mono_jit_init_version_orig(name, version);

    if (!domain)
        MessageBoxA(NULL, "[Bootstrap] Cannot get root domain!", "Error", MB_OK);
    return nullptr;

    LoadManagedMod(domain);
    return domain;
}

void* hooked_mono_assembly_open_full(
    const char* filename,
    void* status,
    int refonly)
{
    MessageBoxA(NULL, "Running open full", "INFO", MB_OK);
    static bool injected = false;

    if (!injected && filename)
    {
        if (strstr(filename, "Assembly-CSharp.dll"))
        {
            injected = true;

            // 🔥 EARLIEST SAFE POINT
            void* domain = nullptr;

            auto mono_domain_get =
                (void* (*)())GetProcAddress(mono, "mono_domain_get");

            if (mono_domain_get)
                domain = mono_domain_get();

            if (domain)
                LoadManagedMod(domain);
        }
    }

    return mono_assembly_open_full_orig(filename, status, refonly);
}

void* hooked_mono_runtime_invoke(void* method, void* obj, void** params, void** exc)
{
    MessageBoxA(NULL, "[Hook] mono_runtime_invoke called", "INFO", MB_OK);

    // Call the original function
    void* result = mono_runtime_invoke_orig(method, obj, params, exc);

    // Check for exceptions
    if (exc && *exc) {
        MessageBoxA(NULL, "[Hook] Exception detected in mono_runtime_invoke", "WARNING", MB_OK);
    }

    auto mono_domain_get = (void* (*)())GetProcAddress(mono, "mono_domain_get");
    if (mono_domain_get) {
        void* domain = mono_domain_get();
        if (domain) {
            LoadManagedMod(domain);
        }
    }
    return result;
}

/*void* hooked_mono_jit_init(const char* name)
{
    MessageBoxA(NULL, "[Bootstrap] Hooked", "Info", MB_OK);
    void* domain = mono_jit_init_orig(name);

    if (g_mod_loaded) {
        MessageBoxA(NULL, "[Bootstrap] GLoaded", "Warning", MB_OK);
        return domain;
    }

    if (!domain) {
        MessageBoxA(NULL, "[Bootstrap] Cannot get root domain!", "Error", MB_OK);
        return nullptr;
    }

    // Attach thread
    auto mono_thread_attach =
        (void* (*)(void*))GetProcAddress(mono, "mono_thread_attach");

    if (mono_thread_attach)
        mono_thread_attach(domain);

    // Load your managed DLL EARLY
    LoadManagedMod(domain);

    return domain;
}*/

void InstallMonoHooks()
{
    if (g_monoHookInstalled || !mono)
        return;

    auto assembly_open_full_addr = GetProcAddress(mono, "mono_domain_assembly_open");
    if (assembly_open_full_addr)
    {
        MH_CreateHook(
            assembly_open_full_addr,
            &hooked_mono_assembly_open_full,
            reinterpret_cast<void**>(&mono_assembly_open_full_orig)
        );

        MH_EnableHook(assembly_open_full_addr);
    }
    else {
        MessageBoxA(NULL, "mono_domain_assembly_open not found", "Error", MB_OK);
    }


    auto assembly_open_addr = GetProcAddress(mono, "mono_domain_assembly_open");
    if (assembly_open_addr)
    {
        MH_CreateHook(
            assembly_open_addr,
            &hooked_mono_domain_assembly_open,
            reinterpret_cast<void**>(&mono_domain_assembly_open_orig)
        );

        MH_EnableHook(assembly_open_addr);
    }
    else {
        MessageBoxA(NULL, "mono_domain_assembly_open not found", "Error", MB_OK);
    }


    mono_jit_init_addr =
        GetProcAddress(mono, "mono_jit_init");

    mono_jit_init_version_addr =
        GetProcAddress(mono, "mono_jit_init_version");

    if (mono_jit_init_addr)
    {
        MH_CreateHook(
            mono_jit_init_addr,
            &hooked_mono_jit_init,
            reinterpret_cast<void**>(&mono_jit_init_orig)
        );
        MH_EnableHook(mono_jit_init_addr);
    }
    else {
        MessageBoxA(NULL, "Missing jit init", "ERROR", MB_OK);
    }

    if (mono_jit_init_version_addr)
    {
        MH_CreateHook(
            mono_jit_init_version_addr,
            &hooked_mono_jit_init_version,
            reinterpret_cast<void**>(&mono_jit_init_version_orig)
        );
        MH_EnableHook(mono_jit_init_version_addr);
    }
    else {
        MessageBoxA(NULL, "Missing jit init version", "ERROR", MB_OK);
    }

    auto mono_runtime_invoke_addr = GetProcAddress(mono, "mono_runtime_invoke");
    if (mono_runtime_invoke_addr)
    {
        MH_CreateHook(
            mono_runtime_invoke_addr,
            &hooked_mono_runtime_invoke,
            reinterpret_cast<void**>(&mono_runtime_invoke_orig)
        );
        MH_EnableHook(mono_runtime_invoke_addr);
    }
    else {
        MessageBoxA(NULL, "Missing mono_runtime_invoke", "ERROR", MB_OK);
    }

    g_monoHookInstalled = true;
}

/*HMODULE WINAPI hooked_LoadLibraryW(LPCWSTR lpLibFileName)
{
    HMODULE mod = LoadLibraryW_orig(lpLibFileName);

    if (lpLibFileName && !g_monoHookInstalled)
    {
        const wchar_t* name = wcsrchr(lpLibFileName, L'\\');
        name = name ? name + 1 : lpLibFileName;

        if (_wcsicmp(name, L"mono-2.0-bdwgc.dll") == 0 ||
            _wcsicmp(name, L"mono.dll") == 0) {

            g_monoHookInstalled = true;
            mono = mod;
            mono_jit_init_addr = GetProcAddress(mono, "mono_jit_init");

            if (mono_jit_init_addr) {
                MessageBoxA(NULL, "[Bootstrap] Load Lib (prehook)!", "Info", MB_OK);
                MH_CreateHook(
                    mono_jit_init_addr,
                    &hooked_mono_jit_init,
                    reinterpret_cast<void**>(&mono_jit_init_orig)
                );
                MH_EnableHook(mono_jit_init_addr);
                MessageBoxA(NULL, "[Bootstrap] Load Lib (posthook)!", "Info", MB_OK);
                MH_DisableHook(&LoadLibraryW);
            }
            else {
                MessageBoxA(NULL, "[Boostrap] No mono jit init found", "Error", MB_OK);
            }
        }
    }

    return mod;
}*/

HMODULE WINAPI hooked_LoadLibraryW(LPCWSTR lpLibFileName)
{
    HMODULE mod = LoadLibraryW_orig(lpLibFileName);

    if (IsMonoDLL(lpLibFileName))
    {
        mono = mod;
        InstallMonoHooks();
    }

    return mod;
}

HMODULE WINAPI hooked_LoadLibraryExW(
    LPCWSTR lpLibFileName,
    HANDLE hFile,
    DWORD dwFlags)
{
    HMODULE mod =
        LoadLibraryExW_orig(lpLibFileName, hFile, dwFlags);

    if (IsMonoDLL(lpLibFileName))
    {
        mono = mod;
        InstallMonoHooks();
    }

    return mod;
}

void Bootstrap()
{
    MH_Initialize();

    HMODULE kernel32 = GetModuleHandleA("kernel32.dll");
    if (!kernel32)
    {
        MessageBoxA(NULL, "kernel32 not found", "Error", MB_OK);
        return;
    }

    LoadLibraryW_orig = (LoadLibraryW_t)GetProcAddress(kernel32, "LoadLibraryW");
    if (!LoadLibraryW_orig)
    {
        MessageBoxA(NULL, "LoadLibraryW not found", "Error", MB_OK);
        return;
    }

    LoadLibraryExW_orig = (LoadLibraryExW_t)GetProcAddress(kernel32, "LoadLibraryExW");
    if (!LoadLibraryExW_orig)
    {
        MessageBoxA(NULL, "LoadLibraryW not found", "Error", MB_OK);
        return;
    }


    MH_CreateHook(
        &LoadLibraryW,
        &hooked_LoadLibraryW,
        reinterpret_cast<void**>(&LoadLibraryW_orig)
    );

    MH_CreateHook(
        LoadLibraryExW_orig,
        &hooked_LoadLibraryExW,
        reinterpret_cast<void**>(&LoadLibraryExW_orig)
    );


    MH_EnableHook(MH_ALL_HOOKS);

    HMODULE monoNow = GetModuleHandleW(L"mono-2.0-bdwgc.dll");
    if (monoNow)
    {
        mono = monoNow;
        InstallMonoHooks();
    }
}

BOOL WINAPI DllMain(HINSTANCE hinst, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        thisModule = hinst;  // Store our module handle
        DisableThreadLibraryCalls(hinst);

        // Load real system winhttp
        wchar_t path[MAX_PATH];
        GetSystemDirectoryW(path, MAX_PATH);
        wcscat_s(path, L"\\winhttp.dll");

        realWinHttp = LoadLibraryW(path);

        if (!realWinHttp)
        {
            MessageBoxA(NULL, "Failed to load real winhttp.dll", "Error", MB_OK);
            return FALSE;
        }


        CreateThread(nullptr, 0,
            (LPTHREAD_START_ROUTINE)Bootstrap,
            nullptr, 0, nullptr);
        // Run bootstrap in a new thread cause we still want unity to run behind the scene and load mono.dll
        // std::thread(Bootstrap).detach();
    }
    return TRUE;
}
