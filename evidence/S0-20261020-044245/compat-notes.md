# compat-notes — Harmony patches present at startup

Generated: 2026-10-20 04:53:59
Process: 476
Total patched methods: 32

| # | Declaring type | Method | Owners | Prefix | Postfix | Transpiler | Finalizer |
|---|---|---|---|---|---|---|---|
| 1 | AsmResolver.Utf8String | Concat | MelonLoader | 1 | 0 | 0 | 0 |
| 2 | HarmonyLib.PatchFunctions | ReversePatch | MelonLoader | 1 | 0 | 0 | 0 |
| 3 | HarmonyLib.PatchProcessor | Patch | MelonLoader | 1 | 0 | 0 | 0 |
| 4 | HarmonyLib.PatchProcessor | Unpatch | MelonLoader | 1 | 0 | 0 | 0 |
| 5 | HarmonyLib.PatchProcessor | Unpatch | MelonLoader | 1 | 0 | 0 | 0 |
| 6 | Il2Cpp.GameController | Update | longyin.coop.probe | 1 | 0 | 0 | 0 |
| 7 | Il2CppInterop.Generator.Contexts.MethodRewriteContext | UnmangleMethodNameWithSignature | MelonLoader | 1 | 0 | 0 | 0 |
| 8 | Il2CppInterop.Generator.Contexts.RewriteGlobalContext | AddAssemblyContext | MelonLoader | 0 | 1 | 0 | 0 |
| 9 | Il2CppInterop.Generator.Contexts.RewriteGlobalContext | Dispose | MelonLoader | 1 | 0 | 0 | 0 |
| 10 | Il2CppInterop.Generator.Contexts.RewriteGlobalContext | GetNewAssemblyForOriginal | MelonLoader | 1 | 0 | 0 | 0 |
| 11 | Il2CppInterop.Generator.Contexts.RewriteGlobalContext | TryGetNewTypeForOriginal | MelonLoader | 1 | 0 | 0 | 0 |
| 12 | Il2CppInterop.Generator.Extensions.ILGeneratorEx | EmitObjectToPointer | MelonLoader | 1 | 0 | 0 | 0 |
| 13 | Il2CppInterop.HarmonySupport.Il2CppDetourMethodPatcher | ReportException | MelonLoader | 1 | 0 | 0 | 0 |
| 14 | Il2CppInterop.Runtime.Injection.ClassInjector | ConvertMethodInfo | MelonLoader | 0 | 0 | 1 | 0 |
| 15 | Il2CppInterop.Runtime.Injection.ClassInjector | IsTypeSupported | MelonLoader | 0 | 0 | 1 | 0 |
| 16 | Il2CppInterop.Runtime.Injection.ClassInjector | RegisterTypeInIl2Cpp | MelonLoader | 0 | 0 | 1 | 0 |
| 17 | Il2CppInterop.Runtime.Injection.ClassInjector | RewriteType | MelonLoader | 1 | 0 | 0 | 0 |
| 18 | Il2CppInterop.Runtime.Injection.InjectorHelpers | Setup | MelonLoader | 0 | 0 | 1 | 0 |
| 19 | MonoMod.RuntimeDetour.DetourContext | Dispose | MelonLoader | 0 | 0 | 1 | 0 |
| 20 | System.Diagnostics.Process | get_MainWindowHandle | MelonLoader | 1 | 0 | 0 | 0 |
| 21 | System.Diagnostics.Process | get_MainWindowTitle | MelonLoader | 1 | 0 | 0 | 0 |
| 22 | System.Reflection.Assembly | Load | MelonLoader | 1 | 0 | 0 | 0 |
| 23 | System.Reflection.Assembly | LoadFile | MelonLoader | 1 | 0 | 0 | 0 |
| 24 | System.Runtime.Loader.AssemblyLoadContext | LoadFromPath | MelonLoader | 1 | 0 | 0 | 0 |
| 25 | System.Runtime.Loader.AssemblyLoadContext | LoadFromStream | MelonLoader | 1 | 0 | 0 | 0 |
| 26 | System.Runtime.Loader.AssemblyLoadContext | ValidateAssemblyNameWithSimpleName | MelonLoader | 1 | 0 | 0 | 0 |
| 27 | System.Threading.Thread | get_CurrentCulture | MelonLoader | 1 | 0 | 0 | 0 |
| 28 | System.Threading.Thread | get_CurrentUICulture | MelonLoader | 1 | 0 | 0 | 0 |
| 29 | System.Threading.Thread | set_CurrentCulture | MelonLoader | 1 | 0 | 0 | 0 |
| 30 | System.Threading.Thread | set_CurrentUICulture | MelonLoader | 1 | 0 | 0 | 0 |
| 31 | UnityEngine.SceneManagement.SceneManager | Internal_SceneLoaded | MelonLoader | 1 | 0 | 0 | 0 |
| 32 | UnityEngine.SceneManagement.SceneManager | Internal_SceneUnloaded | MelonLoader | 1 | 0 | 0 | 0 |

## Notes
- อ่านอย่างเดียว ไม่ได้แก้/ถอด patch ของ mod อื่น
- ใช้ตรวจการชนกับ LongYinThai (โดยเฉพาะ AddInfoText และ setter ของ Text) ก่อนขึ้น S1
