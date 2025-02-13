using System.Reflection.Emit;
using System.Reflection;

namespace RubbleAutoloader;

internal static class ClassBuilder
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

    public static object CreateDynamic(object sourceObject, string name, out Type outType)
    {
        TypeAttributes attributes = TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass;
        TypeBuilder typeBuilder = GetModule().DefineType(name, attributes, sourceObject.GetType());

        Type dynamicType = typeBuilder.CreateType();

        outType = dynamicType;
        return Activator.CreateInstance(dynamicType);
    }
}
