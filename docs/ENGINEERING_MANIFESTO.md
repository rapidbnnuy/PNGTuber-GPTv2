# ENGINEERING MANIFESTO

> **READ THIS BEFORE YOU WRITE A SINGLE LINE OF CODE.**
>
> If you violate these rules, the build will fail, the bot will crash, and I will be very unhappy.
> This document is not a suggestion. It is the **spec**.
>
> -- The Senior Architect

---

## 1. THE GOLDEN RULES (NON-NEGOTIABLE)

### 1.1 The "No-Comment" Policy
*   **Philosophy**: Comments are lies waiting to happen. If you need a comment to explain your code, **your code is garbage**. Refactor it.
*   **The Rule**: **NO COMMENTS ALLOWED**. None. Zero.
*   **The Exception**: XML DocStrings on `public` interfaces *only* if required for Intellisense.
*   **Enforcement**: If I see `// TODO` or `// This does X`, I will reject the PR. Use `task.md` for todos.

### 1.2 The "30-Line" Limit
*   **The Rule**: No method shall exceed **30 lines of code**.
*   **The Reason**: If it's longer than 30 lines, you're doing too much. Break it down.
*   **The Fix**: Extract private helper methods with descriptive names. `ProcessEvent` is better than 50 lines of spaghetti.

### 1.3 The Tech Stack Constraints
*   **Runtime**: `.NET Framework 4.8.1` (Streamer.bot native).
*   **Forbidden Fruit**: Do NOT use `.NET Core` features (`DateOnly`, `Span<T>`, `record`). They do not exist here.
*   **Project Type**: Class Library (`.dll`).
*   **Proxy**: Uses `CPH` (`IInlineInvokeProxy`).

---

## 2. THE ARCHITECTURE: "Sequential Enrichment Pipeline"

We do not write "Event Handlers". We write **Pipelines**.
Traditional bots crash because they try to do 10 things at once inside `OnMessage`. We do **one** day at a time.

### 2.1 The Flow ("ProcessEvents" Pattern)
1.  **Ingestion ("The Coordinator")**:
    *   **Single Entry Point**: All Streamer.bot Actions (Chat, Commands, Raids) point to ONE C# Action: `ProcessEvents`.
    *   **Classification**: The Coordinator reads `triggerType`, `commandId`, etc. to determine the **Event Type**.
    *   **Extraction**: Pulls 60+ variables from SB Args into a `RequestContext`.
    *   **Queueing**: Pushes to `Channel<string>`. **RETURNS IMMEDIATELY**.
2.  **Processing (Background Thread)**:
    *   Picks up `RequestContext`.
    *   Runs through `IPipelineStep`s sequentially.
    *   **IdentityStep** (Who?) -> **LogicStep** (What?) -> **EnrichmentStep** (Format) -> **OutputStep**.
3.  **Enrichment ("The Context")**:
    *   We do not pass raw strings. We pass a hydrated `RequestContext`.
    *   **Formatted Message**: "He/Him [Nick]: Hello world" is generated for the AI.
    *   **KV Cache**: All resolved data is cached for instant retrieval.

### 2.2 The "Three Rings" of Defense
We assume everything will fail. We code for survival.
1.  **Ring 1: Async Buffering** (`System.Threading.Channels`)
    *   Bursty chat? We queue it. We process at *our* speed, not theirs.
2.  **Ring 2: Isolation** (`RequestContext`)
    *   No global state. Each request is an island.
3.  **Ring 3: The Nuclear Lock** (`DatabaseMutex`)
    *   **Named System Mutex** (`Global\PNGTuber-GPTv2-DB-Lock`).
    *   Only ONE thread (or process) touches the `.db` file at a time. No corruption. Ever.

---

## 3. THE SYSTEMS

### 3.1 Identity Code (Pronouns & Nicknames)
We respect identity. It is the most critical metadata.
*   **The "3-Layer" Cache Strategy**:
    1.  **L1 (RAM)**: `MemoryCache`. Instant. (TTL: 30m).
    2.  **L2 (Disk)**: LiteDB `user_pronouns`. Fast. (TTL: 7 Days).
    3.  **L3 (API)**: Alejo.io. Slow. Fallback only.
*   **The Rule**: NEVER block chat for an API call. If L2 is stale, return it *anyway* and trigger a background refresh.
*   **Neopronouns**: We support all 14 Alejo sets. We **infer** grammar (`xexem` -> "Xe went to the store").

### 3.2 Resilience Patterns
*   **Null Safety**: The "Billion Dollar Mistake".
    *   **Guard Clauses**: First 5 lines of any method check inputs.
    *   **Return Empty**: Never return `null` collections. Return `Array.Empty<T>()`.
*   **Timeouts**: Every `await` must have a `CancellationToken`. We do not wait forever.

---

## 4. LOGGING: "The Firehose & The Narrative"

We log strictly. No stray `Console.WriteLine`.

| Level | Usage |
|-------|-------|
| `ERROR` | System Failure. Stack Trace required. |
| `WARN` | Recoverable. "API failed, using cache." |
| `INFO` | Narrative. "User joined." "Response sent." |
| `DEBUG` | **The Firehose**. Loop entries. Full JSON dumps. SQL queries. |

*   **Rule**: `TRACE` is forbidden.
*   **Rule**: In `DEBUG`, log the **Entry** and **Exit** of every complex workflow.

---

## 5. DATABASE SCHEMA (LiteDB)

We use LiteDB, but we treat it like SQL.
*   **Relational**: We store `UserId` references, not nested objects.
*   **Collections**:
    *   `users`: Profile data. `_id` = `twitch:12345`.
    *   `user_pronouns`: Identity map.
    *   `settings`: Config documents.
*   **Immutability**: Logs are write-only.

---

## 6. INFRASTRUCTURE & CI/CD

*   **Build**: We use a custom **Self-Hosted Runner** because we cannot distribute `Streamer.bot.dll`.
*   **Pathing**: The runner hacks the `.csproj` logic by copying DLLs into place. Do not touch the `.csproj` references unless you know exactly what you are doing.

---

**Summary**:
1.  **Simple Code** (No Comments, Small Methods).
2.  **Strict Types** (Structs over Strings).
3.  **Sequential Processing** (Queues over Event Handlers).
4.  **Paranoid Locking** (Mutex over Hope).

**Do not break my bot.**
