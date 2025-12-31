# PNGTuber-GPTv2 Coding Bible

This document serves as the single source of truth for all architectural decisions, coding standards, and project patterns. It is designed to be read by both human developers and AI agents to ensure consistency, reliability, and maintainability.

## 1. Architectural Constraints & Tech Stack

### 1.1 Target Framework
- **Strict Requirement**: `.NET Framework 4.8.1`.
- **Reasoning**: This is the runtime environment of Streamer.bot.
- **Constraints**:
  - Do **NOT** use .NET Core or .NET 5+ specific APIs (e.g., `DateOnly`, `TimeOnly`, `Span<T>` optimizations tailored for Core runtime).
  - Do **NOT** suggest upgrading to .NET 6/7/8 unless Streamer.bot itself updates.

### 1.2 Plugin Architecture
- **Interface**: `Streamer.bot.Plugin.Interface.dll`.
- **Entry Point**: `CPHInline` class with an `Execute` method.
- **Proxy**: Access all Streamer.bot functionality via the `CPH` (`IInlineInvokeProxy`) instance.

## 2. Coding Standards

### 2.1 Async/Await Pattern
- **Preferred**: Use `async` and `await` for I/O bound operations.
- **Avoid**: `Thread.Sleep()`. Use `await Task.Delay()` instead to prevent blocking the main thread.
- **Concurrency**: Use `System.Threading.Channels` for producer/consumer workflows (e.g., queuing messages for TTS or LLM processing).

### 2.2 Resource Locking
- **Locking**: Use `SemaphoreSlim` for async-compatible locking. Avoid `lock(_object)` when inside async methods.

### 2.3 Style & Formatting
- **Naming**: `PascalCase` for classes, methods, and properties. `camelCase` for local variables and private fields.
- **Namespace**: `PNGTuber_GPTv2`.

## 3. Logging Strategy ("Log All The Things")

### 3.1 Philosophy
We must know *what* happened, *when* it happened, and *why*. Silence is the enemy of debugging.

### 3.2 Standards
- **Verbose/Trace**: Log entry and exit of complex workflows.
- **Info**: Log significant state changes (e.g., "Connected to Twitch", "Received Message", "LLM Response Generated").
- **Warning**: Log recoverable errors (e.g., "API Timeout, retrying...").
- **Error**: Log exceptions with full stack traces.

### 3.3 Pattern
```csharp
CPH.LogInfo($"[ServiceName] Starting process X with inputs: {input}");
try {
    // ... work ...
    CPH.LogInfo($"[ServiceName] Process X completed successfully.");
} catch (Exception ex) {
    CPH.LogInfo($"[ServiceName] Error in process X: {ex}"); // LogInfo usually captures the whole string, verify if LogError exists in CPH
}
```

## 4. Data Persistence (LiteDB)

### 4.1 Schema
- **Structure**: Use POCO classes.
- **Identity**: All documents must have a unique `Id` (usually `ObjectId` or `int`).

### 4.2 Migrations
- **Philosophy**: The code is the source of truth. If the DB schema lags, update it on startup.
- **Pattern**:
  - Store a `Version` document in a `Metadata` collection.
  - On startup, check DB version vs Code version.
  - Execute migration logic step-by-step if needed.

## 5. CI/CD Methodology

### 5.1 Infrastructure
- **Runner**: Self-Hosted macOS Runner.
- **Reason**: We rely on local `Streamer.bot` DLLs that cannot be publicly distributed in the repo for copyright/licensing reasons (or size).

### 5.2 Build Pipeline
- **Workflow**: `.github/workflows/build.yml`.
- **Dependency Resolution**:
  - The runner copies `Streamer.bot-x64-1.0.1` from its local storage (`../`) into the build workspace (`./`).
  - This allows the relative path `<HintPath>..\Streamer.bot-x64-1.0.1\Streamer.bot.Plugin.Interface.dll</HintPath>` to resolve correctly during `dotnet build`.

### 5.3 Release
- **Artifacts**: Compiled DLLs are output to `bin/Release/net481/`.

## 6. Event Brain Architecture

### 6.1 Core Concept
The "Event Brain" is a high-throughput, microservices-style controller that orchestrates Streamer.bot events.
- **Ingest**: Receives raw events/args from `CPH`.
- **Normalize**: Converts raw args into strongly-typed `structs` (Data Transfer Objects).
- **Dispatch**: Routes verified DTOs to specific **Channels** (queues).
- **Consume**: Autonomous **Micro-Consumers** pick up tasks from channels and execute them.

### 6.2 The Micro-Consumer Pattern
Each consumer must be a small, single-responsibility service running asynchronously.
- **Input**: A strongly-typed struct.
- **Action**: One specific task (e.g., "Check Pronouns", "Log to DB", "Send Chat").
- **Output**: Optionally pushes a new result to another Channel (chaining).

```csharp
// Example Micro-Consumer Signature
public async Task ProcessAsync(ChatMessage msg, CancellationToken ct) { ... }
```

## 7. Anti-Patterns & AI Constraints (The "No-Go" Zone)

### 7.1 "Small, Modular, Repeatable"
- **Rule**: No method shall exceed **30 lines of code**. If it does, refactor into helper methods.
- **Rule**: No "God Classes". One class, one responsibility.
- **Rule**: No massive `switch` statements. Use Dictionary dispatch or Polymorphism.

### 7.2 Security First
- **Input Validation**: NEVER trust `args` from Streamer.bot blindly. Validate and sanitize immediately upon ingestion.
- **State Isolation**: Consumers should not share mutable state. Pass data via immutable structs.

### 7.3 Negative Design Patterns
- **Avoid**: Deep nesting (Arrow Code). Return early.
- **Avoid**: Global state modification from deep within consumers.
- **Avoid**: "Magic Strings". Use constants or `nameof()`.
