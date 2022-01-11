﻿using System;
using System.Reflection.Emit;
using EmitMapper.AST.Helpers;
using EmitMapper.AST.Interfaces;

namespace EmitMapper.AST.Nodes;

internal class AstCastclass : IAstRefOrValue
{
  protected IAstRefOrValue Value;
  protected Type TargetType;

  public AstCastclass(IAstRefOrValue value, Type targetType)
  {
    Value = value;
    TargetType = targetType;
  }

  #region IAstStackItem Members

  public Type ItemType => TargetType;

  #endregion

  #region IAstNode Members

  public virtual void Compile(CompilationContext context)
  {
    if (Value.ItemType != TargetType)
    {
      if (!Value.ItemType.IsValueType && !TargetType.IsValueType)
      {
        Value.Compile(context);
        context.Emit(OpCodes.Castclass, TargetType);
        return;
      }

      if (TargetType.IsValueType && !Value.ItemType.IsValueType)
      {
        new AstUnbox { RefObj = (IAstRef)Value, UnboxedType = TargetType }.Compile(context);
        return;
      }

      throw new EmitMapperException();
    }

    Value.Compile(context);
  }

  #endregion
}

internal class AstCastclassRef : AstCastclass, IAstRef
{
  public AstCastclassRef(IAstRefOrValue value, Type targetType)
    : base(value, targetType)
  {
  }

  public override void Compile(CompilationContext context)
  {
    CompilationHelper.CheckIsRef(ItemType);
    base.Compile(context);
  }
}

internal class AstCastclassValue : AstCastclass, IAstValue
{
  public AstCastclassValue(IAstRefOrValue value, Type targetType)
    : base(value, targetType)
  {
  }

  public override void Compile(CompilationContext context)
  {
    CompilationHelper.CheckIsValue(ItemType);
    base.Compile(context);
  }
}