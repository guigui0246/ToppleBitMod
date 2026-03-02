# ToopleBitMod
Mod Setup for ToppleBit (**WINDOWS ONLY**)

Copy the contents of this folder into your ToppleBit installation folder.
Put your mods in the `Mods` folder.

`[Patch(typeof(SomeClass))]` to patch a class.
`[Patch(typeof(SomeClass), int)]` to patch a class AND give an order for the call of the Awake function.

`Constructor` functions patch will **replace** the original constructor.

`PatchEngine.GetOriginalMethod(instance, "SomeMethod")` to get the original method of a function.

`FieldAccess.Get<T>(instance, "fieldName")` to get the value of a field or method.
`FieldAccess.Set(instance, "fieldName", value)` to set the value of a field.

**Due to unity's loading order**:
`Awake` functions patch won't **replace** the original `Awake` functions.
They will be called in addition after **ALL** the original `Awake` functions.
