# Memish Tutor — System Architecture

This document outlines the high-level architecture, pipeline processing flow, and data domain model of Memish Tutor.

---

## 1. Automated Meme-to-Lesson Pipeline

Every 2 hours, background services fetch popular internet memes, filter appropriate content, call Claude Vision to generate micro-learning linguistic cards, and store structured lessons in MS SQL Server.

```mermaid
flowchart TD
    subgraph Sourcing
        API[External Meme API / Subreddits]
        BG_Fetch[MemeFetchingBackgroundService]
        RawMemes[(Raw Memes Table)]
    end

    subgraph Intelligence
        BG_Proc[MemeProcessingBackgroundService]
        LLM_Client{ILLMClient Interface}
        Claude[Anthropic Claude Haiku / Vision]
        Mock[FakeLlmClient / Local Stub]
        Lessons[(Lessons & Quizzes DB)]
    end

    subgraph Distribution
        BG_Delivery[DailyDeliveryBackgroundService]
        OnDemand[OnDemandLessonDeliveryService]
        Formatter[LessonMessageFormatter]
        TG_Sender[TelegramMediaSender]
        Users((Telegram Users))
    end

    API -->|Batch JSON| BG_Fetch
    BG_Fetch -->|Raw URLs & Meta| RawMemes
    RawMemes --> BG_Proc
    BG_Proc --> LLM_Client
    LLM_Client -.->|Production| Claude
    LLM_Client -.->|Showcase Mode| Mock
    LLM_Client -->|Structured JSON: Idiom, CEFR, Quiz| Lessons
    Lessons --> BG_Delivery
    Lessons --> OnDemand
    BG_Delivery --> Formatter
    OnDemand --> Formatter
    Formatter --> TG_Sender
    TG_Sender -->|Interactive Cards & Inline Quizzes| Users
```

---

## 2. Inbound Telegram Interaction Flow

Handles user commands (`/lesson`, `/feedback`, `/donate`), callback queries, and interactive quizzes via a modular Command Handler pattern.

```mermaid
sequenceDiagram
    autonumber
    actor User as Telegram User
    participant Caddy as Caddy (Reverse Proxy & SSL)
    participant Ctrl as TelegramController
    participant Router as UpdateHandlerService
    participant Handler as ICommandHandler (e.g. LessonCommandHandler)
    participant Delivery as OnDemandLessonDeliveryService
    participant DB as SQL Server (EF Core)

    User->>Caddy: Webhook POST /api/bot/webhook
    Caddy->>Ctrl: Reverse proxy :5001
    Ctrl->>Router: ProcessUpdateAsync(update)
    Router->>Handler: Match route ("lesson" / Callback)
    Handler->>Delivery: DeliverLessonAsync(userId)
    Delivery->>DB: Query unseen lesson + Record LessonDelivery
    DB-->>Delivery: Lesson entity & Quiz
    Delivery-->>User: Photo + Formatted Caption + Inline Keyboard
```

---

## 3. Domain Entity Model

```mermaid
erDiagram
    User ||--o{ LessonDelivery : receives
    User ||--o{ UserFeedback : leaves
    User ||--o{ UserMilestone : achieves
    User ||--o{ Referral : invites

    Lesson ||--o{ LessonDelivery : tracked_in
    Lesson ||--|| LessonQuiz : includes
    RawMeme ||--o| Lesson : transforms_into

    User {
        long TelegramChatId PK
        string Username
        int StreakDays
        DateTime LastActiveAt
        bool IsSubscribed
    }

    Lesson {
        Guid Id PK
        string ImageUrl
        string HeroPhrase
        string Explanation
        string Translation
        string SourceLanguage
        double Confidence
        DateTime CreatedAt
    }

    LessonQuiz {
        Guid Id PK
        Guid LessonId FK
        string OptionsJson
        int CorrectIndex
    }
```

