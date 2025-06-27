# Codebase Overview

This document provides a high-level introduction for newcomers. It summarizes the structure of the repository and explains the main components.

## Repository Layout

The solution contains several projects:

- **Dto** – Defines the data transfer objects (DTOs) used to deserialize user event JSON.
- **Processors** – Includes small processors that operate on DTOs. Each processor reads JSON input, deserializes it, and writes formatted output.
- **DtoUsageAnalyzer** – Library containing the `AnalysisService` used for Roslyn-based analysis.
- **Analyze** – A command-line tool that uses `DtoUsageAnalyzer` to analyze how DTO classes and their properties are used across a solution.
- **Dto.Tests** – Unit tests for the DTO project.
- **Processors.Tests** – Unit tests for the processor components.

## DTOs

DTOs such as `UserEventDto` model the data contained in user event payloads. They use C# `required` properties so deserialization fails if mandatory fields are missing.

## Processors

Processors inherit from `BaseProcessor<TDto>`, which handles JSON deserialization and shared helper methods. Examples include `UserAddressProcessor` and `UserEventProcessor`. Each processor writes formatted information about the DTO to a `TextWriter`.

## Analyze Project

The `Analyze` project is a console application. It loads a solution, locates DTO classes, and uses Roslyn to analyze how properties are accessed. The tool reports usage counts and unused properties.

### Key Steps

1. Configure services and logging.
2. Select the solution file and DTO class to analyze.
3. Run Roslyn-based analysis to count property access.
4. Display results in the console.

## Tips for New Contributors

- Explore the processors under `Processors/` to understand their structure.
- Review `DtoUsageAnalyzer/AnalysisService.cs` to see how Roslyn is used for static analysis.
- Run the command-line tool with a real solution to view usage output.
- Check the unit tests for examples of how processors and DTOs are validated.

