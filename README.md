# FlowMetrics
> An income streams dashboard that answers: **Where am I? Where am I going? Where will I be?**

<img width="1303" height="866" alt="flow-metrics-screenshot" src="https://github.com/user-attachments/assets/902dfc48-5eb2-4ce1-aa30-13c9ee89856b" />



## The Problem

Personal finance applications are obsessed with expenses. Mint, YNAB, Monarch - they all focus on budgeting and expense categorization. Net worth apps like Kubera show static wealth, not income flows.

**There's no tool that answers:**
- How much am I generating per day from multiple sources?
- What's the trend?
- Where will I be in 6 months?

The visual pattern we need already exists - but in cloud billing tools like Azure Cost Explorer, AWS Billing, and GCP Cost Management. Only applied in reverse: not "how much I spend" but **"how much I generate"**.

## The Pain Point

Modern professionals increasingly have diversified income sources. Traders, content creators, freelancers, consultants, SaaS founders - they all face the same challenge: **no unified view of financial inflows**.

Each income source lives in its own silo:
- Exchange dashboards show trading PnL
- Bank statements show salary deposits
- Platform dashboards show creator earnings
- Spreadsheets (maybe) try to consolidate everything

The result? Simple questions become impossible to answer:
- What's the true daily income rate across all sources?
- Which streams are growing vs declining?
- How long until reaching financial goals?
- What happens if one stream disappears?

### Current State (Where Am I?)
- Income sources scattered across platforms
- Manual tracking in spreadsheets (inconsistent, time-consuming)
- No real-time visibility into combined earnings
- Unable to spot trends or patterns

### The Journey (Where Am I Going?)
- A unified dashboard that aggregates all income streams
- Automatic sync with exchanges and platforms
- Statistical analysis to understand income stability
- Projections based on historical data

### Target State (Where Will I Be?)
- Complete visibility of all income flows in one place
- Daily normalized view for comparing different income types
- Trend analysis showing momentum and growth
- Financial projections with confidence intervals
- Data-driven decisions about which streams to grow or drop

## Project Scope

### Core Features

| Feature | Description |
|---------|-------------|
| **Multi-Source Aggregation** | Connect multiple income sources (exchanges, platforms, manual entries) |
| **Daily Normalization** | Convert all income to daily rates for fair comparison |
| **Stacked Area Charts** | Azure Cost Explorer-style visualization of income streams |
| **Statistical Analysis** | Mean, median, standard deviation, coefficient of variation |
| **Trend Detection** | Moving averages, MoM growth, seasonality analysis |
| **Financial Projections** | Linear projections, percentile scenarios, time-to-goal |
| **Connection Management** | Multiple accounts per provider, health checks, sync logs |

### Secondary Features (Future)
- Monte Carlo simulation for confidence ranges
- Alerts and notifications for anomalies
- Goal tracking and milestones
- Mobile PWA support
- Report exports (PDF, CSV)

## Technical Implementation

### Stack

| Layer | Technology |
|-------|------------|
| **Frontend** | Blazor Server (.NET 10) |
| **Backend** | ASP.NET Core 10 (Minimal APIs) |
| **Database** | PostgreSQL (Neon Serverless) |
| **ORM** | Entity Framework Core |
| **Background Jobs** | IHostedService / Hangfire |
| **Architecture** | Clean Architecture + Modular Monolith |

### Architecture Goals

This project serves a dual purpose:

1. **Solve a real problem** - Build a functional income tracking dashboard
2. **Demonstrate modern C# patterns** - Showcase Clean Architecture and Modular Monolith in a real-world application

#### Clean Architecture Layers
```
src/
├── FlowMetrics.Domain/           # Entities, Value Objects, Domain Events
├── FlowMetrics.Application/      # Use Cases, DTOs, Interfaces
├── FlowMetrics.Infrastructure/   # EF Core, External APIs, Services
└── FlowMetrics.Web/              # Blazor Server UI
```

#### Modular Monolith Structure
```
src/
├── Modules/
│   ├── Connections/              # Provider connections management
│   ├── Income/                   # Income records and aggregation
│   ├── Analytics/                # Statistics and projections
│   └── Sync/                     # Background sync jobs
└── Shared/
    ├── Kernel/                   # Shared domain primitives
    └── Infrastructure/           # Cross-cutting concerns
```

### Modern C# Features (.NET 10)

- Primary constructors
- Collection expressions
- Required members
- File-scoped types
- Pattern matching enhancements
- Records for DTOs and Value Objects
- Source generators where applicable

## Roadmap

### Phase 1: Foundation
- [x] Project structure setup (Clean Architecture)
- [x] Database schema and EF Core configuration
- [x] Domain entities and value objects
- [x] Basic Blazor Server UI scaffold

### Phase 2: Core Functionality
- [x] Connection management module
- [x] Manual income entry
- [ ] First exchange integration (plugin architecture ready)
- [x] Basic dashboard with charts

### Phase 3: Analytics
- [x] Statistical calculations
- [x] Trend analysis
- [x] Simple projections
- [ ] Multi-currency support (basic conversion exists)

### Phase 4: Polish
- [ ] Additional integrations
- [ ] Advanced projections (Monte Carlo)
- [ ] Alerts and notifications
- [ ] Performance optimization

## Target Audience

- **Active traders** with multiple exchange accounts
- **Content creators** with Patreon/YouTube/Substack income
- **Freelancers** juggling multiple clients
- **Side hustlers** balancing employment with projects
- **SaaS founders** tracking MRR alongside consulting

## License

MIT License - See LICENSE file for details.

---

*Built with Blazor Server, .NET 10, and Clean Architecture principles.*
