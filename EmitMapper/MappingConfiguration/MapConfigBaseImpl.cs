﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EmitMapper.Conversion;
using EmitMapper.MappingConfiguration.MappingOperations;
using EmitMapper.MappingConfiguration.MappingOperations.Interfaces;
using EmitMapper.Utils;

namespace EmitMapper.MappingConfiguration;

public abstract class MapConfigBaseImpl : IMappingConfigurator
{
    private readonly TypeDictionary<Delegate> _customConstructors = new();

    private readonly TypeDictionary<Delegate> _customConverters = new();

    private readonly TypeDictionary<Delegate> _destinationFilters = new();

    private readonly TypeDictionary<Delegate> _nullSubstitutors = new();

    private readonly TypeDictionary<Delegate> _postProcessors = new();

    private readonly TypeDictionary<Delegate> _sourceFilters = new();

    private readonly TypeDictionary<ICustomConverterProvider> _customConvertersGeneric = new();

    private readonly TypeDictionary<List<string>> _ignoreMembers = new();

    private string _configurationName;

    public MapConfigBaseImpl()
    {
        RegisterDefaultCollectionConverters();
    }

    public abstract IMappingOperation[] GetMappingOperations(Type from, Type to);

    public virtual string GetConfigurationName()
    {
        return _configurationName;
    }

    public virtual StaticConvertersManager GetStaticConvertersManager()
    {
        return null;
    }

    public virtual IRootMappingOperation GetRootMappingOperation(Type from, Type to)
    {
        var converter = _customConverters.GetValue(new[] { from, to }) ?? GetGenericConverter(from, to);

        return new RootMappingOperation(from, to)
        {
            TargetConstructor = _customConstructors.GetValue(new[] { to }),
            NullSubstitutor = _nullSubstitutors.GetValue(new[] { to }),
            ValuesPostProcessor = _postProcessors.GetValue(new[] { to }),
            Converter = converter,
            DestinationFilter = _destinationFilters.GetValue(new[] { to }),
            SourceFilter = _sourceFilters.GetValue(new[] { from })
        };
    }

    protected static string ToStrEnum<T>(IEnumerable<T> t)
    {
        return t == null ? "" : t.ToCsv("|");
    }

    protected static string ToStr<T>(T t)
        where T : class
    {
        return t == null ? "" : t.ToString();
    }

    public virtual void BuildConfigurationName()
    {
        _configurationName = new[]
        {
            ToStr(_customConverters), ToStr(_nullSubstitutors),
            ToStr(_ignoreMembers), ToStr(_postProcessors),
            ToStr(_customConstructors)
        }.ToCsv(";");
    }

    /// <summary>
    ///     Define custom type converter
    /// </summary>
    /// <typeparam name="TFrom">Source type</typeparam>
    /// <typeparam name="To">Destination type</typeparam>
    /// <param name="converter">Function which converts an instance of the source type to an instance of the destination type</param>
    /// <returns></returns>
    public IMappingConfigurator ConvertUsing<TFrom, To>(Func<TFrom, To> converter)
    {
        _customConverters.Add(
            new[] { Meta<TFrom>.Type, Meta<To>.Type },
            (ValueConverter<TFrom, To>)((v, s) => converter(v)));
        return this;
    }

    /// <summary>
    ///     Define conversion for a generic. It is able to convert not one particular class but all generic family
    ///     providing a generic converter.
    /// </summary>
    /// <param name="from">Type of source. Can be also generic class or abstract array.</param>
    /// <param name="to">Type of destination. Can be also generic class or abstract array.</param>
    /// <param name="converterProvider">Provider for getting detailed information about generic conversion</param>
    /// <returns></returns>
    public IMappingConfigurator ConvertGeneric(Type from, Type to, ICustomConverterProvider converterProvider)
    {
        _customConvertersGeneric.Add(new[] { from, to }, converterProvider);
        return this;
    }

    /// <summary>
    ///     Setup function which returns value for destination if appropriate source member is null.
    /// </summary>
    /// <typeparam name="TFrom">Type of source member</typeparam>
    /// <typeparam name="TTo">Type of destination member</typeparam>
    /// <param name="nullSubstitutor">Function which returns value for destination if appropriate source member is null</param>
    /// <returns></returns>
    public IMappingConfigurator NullSubstitution<TFrom, TTo>(Func<object, TTo> nullSubstitutor)
    {
        _nullSubstitutors.Add(new[] { Meta<TFrom>.Type, Meta<TTo>.Type }, nullSubstitutor);
        return this;
    }

    /// <summary>
    ///     Define members which should be ignored
    /// </summary>
    /// <param name="typeFrom">Source type for which ignore members are defining</param>
    /// <param name="typeTo">Destination type for which ignore members are defining</param>
    /// <param name="ignoreNames">Array of member names which should be ignored</param>
    /// <returns></returns>
    public IMappingConfigurator IgnoreMembers(Type typeFrom, Type typeTo, string[] ignoreNames)
    {
        var ig = _ignoreMembers.GetValue(new[] { typeFrom, typeTo });
        if (ig == null)
            _ignoreMembers.Add(new[] { typeFrom, typeTo }, ignoreNames.ToList());
        else
            ig.AddRange(ignoreNames);
        return this;
    }

    /// <summary>
    ///     Define members which should be ignored
    /// </summary>
    /// <typeparam name="TFrom">Source type for which ignore members are defining</typeparam>
    /// <typeparam name="TTo">Destination type for which ignore members are defining</typeparam>
    /// <param name="ignoreNames">Array of member names which should be ignored</param>
    /// <returns></returns>
    public IMappingConfigurator IgnoreMembers<TFrom, TTo>(string[] ignoreNames)
    {
        return IgnoreMembers(Meta<TFrom>.Type, Meta<TTo>.Type, ignoreNames);
    }

    /// <summary>
    ///     Define a custom constructor for the specified type
    /// </summary>
    /// <typeparam name="T">Type for which constructor is defining</typeparam>
    /// <param name="constructor">Custom constructor</param>
    /// <returns></returns>
    public IMappingConfigurator ConstructBy<T>(TargetConstructor<T> constructor)
    {
        _customConstructors.Add(new[] { Meta<T>.Type }, constructor);
        return this;
    }

    /// <summary>
    ///     Define postprocessor for specified type
    /// </summary>
    /// <typeparam name="T">Objects of this type and all it's descendants will be postprocessed</typeparam>
    /// <param name="postProcessor"></param>
    /// <returns></returns>
    public IMappingConfigurator PostProcess<T>(ValuesPostProcessor<T> postProcessor)
    {
        _postProcessors.Add(new[] { Meta<T>.Type }, postProcessor);
        return this;
    }

    /// <summary>
    ///     Set unique configuration name to force Emit Mapper create new mapper instead using appropriate cached one.
    /// </summary>
    /// <param name="configurationName">Configuration name</param>
    /// <returns></returns>
    public IMappingConfigurator SetConfigName(string configurationName)
    {
        _configurationName = configurationName;
        return this;
    }

    public IMappingConfigurator FilterDestination<T>(ValuesFilter<T> valuesFilter)
    {
        _destinationFilters.Add(new[] { Meta<T>.Type }, valuesFilter);
        return this;
    }

    public IMappingConfigurator FilterSource<T>(ValuesFilter<T> valuesFilter)
    {
        _sourceFilters.Add(new[] { Meta<T>.Type }, valuesFilter);
        return this;
    }

    protected void RegisterDefaultCollectionConverters()
    {
        ConvertGeneric(typeof(ICollection<>), Meta<Array>.Type, new ArraysConverterProvider());
    }

    protected IEnumerable<IMappingOperation> FilterOperations(
        Type from,
        Type to,
        IEnumerable<IMappingOperation> operations)
    {
        var result = new List<IMappingOperation>();
        foreach (var op in operations)
        {
            if (op is IReadWriteOperation readwrite)
            {
                if (TestIgnore(from, to, readwrite.Source, readwrite.Destination))
                    continue;

                readwrite.NullSubstitutor =
                    _nullSubstitutors.GetValue(new[] { readwrite.Source.MemberType, readwrite.Destination.MemberType });
                readwrite.TargetConstructor = _customConstructors.GetValue(new[] { readwrite.Destination.MemberType });
                readwrite.Converter = _customConverters.GetValue(new[] { readwrite.Source.MemberType, readwrite.Destination.MemberType }) ?? GetGenericConverter(readwrite.Source.MemberType, readwrite.Destination.MemberType);
                readwrite.DestinationFilter = _destinationFilters.GetValue(new[] { readwrite.Destination.MemberType });
                readwrite.SourceFilter = _sourceFilters.GetValue(new[] { readwrite.Source.MemberType });
            }

            if (op is ReadWriteComplex readWriteComplex)
            {
                readWriteComplex.ValuesPostProcessor = _postProcessors.GetValue(new[] { readWriteComplex.Destination.MemberType });
            }

            if (op is IComplexOperation complexOperation)
            {
                var orw = complexOperation as IReadWriteOperation;
                complexOperation.Operations = FilterOperations(
                    orw == null ? from : orw.Source.MemberType,
                    orw == null ? to : orw.Destination.MemberType,
                    complexOperation.Operations).ToList();
            }

            result.Add(op);
        }

        return result;
    }

    private Delegate GetGenericConverter(Type from, Type to)
    {
        var converter = _customConvertersGeneric.GetValue(new[] { from, to });
        if (converter == null)
            return null;

        var converterDescr = converter.GetCustomConverterDescr(from, to, this);

        if (converterDescr == null)
            return null;

        var genericConverter = converterDescr.ConverterClassTypeArguments is { Length: > 0 } ? converterDescr.ConverterImplementation.MakeGenericType(converterDescr.ConverterClassTypeArguments) : converterDescr.ConverterImplementation;

        var mi = genericConverter.GetMethod(converterDescr.ConversionMethodName);

        var converterObj = Expression.Lambda<Func<object>>(Expression.New(genericConverter)).Compile()();


        if (converterObj is not ICustomConverter customConverter)
            return Delegate.CreateDelegate(typeof(Func<,,>).MakeGenericType(from, Meta<object>.Type, to), converterObj, mi);
        customConverter.Initialize(from, to, this);

        return Delegate.CreateDelegate(typeof(Func<,,>).MakeGenericType(from, Meta<object>.Type, to), converterObj, mi);
    }

    private bool TestIgnore(Type from, Type to, MemberDescriptor fromDescr, MemberDescriptor toDescr)
    {
        var ignore = _ignoreMembers.GetValue(new[] { from, to });
        if (ignore != null && (ignore.Contains(fromDescr.MemberInfo.Name) || ignore.Contains(toDescr.MemberInfo.Name)))
            return true;
        return false;
    }
}