﻿namespace EmitMapper.AST.Nodes;

using System.Reflection.Emit;

using EmitMapper.AST.Interfaces;

internal class AstIf : IAstNode
{
  public IAstValue Condition;

  public AstComplexNode FalseBranch;

  public AstComplexNode TrueBranch;

  public void Compile(CompilationContext context)
  {
    var elseLabel = context.ILGenerator.DefineLabel();
    var endIfLabel = context.ILGenerator.DefineLabel();

    Condition.Compile(context);
    context.Emit(OpCodes.Brfalse, elseLabel);

    if (TrueBranch != null)
      TrueBranch.Compile(context);
    if (FalseBranch != null)
      context.Emit(OpCodes.Br, endIfLabel);

    context.ILGenerator.MarkLabel(elseLabel);
    if (FalseBranch != null)
      FalseBranch.Compile(context);
    context.ILGenerator.MarkLabel(endIfLabel);
  }
}