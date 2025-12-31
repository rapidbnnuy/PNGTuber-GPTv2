# PNGTuber-GPTv2

A Streamer.bot plugin that integrates LLM capabilities for PNGTubers.

## Overview
This project is a C# Class Library targeting .NET Framework 4.8.1, designed to run as a plugin within Streamer.bot. It utilizes the `Streamer.bot.Plugin.Interface` to interact with the bot's core functionality.

## Development

### Prerequisites
- .NET SDK (for build tooling)
- Streamer.bot installed locally (specifically for `Streamer.bot.Plugin.Interface.dll`)

### Build
The project references `Streamer.bot.Plugin.Interface.dll` via a relative path. ensure your directory structure is as follows:
```
/ParentDirectory
  /Streamer.bot-x64-1.0.1  <-- Contains Streamer.bot.Plugin.Interface.dll
  /PNGTuber-GPTv2          <-- This repository
```

Run the build command:
```bash
dotnet build -c Release
```

## CI/CD
This project uses GitHub Actions with a self-hosted runner (macOS). The runner is configured to copy the local Streamer.bot dependency into the build workspace to resolve references.
