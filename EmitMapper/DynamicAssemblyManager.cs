﻿namespace EmitMapper;

using System;
using System.Reflection;
using System.Reflection.Emit;

using EmitMapper.Mappers;

/// <summary>
///     Class which maintains an assembly for created object Mappers
/// </summary>
public class DynamicAssemblyManager
{
    /// <summary>
    ///     Saves assembly with created Mappers to file. This method is useful for debugging purpose.
    /// </summary>
    public static void SaveAssembly()
    {
        lock (typeof(DynamicAssemblyManager))
        {
            throw new NotImplementedException("DynamicAssemblyManager.SaveAssembly");
            //assemblyBuilder.Save(assemblyName.Name + ".dll");
        }
    }

    #region Non-public members

    private static readonly AssemblyName _AssemblyName;

    private static readonly AssemblyBuilder _AssemblyBuilder;

    private static readonly ModuleBuilder _ModuleBuilder;

    static DynamicAssemblyManager()
    {
        var curAssemblyName = Assembly.GetExecutingAssembly().GetName();

#if !SILVERLIGHT
        _AssemblyName = new AssemblyName("EmitMapperAssembly");
        _AssemblyName.SetPublicKey(curAssemblyName.GetPublicKey());
        _AssemblyName.SetPublicKeyToken(curAssemblyName.GetPublicKeyToken());
        _AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(_AssemblyName, AssemblyBuilderAccess.RunAndCollect);

        _ModuleBuilder = _AssemblyBuilder.DefineDynamicModule(_AssemblyName.Name + ".dll");
#else
			assemblyName = new AssemblyName("EmitMapperAssembly.SL");
			assemblyName.KeyPair = kp;
			assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
				  assemblyName,
				  AssemblyBuilderAccess.Run
				  );
			moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, true);
#endif
    }

    private static string CorrectTypeName(string typeName)
    {
        if (typeName.Length >= 1042)
            typeName = "type_" + typeName.Substring(0, 900) + Guid.NewGuid().ToString().Replace("-", "");
        return typeName;
    }

    internal static TypeBuilder DefineMapperType(string typeName)
    {
        lock (typeof(DynamicAssemblyManager))
        {
            return _ModuleBuilder.DefineType(
                CorrectTypeName(typeName + Guid.NewGuid().ToString().Replace("-", "")),
                TypeAttributes.Public,
                typeof(MapperForClassImpl),
                null);
        }
    }

    internal static TypeBuilder DefineType(string typeName, Type parent)
    {
        lock (typeof(DynamicAssemblyManager))
        {
            return _ModuleBuilder.DefineType(CorrectTypeName(typeName), TypeAttributes.Public, parent, null);
        }
    }

    #endregion
}