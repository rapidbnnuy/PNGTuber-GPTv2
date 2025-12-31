# PNGTuber-GPTv2: Architecture & Design Strategy

> **"Slow is Smooth, Smooth is Fast."**

## 1. Executive Summary

This document details the architectural decisions behind PNGTuber-GPTv2. The goal is to create a Twitch Integration plugin that is **crash-proof**, **highly scalable**, and **developer-friendly**, handling the chaotic nature of live streaming (bursty chat, API failures) without freezing the UI or corrupting data.

## 2. Core Architectural Pattern: The Sequential Enrichment Pipeline

We moved away from the traditional "Event Handler" spaghetti code (where one function does everything) to a **Sequential Pipeline**.

### 2.1 The Problem it Solves
In traditional bots, `OnChatMessage` runs logic, calls APIs, and writes DBs.
- **Race Condition**: If user types twice fast, two threads modify their karma simultaneously.
- **UI Freeze**: Waiting for OpenAI to reply (3s) blocks the main thread.
- **Bug Complexity**: 10 things happening at once is hard to debug.

### 2.2 The Solution
We treat every event (Chat, Subscription, Raid) as a **Data Packet** that flows through a factory line.

1.  **Ingestion (The Brain)**: 
    *   Receives the raw event.
    *   Creates a `RequestContext` (Unique ID).
    *   Queues it. **Returns immediately.**
2.  **Enrichment (The Steps)**:
    *   A single background worker picks up the Context ID.
    *   **IdentityStep**: "Who is this?" (Resolves User/Pronouns from Cache/DB).
    *   **ModerationStep**: "Is this safe?" (scans text).
    *   **LogicStep**: "What do I do?" (LLM generation).
3.  **Action (The Sink)**:
    *   Finalizes state.
    *   Persists to Database (Nuclear-safe lock).
    *   Sends output to Chat.

**Why this manages complexity**: Each "Step" is a tiny, isolated class. You can write a unit test for `IdentityStep` without needing OpenAI or LiteDB.

---

## 3. Concurrency & Safety Strategy

We employ a "Defense in Depth" strategy against race conditions.

### Defensive Ring 1: Thread-Safe Buffering
*   **Mechanism**: `System.Threading.Channels`.
*   **Effect**: Handles highly bursty traffic. If 1,000 messages arrive in 1 second, they sit in the queue. The processing never chokes.
*   **Constraint**: The "Brain" is the ONLY entry point. No side doors.

### Defensive Ring 2: In-Memory State Isolation
*   **Mechanism**: `ICacheService` + `RequestContext`.
*   **Effect**: 
    *   Reference data (Users, Keywords) is cached in RAM.
    *   Mutable state (The current request) lives in an isolated `RequestContext` object.
    *   Steps do not share global variables. They only touch the Context they are given.

### Defensive Ring 3: Exclusive Database Persistence
*   **Mechanism**: `DatabaseMutex` (Named System Mutex).
*   **Effect**: 
    *   Database writes are effectively single-threaded at the OS level.
    *   Impossible for two threads (or two instances of Streamer.bot) to corrupt the `.db` file.
    *   **Self-Healing**: If a crash leaves a lock file, `PruneLockFile()` exists to recover.

---

## 4. Performance Optimization

*   **Zero-Block Ingestion**: Costs <0.1ms to accept a chat message.
*   **Memory Caching**: We use `System.Runtime.Caching` to avoid hitting the disk for repeat users. Getting "Rapid's Pronouns" takes nanoseconds after the first lookup.
*   **Lazy Loading**: The database connection is only opened when a specific Step needs to WRITE. We don't hold the file open idle.

## 5. Ease of Use & Developer Experience

*   **Modular Steps**: want to add a "Raid Welcome" feature? Just write a class implementing `IPipelineStep` and add it to the Brain's list. You don't touch the existing code.
*   **Strict Types**: We use Immutable Structs (`Pronouns`, `ChatMessage`) where possible. You can't accidentally change a pronoun "in transit".
*   **Clear Logs**: Because functionality is separated, logs clearly show `[IdentityStep] Resolved User` -> `[LLMStep] Generated Text`. Tracing bugs becomes linear reading.
