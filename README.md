# kOS SimpleJson Addon

A JSON serialization addon for [kOS (Kerbal Operating System)](https://github.com/KSP-KOS/KOS) that provides extended JSON parsing and stringification capabilities for kOS scripts.

## Overview

This addon extends kOS with simple JSON functionality, allowing you to serialize kOS data structures to basic JSON strings and parse plain JSON strings back into kOS structures. It uses the SimpleJson library internally for efficient JSON processing.

While kOS provides its own `READJSON` and `WRITEJSON` functions which allow complete serialization and deserialization without loss of types, this addon provides the functionality to read and write plain JSON data without the need for custom types.
The only drawback is that when converting a structure to and from JSON, it might not be the same type anymore. See [supported type conversions](#supported-types) for more info.

## Features

- **JSON Stringification**: Convert kOS structures (Lexicons, Lists, primitives, etc.) to JSON strings
- **JSON Parsing**: Parse JSON strings into kOS structures
- **Type Support**: Handles strings, numbers (int/double), booleans, arrays, and objects
- **Robust Number Handling**: Automatically handles numeric type conversions and ranges
- **Special Structure Support**: Serializes PID loops and ranges correctly

## Requirements
- kOS

## Installation

1. Download the latest release
2. Extract the contents to your KSP `GameData` folder
3. The addon will be loaded automatically when KSP starts

Your directory structure should look like:
```
GameData/
└─ kOS-simpleJson/
   ├─ Plugins/
   │  └─ kOS-simpleJson.dll
   └─ LICENSE
```

## Usage

The addon provides two main functions accessible through the base path `ADDONS:JSON`:

### STRINGIFY

Converts a kOS structure to a JSON string.

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

Parses a JSON string into a kOS structure.

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
| string           | StringValue       |
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

- Visual Studio 2015 or later
- .NET Framework 4.8
- KSP and kOS DLL references (place in `../dlls/` relative to the repository)

### Build Steps

1. Clone the repository
2. Place required DLLs in the `dlls` folder:
   - Assembly-CSharp.dll
   - Assembly-CSharp-firstpass.dll
   - kOS.dll
   - kOS.Safe.dll
   - UnityEngine.dll
   - UnityEngine.CoreModule.dll
   - UnityEngine.JSONSerializeModule.dll
   - UnityEngine.UIModule.dll
3. Open [kOS-simpleJson.sln](kOS-simpleJson.sln) in Visual Studio
4. Build the solution (F6)
5. The compiled DLL will be in `bin/Debug/` or `bin/Release/`

## Technical Details

The addon implements the [`IFormatWriter`](kOS-simpleJson/SimpleJsonFormatter.cs) interface from kOS.Safe.Serialization, using the [`SimpleJsonFormatter`](kOS-simpleJson/SimpleJsonFormatter.cs) class to handle serialization and deserialization. The main entry point is the [`SimpleJsonAddon`](kOS-simpleJson/SimpleJsonAddon.cs) class, which is decorated with the `[kOSAddon("JSON")]` attribute to register it with kOS.

## Known Limitations

- Circular references are not supported and will cause serialization errors
- Some complex kOS types may not serialize correctly (please report as issues)
- JSON null values are converted to empty strings

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.

## Credits

- Uses kOS built-in [SimpleJson](https://github.com/facebook-csharp-sdk/simple-json) for JSON processing
- Built for [kOS (Kerbal Operating System)](https://github.com/KSP-KOS/KOS)
- Copyright © Throin 2025
