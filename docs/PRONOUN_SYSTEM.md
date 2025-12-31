# Pronoun System Architecture

This document details the **Identity Resolution Engine** used in PNGTuber-GPTv2. It is designed to be **fault-tolerant**, **high-performance**, and **privacy-respecting**.

## 1. Core Philosophy: "Efficiency through Layering"

Resolving pronouns for every chat message is expensive if done wrong. Calling an API for every message would result in rate-limiting and 300ms+ latency.

We solve this with a **3-Layer Defense-in-Depth Strategy**:

| Layer | Type | Speed | TTL (Time-To-Live) | Responsibility |
|-------|------|-------|--------------------|----------------|
| **L1** | **Memory Cache** | Nanoseconds | 30 Minutes | Instant access for active chatters. |
| **L2** | **LiteDB** | <5ms | 7 Days | Persistent storage across reboots. |
| **L3** | **Alejo.io API** | ~500ms | N/A | Source of Truth (External). |

---

## 2. The Resolution Flow (`PronounRepository`)

When `GetPronounsAsync(userId)` is called:

### Step 1: L1 Check (RAM)
- We query `ICacheService` for `pronouns_{userId}`.
- **Hit**: Return immediately. **(99% of traffic)**
- **Miss**: Proceed to Step 2.

### Step 2: L2 Check (Disk)
- We acquire a `DatabaseMutex` (Read Lock).
- We query the `user_pronouns` collection in LiteDB.
- **Hit**: 
    - Check `LastUpdated` timestamp.
    - If `< 7 Days Old`: Update L1 Cache and Return.
    - If `> 7 Days Old`: Mark as "Stale" and proceed to Step 3 (Background Refresh).
- **Miss**: Proceed to Step 3.

### Step 3: L3 Fetch (API)
- We call `https://pronouns.alejo.io/api/users/{userId}`.
- **Success**: 
    - Parse JSON (He/Him, She/Her, etc).
    - Acquire `DatabaseMutex` (Write Lock).
    - **Upsert** to LiteDB with `LastUpdated = Now`.
    - Update L1 Cache.
    - Return Result.
- **Failure** (API Down/404):
    - Return `Pronouns.TheyThem` (Safe Default) OR the Stale L2 value if available.
    - Do *not* crash correctly.

---

## 3. Data Structures

### 3.1 `Pronouns` Struct (Immutable)
We use a **Read-Only Struct** to pass pronouns around. This ensures that no downstream consumer (like the LLM or TTS) can accidentally modify a user's identity.

```csharp
public readonly struct Pronouns
{
    public string Display { get; }      // "He/Him"
    public string Subject { get; }      // "He"
    public string Object { get; }       // "Him"
    public string Possessive { get; }    // "His"
    // ...
}
```

### 3.2 Database Schema (`user_pronouns`)
```json
{
    "_id": {"$oid": "..." },
    "UserId": "twitch:12345",
    "Display": "He/Him",
    "Subject": "He",
    "Object": "Him",
    "LastUpdated": {"$date": "2023-12-31T12:00:00Z"}
}
```

---

## 4. API Integration (Alejo.io)

We use the [Alejo.io Pronouns API](https://pronouns.alejo.io/), which is the de-facto standard for Twitch pronouns (compatible with 7TV/FrankerFaceZ).

- **Endpoint**: `GET /api/users/{login_or_id}`
- **Mapping**:
    - `hehim` -> He/Him
    - `sheher` -> She/Her
    - `theythem` -> They/Them
    - `other` -> They/Them (Fallback)

---

## 5. Concurrency & Safety

- **Thread Safety**: L1 Cache (`MemoryCache`) is internally thread-safe.
- **Process Safety**: L2 Access is protected by `DatabaseMutex` (`Global\PNGTuber-GPTv2-DB-Lock`), ensuring that if multiple instances of the bot run, they don't corrupt the DB.
- **Fail-Safe**: If any layer fails, it gracefully degrades to the next, preventing the bot from crashing on network errors.
