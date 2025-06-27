// <copyright file="IProcessor.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Processors;

public interface IProcessor<TDto>
{
  void Process(string jsonInput, TextWriter output);
}