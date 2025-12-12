# kOS SimpleJson Addon

A JSON serialization addon for [kOS (Kerbal Operating System)](https://github.com/KSP-KOS/KOS) that provides extended JSON parsing and stringification capabilities for kOS scripts.

## Overview

This addon extends kOS with simple JSON functionality, allowing you to serialize kOS data structures to basic JSON strings and parse plain JSON strings back into kOS structures. It uses the SimpleJson library internally for efficient JSON processing. It also exposes helper suffixes to validate JSON strings and return safe fallbacks when parsing fails, so scripts stay running.

While kOS provides its own `READJSON` and `WRITEJSON` functions which allow complete serialization and deserialization without loss of types, this addon provides the functionality to read and write plain JSON data without the need for custom types.
The only drawback is that when converting a structure to and from JSON, it might not be the same type anymore. See [supported type conversions](#supported-types) for more info.

## Features

- **JSON Stringification**: Convert kOS structures (Lexicons, Lists, primitives, etc.) to JSON strings
- **JSON Parsing**: Parse JSON strings into kOS structures
- **Safe Parsing Helpers**: Validate JSON and supply fallbacks via `PARSEORELSE`, `PARSEORELSEGET`, and `ISPARSEABLE`
- **Type Support**: Handles strings, numbers (int/double), booleans, arrays, and objects
- **Robust Number Handling**: Automatically handles numeric type conversions and ranges
- **Special Structure Support**: Serializes PID loops and ranges correctly

## Requirements
- kOS

## Installation

1. Download the latest release
2. Extract the contents into your KSP root folder
3. The addon will be loaded automatically when KSP starts

Your directory structure should look like:
```
KSP_ROOT/
└─ GameData/
   └─ kOS-simpleJson/
      ├─ kOS-simpleJson.dll
      ├─ kOS-simpleJson.version
      ├─ LICENSE
      └─ README
```

## Usage

The addon provides several functions accessible through the base path `ADDONS:JSON`:

### STRINGIFY

Converts a kOS structure to a JSON string. Works with kOS-serializable structures (Lexicons, Lists, primitives, PID loops, ranges).

```kerboscript
// Stringify a lexicon
SET myLex TO LEXICON("name", "Rocket", "altitude", 1000, "active", True).
SET jsonString TO ADDONS:JSON:STRINGIFY(myLex).
PRINT jsonString.
// Output: {"name":"Rocket","altitude":1000,"active":true}

// Stringify a list
SET myList TO LIST(1, 2, 3, "four").
SET jsonString TO ADDONS:JSON:STRINGIFY(myList).
PRINT jsonString.
// Output: [1,2,3,"four"]

// Stringify primitives
PRINT ADDONS:JSON:STRINGIFY(42).        // Output: 42
PRINT ADDONS:JSON:STRINGIFY("hello").   // Output: "hello"
PRINT ADDONS:JSON:STRINGIFY(True).      // Output: true
```

### PARSE

Parses a JSON string into a kOS structure. Throws if the JSON is invalid.

```kerboscript
// Parse JSON object
SET jsonString TO "{""name"":""Rocket"",""altitude"":1000}".
SET myLex TO ADDONS:JSON:PARSE(jsonString).
PRINT myLex["name"].      // Output: Rocket
PRINT myLex["altitude"].  // Output: 1000

// Parse JSON array
SET jsonString TO "[1,2,3,4,5]".
SET myList TO ADDONS:JSON:PARSE(jsonString).
PRINT myList[0].  // Output: 1

// Parse primitives
PRINT ADDONS:JSON:PARSE("42").      // Output: 42
PRINT ADDONS:JSON:PARSE("true").    // Output: True
```

### PARSEORELSE

Parses a JSON string, or returns the provided fallback if parsing fails.

```kerboscript
SET badJson TO "{""name"":""Rocket""".
SET fallback TO LEXICON("name", "Fallback", "active", False).
SET data TO ADDONS:JSON:PARSEORELSE(badJson, fallback).
PRINT data["name"].    // Output: Fallback
```

### PARSEORELSEGET

Parses a JSON string, or calls a delegate to produce a fallback when parsing fails.
The delegate function is only called if parsing the given JSON fails.

```kerboscript
DECLARE FUNCTION BuildDefault {
    RETURN LEXICON("status", "unknown", "tries", 1).
}.
SET maybeJson TO "}not-json{".
SET data TO ADDONS:JSON:PARSEORELSEGET(maybeJson, BuildDefault@).
PRINT data["status"].   // Output: unknown
```

### ISPARSEABLE

Checks if a string can be parsed as JSON without throwing.

```kerboscript
SET candidate TO "{""value"":1}".
IF ADDONS:JSON:ISPARSEABLE(candidate) {
    PRINT ADDONS:JSON:PARSE(candidate).
} ELSE {
    PRINT "Invalid JSON".
}
```

## Supported Types

### kOS to JSON (STRINGIFY)

| kOS Type                | JSON Type        |
| ----------------------- | ---------------- |
| String                  | string           |
| Number (integer)        | number (integer) |
| Number (floating point) | number (float)   |
| Boolean                 | boolean          |
| any List-like           | array            |
| Lexicon                 | object           |
| all others              | object           |

### JSON to kOS (PARSE)

| JSON Type        | kOS Type          |
| ---------------- | ----------------- |
| string           | String            |
| number (integer) | Number            |
| number (float)   | Number            |
| boolean          | Boolean           |
| array            | List              |
| object           | Lexicon           |
| null             | empty String ("") |

## Examples

### Working with API Responses

```kerboscript
// Simulating an API response
SET apiResponse TO "{""vessel"":{""name"":""Explorer 1"",""mass"":5000,""parts"":25}}".
SET data TO ADDONS:JSON:PARSE(apiResponse).
PRINT "Vessel: " + data["vessel"]["name"].
PRINT "Mass: " + data["vessel"]["mass"].
```

### Saving Configuration

```kerboscript
// Create configuration
SET config TO LEXICON(
    "launchAzimuth", 90,
    "targetAltitude", 80000,
    "stages", LIST(
        LEXICON("fuel", 100, "engines", 1),
        LEXICON("fuel", 200, "engines", 2)
    )
).

// Save to file
SET jsonConfig TO ADDONS:JSON:STRINGIFY(config).
LOG jsonConfig TO "0:/config.json".

// Load from file
SET loadedJson TO OPEN("0:/config.json"):READALL:STRING.
SET loadedConfig TO ADDONS:JSON:PARSE(loadedJson).
```

## Building from Source

### Requirements

- Visual Studio 2022 (or Build Tools 2022) with the .NET Framework 4.8 targeting pack
- KSP with kOS installed (provide the kOS DLLs from your KSP install)
- .NET SDK/CLI available (`dotnet`)

### Build Steps

1. Clone the repository.
2. Link a KSP + kOS install using [KSPBuildTools](https://github.com/KSPModdingLibs/KSPBuildTools).
3. Restore tools and packages: `dotnet restore`.
4. Build: `dotnet build -c Release` (or open `kOS-simpleJson.sln` in VS 2022 and build).
5. Outputs land in `kOS-simpleJson/bin/<Config>/net48/`. KSPBuildTools also mirrors the build + `.version` file into `GameData/kOS-simpleJson/` for a local install.

## Technical Details

The addon implements the [`IFormatWriter`](https://github.com/KSP-KOS/KOS/blob/9d896ace93adca5e13c915a06f886e4e23761d0b/src/kOS.Safe/Serialization/Formatter.cs) interface from kOS.Safe.Serialization, using the [`SimpleJsonFormatter`](kOS-simpleJson/SimpleJsonFormatter.cs) class to handle serialization. Deserialization is handled by the [JsonDeserializer](kOS-simpleJson/JsonDeserializer.cs) skipping the conversion to dumps. The main entry point is the [`SimpleJsonAddon`](kOS-simpleJson/SimpleJsonAddon.cs) class, which is decorated with the `[kOSAddon("JSON")]` attribute to register it with kOS.

## Known Limitations

- Circular references are not supported and will cause serialization errors
- Some complex kOS types may not serialize correctly (please report as issues)
- JSON null values are converted to empty strings

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.

## Credits

- Built using [KSPBuildTools](https://github.com/KSPModdingLibs/KSPBuildTools)
- Uses kOS built-in [SimpleJson](https://github.com/facebook-csharp-sdk/simple-json) for JSON processing
- Built for [kOS (Kerbal Operating System)](https://github.com/KSP-KOS/KOS)
- Copyright © Throin 2025
