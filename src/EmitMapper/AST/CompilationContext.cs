namespace EmitMapper.AST;

/// <summary>
/// The compilation context.
/// </summary>
internal class CompilationContext
{
	/// <summary>
	/// Il generator
	/// </summary>
	/// <value cref="ILGenerator">ILGenerator</value>
	public ILGenerator IlGenerator { get; }

	/// <summary>
	/// Output commands
	/// </summary>
	/// <value cref="TextWriter">TextWriter</value>
	public TextWriter OutputCommands { get; }

	private int stackCount;

	/// <summary>
	/// Initializes a new instance of the <see cref="CompilationContext"/> class.
	/// </summary>
	public CompilationContext()
	{
		// OutputCommands = TextWriter.Null; outputCommands = Console.Out;
		OutputCommands = new DebuggerWriter(1, "IL CODE");
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CompilationContext"/> class.
	/// </summary>
	/// <param name="ilGenerator">The il generator.</param>
	public CompilationContext(ILGenerator ilGenerator)
	  : this()
	{
		IlGenerator = ilGenerator;
	}

	/// <inheritdoc/>
	public void Emit(OpCode opCode)
	{
		ProcessCommand(opCode, 0, string.Empty);
		IlGenerator.Emit(opCode);
	}

	/// <inheritdoc/>
	public void Emit(OpCode opCode, string str)
	{
		ProcessCommand(opCode, 0, str);
		IlGenerator.Emit(opCode, str);
	}

	/// <inheritdoc/>
	public void Emit(OpCode opCode, int param)
	{
		ProcessCommand(opCode, 0, param.ToString(CultureInfo.InvariantCulture));
		IlGenerator.Emit(opCode, param);
	}

	/// <inheritdoc/>
	public void Emit(OpCode opCode, FieldInfo fi)
	{
		ProcessCommand(opCode, 0, fi.ToString());
		IlGenerator.Emit(opCode, fi);
	}

	/// <inheritdoc/>
	public void Emit(OpCode opCode, ConstructorInfo ci)
	{
		ProcessCommand(opCode, 0, ci.ToString());
		IlGenerator.Emit(opCode, ci);
	}

	/// <inheritdoc/>
	public void Emit(OpCode opCode, LocalBuilder lb)
	{
		ProcessCommand(opCode, 0, lb.ToString());
		IlGenerator.Emit(opCode, lb);
	}

	/// <inheritdoc/>
	public void Emit(OpCode opCode, Label lb)
	{
		ProcessCommand(opCode, 0, lb.ToString());
		IlGenerator.Emit(opCode, lb);
	}

	/// <inheritdoc/>
	public void Emit(OpCode opCode, Type type)
	{
		ProcessCommand(opCode, 0, type.ToString());
		IlGenerator.Emit(opCode, type);
	}

	/// <summary>
	/// Emits the call.
	/// </summary>
	/// <param name="opCode">The op code.</param>
	/// <param name="mi">The mi.</param>
	public void EmitCall(OpCode opCode, MethodInfo mi)
	{
		ProcessCommand(
		  opCode,
		  (mi.GetParameters().Length + 1) * -1 + (mi.ReturnType == Metadata.Void ? 0 : 1),
		  mi.ToString());

		IlGenerator.EmitCall(opCode, mi, null);
	}

	/// <summary>
	/// Emits the new object.
	/// </summary>
	/// <param name="ci">The ci.</param>
	public void EmitNewObject(ConstructorInfo ci)
	{
		ProcessCommand(OpCodes.Newobj, ci.GetParameters().Length * -1 + 1, ci.ToString());

		IlGenerator.Emit(OpCodes.Newobj, ci);
	}

	/// <summary>
	/// Throws the exception.
	/// </summary>
	/// <param name="exType">The ex type.</param>
	public void ThrowException(Type exType)
	{
		IlGenerator.ThrowException(exType);
	}

	/// <summary>
	/// Gets the stack change.
	/// </summary>
	/// <param name="beh">The beh.</param>
	/// <returns>An int.</returns>
	private static int GetStackChange(StackBehaviour beh)
	{
		switch (beh)
		{
			case StackBehaviour.Pop0:
			case StackBehaviour.Push0:
				return 0;

			case StackBehaviour.Pop1:
			case StackBehaviour.Popi:
			case StackBehaviour.Popref:
			case StackBehaviour.Varpop:
				return -1;

			case StackBehaviour.Push1:
			case StackBehaviour.Pushi:
			case StackBehaviour.Pushref:
			case StackBehaviour.Varpush:
				return 1;

			case StackBehaviour.Pop1_pop1:
			case StackBehaviour.Popi_pop1:
			case StackBehaviour.Popi_popi:
			case StackBehaviour.Popi_popi8:
			case StackBehaviour.Popi_popr4:
			case StackBehaviour.Popi_popr8:
			case StackBehaviour.Popref_pop1:
			case StackBehaviour.Popref_popi:
				return -2;

			case StackBehaviour.Push1_push1:
				return 2;

			case StackBehaviour.Popref_popi_pop1:
			case StackBehaviour.Popref_popi_popi:
			case StackBehaviour.Popref_popi_popi8:
			case StackBehaviour.Popref_popi_popr4:
			case StackBehaviour.Popref_popi_popr8:
			case StackBehaviour.Popref_popi_popref:
				return -3;
		}

		return 0;
	}

	/// <summary>
	/// Processes the command.
	/// </summary>
	/// <param name="opCode">The op code.</param>
	/// <param name="addStack">The add stack.</param>
	/// <param name="comment">The comment.</param>
	private void ProcessCommand(OpCode opCode, int addStack, string? comment)
	{
		var stackChange = GetStackChange(opCode.StackBehaviourPop) + GetStackChange(opCode.StackBehaviourPush) + addStack;

		stackCount += stackChange;
		WriteOutputCommand(opCode + " " + comment);
	}

	/// <summary>
	/// Writes the output command.
	/// </summary>
	/// <param name="command">The command.</param>
	private void WriteOutputCommand(string command)
	{
		OutputCommands?.WriteLine(new string('\t', stackCount >= 0 ? stackCount : 0) + command);
	}
}