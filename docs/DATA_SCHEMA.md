# Data Schema Documentation

## 1. Database Schema (LiteDB)
The application uses **LiteDB** (`pngtuber.db`) as its persistent storage. The database is strictly used for long-term retention and backup, while the KV Store handles high-frequency access.

### 1.1 `user_pronouns`
Stores pronoun preferences for users, resolved via Alejo.io or local cache.
| Field | Type | Description |
| :--- | :--- | :--- |
| `UserId` | String (Index) | Unique Platform ID (e.g., `twitch:12345`) |
| `Display` | String | Formatted display string (e.g. "He/Him") |
| `Subject` | String | he, she, they |
| `Object` | String | him, her, them |
| `Possessive` | String | his, her, their |
| `PossessivePronoun` | String | his, hers, theirs |
| `Reflexive` | String | himself, herself, themselves |
| `PastTense` | String | was, were |
| `CurrentTense` | String | is, are |
| `Plural` | Boolean | True if plural conjugation used |
| `LastUpdated` | DateTime | Last sync timestamp |

### 1.2 `user_nicknames`
Stores optional nicknames assigned by users via `!setnick`.
| Field | Type | Description |
| :--- | :--- | :--- |
| `UserId` | String (Index) | Unique Platform ID |
| `Nickname` | String | Custom nickname (max 30 chars) |
| `UpdatedAt` | DateTime | Last modification timestamp |

### 1.3 `knowledge_base`
Stores facts taught to the bot via `!teach` command.
| Field | Type | Description |
| :--- | :--- | :--- |
| `_id` | ObjectId | Auto-generated ID |
| `Key` | String (Index) | Search key (lowercase, trimmed) |
| `Content` | String | The fact content |
| `CreatedBy` | String | UserId who taught the fact |
| `CreatedAt` | DateTime | Creation timestamp |

### 1.4 `chat_logs`
Stores raw chat messages for archival purposes.
| Field | Type | Description |
| :--- | :--- | :--- |
| `_id` | ObjectId | Auto-generated ID |
| `Id` | String | Message ID from platform |
| `UserId` | String | Sender's User ID |
| `DisplayName` | String | Sender's Display Name |
| `Message` | String | Raw message content |
| `Timestamp` | DateTime (Index) | Event timestamp |
| `FullText` | String | Full log line |

---

## 2. KV Store Schema (In-Memory Cache)
The application uses `System.Runtime.Caching.MemoryCache` for shared state across microservices. This is the **primary** data source for the runtime pipeline.

| Key Pattern | Value Type | Lifespan | Description |
| :--- | :--- | :--- | :--- |
| `req_{RequestId}` | `RequestContext` | 10 mins | **The context carrier**. Mutable state passed between SEDA stages. |
| `pronouns_{UserId}` | `Pronouns` | 30 mins | Caches API/DB results to minimize external calls. |
| `nick_{UserId}` | `String` | 30 mins | Caches persistent nickname. |
| `knowledge_all` | `List<KnowledgeEntry>` | 60 mins | Caches entire KB for optimized searching. Invalidated on write. |
| `fact_{Key}` | `String` | 7 days | Caches individual facts for rapid lookup. |
| `chat_history` | `List<String>` | 1 day | Ring buffer of last 20 messages for LLM context. |

---

## 3. Queues (Buffers)
The application uses `System.Threading.Channels` (Unbounded) to connect microservices.

| Channel Name | Item Type | Producer | Consumer | Purpose |
| :--- | :--- | :--- | :--- | :--- |
| `IdentityChannel` | `String` (ID) | `BotEngine.Ingest` | `UserPronounService` | Entry point. Resolves User Identity. |
| `RouterChannel` | `String` (ID) | `UserPronounService` | `RouterService` | Decides path based on `TriggerType`. |
| `KnowledgeChannel` | `String` (ID) | `RouterService` | `UserKnowledgeService` | Retrieves facts for Chat events. |
| `CommandChannel` | `String` (ID) | `RouterService` | `CommandProcessingService` | Executes explicit commands. |
| `OutputChannel` | `String` (ID) | `UserKnowledgeService`, `CommandProcessingService` | `ResponseService` | Final stage. Logging & Chat Buffer. |
| `ChatPersistenceChannel` | `String` (ID) | `BotEngine.Ingest` | `TwitchChatMessageService` | Parallel firehose for logging to DB. |
