# Architecture Proposal: SEDA Pipeline (Staged Event-Driven Architecture)

Based on your feedback, we will move from a synchronous "Loop over Steps" model to a **Decoupled Pipeline** where each Service has its own dedicated Input Queue (Channel).

## The Flow
Data flows through a series of "Stages". Each Stage is a Consumer listening to a specific Channel.

```mermaid
graph TD
    Ingest(Ingest Event) --> |"Raw Event"| IngestChannel
    
    subgraph "Stage 1: Identity & Routing"
        IngestChannel --> IdentityService[UserPronounService]
        IdentityService --> |"Enriched (User/Pronouns)"| Router{Router}
    end
    
    subgraph "Stage 2: Processing"
        Router --> |"Chat Event"| KnowledgeChannel
        Router --> |"Command Event"| CommandChannel
        
        KnowledgeChannel --> KnowledgeService[UserKnowledgeService]
        CommandChannel --> CommandService[CommandProcessingService]
    end
    
    subgraph "Stage 3: Aggregation & LLM"
        KnowledgeService --> |"Context Ready"| MemoryChannel
        CommandService --> |"Result Ready"| OutputChannel
        
        MemoryChannel --> HistoryService[ChatHistoryService]
        HistoryService --> |"Buffer Updated"| PersistenceChannel
    end
    
    subgraph "Stage 4: Persistence & Output"
        PersistenceChannel --> PersistenceService[TwitchChatMessageService]
        OutputChannel --> Responder[ResponseService (Future TTS/LLM)]
    end
```

## The "1:1 Mapping"
Each "Consumer" Service defines its own:
1.  **Input Channel**: Where it gets work.
2.  **Processor**: The logic (e.g., "Look up Pronouns").
3.  **Output**: Pushes the mutated/enriched state to the *next* appropriate channel(s).

## Benefits
-   **Parallelism**: Identity lookups for User A don't block Command Processing for User B.
-   **Backpressure**: If the DB is slow, the Persistence Channel fills up, but Chat remains responsive (until RAM limits).
-   **Flexibility**: Easy to inject new stages (e.g., "ModerationFilter") by just rerouting a channel.

## Proposed Class Structure
-   `Core/Channels/IdentityChannel.cs`
-   `Core/Channels/KnowledgeChannel.cs`
-   `Core/Channels/ChatLogChannel.cs`
-   `Consumers/Identity/UserPronounService.cs` (Consumes `IdentityChannel`, Produces `KnowledgeChannel`)
-   `BotEngine` wires them all together (Dependency Injection).
