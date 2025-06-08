# Usage Analyzer

A .NET solution for processing and analyzing user event data.

## Project Structure

The solution consists of the following projects:

- **Dto**: Contains data transfer objects used for deserializing user event data
- **Processors**: Contains processors for handling different types of user data
- **Dto.Tests**: Unit tests for the DTO project
- **Processors.Tests**: Unit tests for the Processors project
- **Analyze**: Main application project for analyzing user data

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later
- Your favorite IDE (Visual Studio, VS Code, etc.)

### Building the Solution

```bash
dotnet build
```

### Running the Application

To run the Analyze application:

```bash
cd Analyze
dotnet run
```

The application provides a console interface for analyzing user data. Follow the on-screen prompts to:
1. Select the type of data to analyze
2. Input or load JSON data
3. View the processed results

### Running Tests

The solution includes comprehensive unit tests for both DTOs and Processors. Run the tests using:

```bash
dotnet test
```

## Usage

The solution provides several processors for handling different types of user data:

- `UserAddressProcessor`: Processes user address information
- `UserPreferencesProcessor`: Processes user preferences (theme, language, notifications, etc.)
- `UserDeviceInfoProcessor`: Processes device information (device type, OS, browser, IP)
- `UserSocialMediaProcessor`: Processes social media profiles
- `ActivityLogProcessor`: Processes user activity logs
- `UserEventProcessor`: Processes general event information
- `UserProcessor`: Processes core user information (profile, preferences, activity)

Example usage:

```csharp
// Create a processor instance
var processor = new UserAddressProcessor();

// Process the JSON input
using var writer = new StringWriter();
processor.Process(jsonInput, writer);
var result = writer.ToString();
```

Each processor follows the same pattern:
1. Takes JSON input containing user event data
2. Deserializes the data into appropriate DTOs
3. Processes the data and writes formatted output to a TextWriter

## License

This project is licensed under the MIT License.
