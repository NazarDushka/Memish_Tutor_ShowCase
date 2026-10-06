# Architecture Decision Records (ADR)

This log details the key engineering decisions made during the design and evolution of **Memish Tutor**.

---

### ADR-01: Modular Command Handler Pattern over Monolithic Switch-Case
* **Context**: Telegram updates contain varied payloads (commands, callback queries, payment checkouts). Handling them in a single service causes massive switch-statements and violates the Open/Closed Principle.
* **Decision**: Implemented an `ICommandHandler` interface registered as multiple scoped services in DI. `UpdateHandlerService` acts as a dispatcher resolving matching handlers dynamically.
* **Alternative Considered**: Telegram.Bot built-in wizard state machines or giant `switch (update.Type)` blocks.
* **Consequence**: Adding a new command (e.g., `/donate` or `/milestones`) requires creating an isolated class without touching existing code.

---

### ADR-02: Decoupling External AI via `ILLMClient` Interface
* **Context**: Image analysis and lesson extraction are handled by third-party multimodal LLMs (like Anthropic Claude Vision). Hardcoding the HTTP client logic into the business services creates tight coupling and makes local testing or provider swapping difficult.
* **Decision**: Abstracted all AI interactions behind the `ILLMClient` interface. The core lesson generation service solely relies on this contract. *(Note: For this showcase repository, the production `LLMClient` has been replaced with a `FakeLlmClient` to protect proprietary system prompts and allow local execution without API keys).*
* **Alternative Considered**: Directly injecting `HttpClient` and Anthropic-specific logic into the generation services.
* **Consequence**: Zero vendor lock-in. The system can seamlessly migrate from Claude to OpenAI, local Ollama models, or a static mock (as demonstrated in this showcase) simply by altering the DI container registration.

---

### ADR-03: Native .NET `BackgroundService` over External Schedulers (Hangfire/Quartz)
* **Context**: The app performs scheduled tasks: meme scraping, batch lesson synthesis, and daily broadcast delivery.
* **Decision**: Utilized native ASP.NET Core `IHostedService` / `BackgroundService` instances managed by the runtime lifecycle.
* **Alternative Considered**: Hangfire or Quartz.NET.
* **Trade-off**: Hangfire introduces dashboard management and extra schema tables. For current MVP scale, native `PeriodicTimer` and `Task.Delay` inside scoped execution blocks keep deployment lightweight with zero extra dependencies.

---

### ADR-04: MS SQL Server + Entity Framework Core with Code-First Migrations
* **Context**: Need reliable relational storage for users, streaks, lesson history, and anti-repetition delivery tracking.
* **Decision**: MS SQL Server 2022 via EF Core.
* **Alternative Considered**: SQLite or PostgreSQL.
* **Rationale**: Strong typing, LINQ expressiveness, and out-of-the-box support in enterprise .NET ecosystems. Migrations are executed at startup/CI deployment.

---

### ADR-05: Caddy Reverse Proxy with Automated TLS
* **Context**: Telegram Webhooks require strict HTTPS with valid public SSL certificates.
* **Decision**: Placed Caddy in front of Kestrel inside the Docker Compose network.
* **Alternative Considered**: NGINX + Certbot scripts or exposing Kestrel with custom certificate bindings.
* **Rationale**: Caddy manages automated Let's Encrypt renewal with a 3-line `Caddyfile` and zero maintenance overhead.

---

### ADR-06: Two-Phase Lesson Lifecycle (Scrape -> Ingest -> Batch Process)
* **Context**: Fetching images from external APIs and sending them to multimodal LLMs is prone to rate limits, network timeouts, and model latency (2-5s per meme).
* **Decision**: Separated ingestion into two stages: `MemeFetchingBackgroundService` dumps raw items into `RawMemes`, and `MemeProcessingBackgroundService` runs controlled batch transformations.
* **Consequence**: Decoupled resilience — if the meme API or Claude is temporarily unavailable, neither users nor the delivery pipeline are blocked.

---

### ADR-07: Structured Output Extraction & Fallback Parsing
* **Context**: Multimodal models occasionally return Markdown formatting (like ````json ... ````) despite system instructions.
* **Decision**: Built resilient extraction logic in `LLMClient` that isolates the innermost JSON payload, strips markdown artifacts, and validates against required schema before persisting.

