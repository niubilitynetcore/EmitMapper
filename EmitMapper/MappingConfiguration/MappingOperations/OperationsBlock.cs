﻿namespace EmitMapper.MappingConfiguration.MappingOperations;

using System.Collections.Generic;

using EmitMapper.MappingConfiguration.MappingOperations.Interfaces;

public class OperationsBlock : IComplexOperation
{
  public List<IMappingOperation> Operations { get; set; }
}