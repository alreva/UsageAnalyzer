# Usage Analyzer

A .NET solution for processing and analyzing user event data.

## Project Structure

The solution consists of the following projects:

- **Dto**: Contains data transfer objects used for deserializing user event data
- **Processors**: Contains processors for handling different types of user data
- **Dto.Tests**: Unit tests for the DTO project
- **Processors.Tests**: Unit tests for the Processors project

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Your favorite IDE (Visual Studio, VS Code, etc.)

### Building the Solution

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

## Usage

The solution provides processors for handling different types of user data:

- `UserAddressProcessor`: Processes user address information
- `UserPreferencesProcessor`: Processes user preferences

Example usage:

```csharp
var processor = new UserAddressProcessor();
using var writer = new StringWriter();
processor.ProcessUserAddress(jsonInput, writer);
var result = writer.ToString();
```

## Testing

The solution includes unit tests for both DTOs and Processors. Run the tests using:

```bash
dotnet test
```

## License

This project is licensed under the MIT License. # UsageAnalyzer
