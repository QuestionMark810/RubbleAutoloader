using System.Reflection.Emit;
using System.Reflection;

namespace RubbleAutoloader;

internal static class TileTypeBuilder
{
    private static AssemblyBuilder _assemblyBuilder;
    private static ModuleBuilder _moduleBuilder;

    private static ModuleBuilder GetModule()
    {
        if (_assemblyBuilder == null)
            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new("AutoAssembly"), AssemblyBuilderAccess.Run);

        if (_moduleBuilder == null)
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule("AutoModule");

        return _moduleBuilder;
    }

    /// <summary> Dynamically creates a Type based on <paramref name="sourceObject"/>, specifically for a rubble tile. </summary>
    /// <returns> An instance of the type created. </returns>
    internal static object CreateDynamic(object sourceObject, string sourceName, string textureOverride, out Type outType)
    {
        TypeAttributes attributes = TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass;
        TypeBuilder typeBuilder = GetModule().DefineType(sourceName + "Rubble", attributes, sourceObject.GetType());

        OverrideSig(typeBuilder, textureOverride);

        Type dynamicType = typeBuilder.CreateType();
        outType = dynamicType;

        return Activator.CreateInstance(dynamicType);

        static void OverrideSig(TypeBuilder t, string value) //Overrides ModTexturedType.Texture.get
        {
            MethodInfo a = typeof(ModTexturedType).GetMethod("get_Texture");
            MethodBuilder b = t.DefineMethod("get_Texture", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual, typeof(string), null);

            var il = b.GetILGenerator();

            il.Emit(OpCodes.Ldstr, value);
            il.Emit(OpCodes.Ret);

            t.DefineMethodOverride(b, a);
        }
    }
}
