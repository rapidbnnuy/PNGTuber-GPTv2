# Logging Rules & Standards

This document defines the STRICT logging standards for PNGTuber-GPTv2. Adherence to these levels is mandatory to ensure observability without noise.

## 1. Log Levels

### 1.1 TRACE (FORBIDDEN)
- **Status**: **DOES NOT EXIST**.
- **Rule**: Do not use Trace. Do not implement Trace. If you feel the need for Trace, use Debug.

#- **Rule**: Do not use Trace. Do not implement Trace. If you feel the need for Trace, use Debug.

## Hierarchy & Filtering

The log level determines "severity". The system logs everything **at or above** the configured level (numerically lower or equal).

| Configured Level | Logs ERROR? | Logs WARN? | Logs INFO? | Logs DEBUG? |
|------------------|-------------|------------|------------|-------------|
| **ERROR**        | ✅ Yes      | ❌ No      | ❌ No      | ❌ No       |
| **WARN**         | ✅ Yes      | ✅ Yes     | ❌ No      | ❌ No       |
| **INFO**         | ✅ Yes      | ✅ Yes     | ✅ Yes     | ❌ No       |
| **DEBUG**        | ✅ Yes      | ✅ Yes     | ✅ Yes     | ✅ Yes      |

### 1.2 DEBUG (The Firehose)
- **Purpose**: Full system transparency for development and deep debugging.
- **Content**:
    - **Entry/Exit**: Log the start and end of every public routine/method.
    - **Data**: Log the **FULL JSON** of every Web Request input and output.
    - **Persistence**: Log the exact SQL/LiteDB query text.
    - **State**: Log detailed state changes during processing.

### 1.3 INFO (The Narrative)
- **Purpose**: High-level operational summary. "What happened?"
- **Content**:
    - **Actions**: "User X joined chat", "Command !help executed".
    - **Results**: "Response sent to Twitch", "Profile updated".
    - **Summaries**:
        - **Cost**: "Run Cost: $0.002"
        - **Moderation**: "Message flagged: False [Categories: None]"
        - **Performance**: "Processing Time: 45ms"

### 1.4 WARN (Recoverable Issues)
- **Purpose**: Something went wrong, but the system kept running.
- **Content**:
    - **Nulls**: Unexpected nulls that were handled via defaults.
    - **Retries**: "API call failed, retrying (1/3)..."
    - **Config**: "Missing optional config 'VoiceStyle', using default."
    - **Process**: Any error that **DID NOT** cause the process to abort or return a Failure result.

### 1.5 ERROR (Failures)
- **Purpose**: Critical failures that stopped a process.
- **Content**:
    - **Exceptions**: Full stack traces.
    - **Aborts**: "Could not save to DB. Process aborted."
    - **Result**: Any event where the system returned a `Result.Failure()`.

## 2. Formatting Standards

- **Timestamp**: `yyyy-MM-dd HH:mm:ss.fff`
- **Structure**: `[Timestamp LEVEL] Message`
- **JSON**: Always formatted/indented in Debug logs if possible, or single-line if massive.
- **Anonymization**: Do NOT log API Keys or Auth Tokens, even in Debug.

## 3. Implementation Rules for AI
- **When writing new methods**: You MUST add `_logger.Debug("Entering MethodName")` at the start.
- **When calling APIs**: You MUST `_logger.Debug()` the request payload and response.
- **When catching exceptions**: If you re-throw or return Fail, it is an ERROR. If you handle/ignore, it is a WARN.
