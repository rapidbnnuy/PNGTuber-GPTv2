# PNGTuber-GPTv2 LiteDB Schema

This document defines the data models for the project. We utilize a **Relational Pattern** within LiteDB to ensure minimal data duplication and high consistency.

## 1. Core Principles
- **Separation of Concerns**: User profile data is distinct from their memories and pronoun mappings.
- **References**: Relationships are stored via IDs (`UserId`, `PronounId`).
- **Immutability**: Event logs are write-only.
- **Finite Space**: Specific fields (Memory, Definitions) have strict character limits (e.g., 500 chars) that are overwritten, not appended.

## 2. Collections & Models

### 2.1 Settings (`settings`)
Stores configuration categories.
- **`_id`** (String): The category name (e.g., "Global", "Moderation", "Voice").
- **`Config`** (Object): The typed configuration object for that category.

```csharp
public class SettingDocument<T>
{
    [BsonId]
    public string Category { get; set; } // "Global", "Moderation"
    public T Settings { get; set; }
}
```

### 2.2 Users (`users`)
Basic user profile tracking. Single source of truth for identity.
- **`_id`** (String): `Platform:Id` (e.g., `twitch:123456`).
- **`DisplayName`** (String): Current display name.
- **`FirstSeen`** (DateTime)
- **`LastSeen`** (DateTime)
- **`Karma`** (Int)

### 2.3 Pronouns System (`pronouns`)
Normalized storage for complex pronoun grammar.
- **`_id`** (Int): Auto-Incremented ID.
- **`Name`**: (String) e.g. "He/Him"
- **`Subject`** (String): "He"
- **`Object`** (String): "Him"
- **`Possessive`** (String): "His" (Adjective: "It is his book")
- **`PossessivePronoun`** (String): "His" (Nominal: "The book is his")
- **`Reflexive`** (String): "Himself"
- **`PastTense`** (String): "Was"
- **`CurrentTense`** (String): "Is"
- **`Plural`** (Bool): False
*Note: We do not store "Lower" variants. We compute `.ToLowerInvariant()` at runtime.*

- **`Plural`** (Bool): False
*Note: We do not store "Lower" variants. We compute `.ToLowerInvariant()` at runtime.*

#### `user_pronouns` Collection
Mapping table linking Users to Pronouns.
- **`_id`** (ObjectId): Internal ID.
- **`UserId`** (String): Reference to `users._id`.
- **`PronounId`** (Int): Reference to `pronouns._id`.

### 2.4 "Pronoun-First" Caching Strategy
**Critical Design Requirement**: We respect identity above all else.
- **Source of Truth**: The `user_pronouns` DB collection.
- **Runtime Access**: **NEVER** query the DB for pronouns on every message.
- **Cache Layer**: A `ConcurrentDictionary<string, PronounStruct>` MUST be maintained in memory.
  - **Read**: Always read from Cache.
  - **Write**: Update DB -> Update Cache.
  - **Expiration**: Cache entries are long-lived (users rarely change pronouns).

### 2.5 User Knowledge (`user_memory`)
Specific facts the bot "knows" about a user. Overwrite-only logic.
- **`_id`** (String): Reference to `users._id` (One-to-One).
- **`Memory`** (String): The knowledge content. **Limit: 500 chars**.
- **`LastUpdated`** (DateTime)

### 2.5 Glossary (`keywords`)
Definitions for specific terms.
- **`_id`** (String): The Keyword (Normalized/Lowercase).
- **`Definition`** (String): The explanation. **Limit: 500 chars**.
- **`CreatedBy`** (String): UserId ref.

### 2.6 Event Log (`events`)
Immutable log of inbound triggers.
- **`_id`** (ObjectId): Time-sortable.
- **`Type`** (Enum): `ChatMessage`, `Follow`, `Cheer`, etc.
- **`UserId`** (String): Reference to `users._id`.
- **`Payload`** (Struct): The **Normalized** arguments.
  - **CRITICAL**: The Payload MUST be stripped of all redundant data (e.g., Pronouns, UserBadges, SubStatus) that is already tracked in the `users` or `user_pronouns` tables.
- **`Timestamp`** (DateTime)

### 2.7 Interactions (`interactions`)
Log of Q&A exchanges.
- **`_id`** (ObjectId)
- **`UserId`** (String): Ref to `users._id`.
- **`Input`** (String): The user's question/message.
- **`Response`** (String): The bot's generated response.
- **`ModelUsed`** (String): e.g., "GPT-4o".

## 3. Data Constraints
- **Foreign Keys**: Enforced via code in the Repository layer.
- **String Limits**: `Memory` and `Definition` fields MUST be truncated to 500 chars before insert/update.
