# FlowMetrics

> A personal income streams dashboard that answers: **Where am I? Where am I going? Where will I be?**

## The Problem

Personal finance applications are obsessed with expenses. Mint, YNAB, Monarch - they all focus on budgeting and expense categorization. Net worth apps like Kubera show static wealth, not income flows.

**There's no tool that answers:**
- How much am I generating per day from my multiple sources?
- What's the trend?
- Where will I be in 6 months?

The visual pattern we need already exists - but in cloud billing tools like Azure Cost Explorer, AWS Billing, and GCP Cost Management. Only applied in reverse: not "how much I spend" but **"how much I generate"**.

## The Personal Pain Point

As a professional with multiple income streams (employment, trading, referrals, subscriptions, side projects), I face a daily challenge: **I have no unified view of my financial inflows**.

Each income source lives in its own silo:
- Exchange dashboards show trading PnL
- Bank statements show salary deposits
- Platform dashboards show creator earnings
- Spreadsheets (maybe) try to consolidate everything

The result? I can't answer simple questions:
- What's my true daily income rate across all sources?
- Which streams are growing vs declining?
- How long until I reach my financial goals?
- What happens if one stream disappears?

### Where I Am (Current State)
- Multiple income sources scattered across platforms
- Manual tracking in spreadsheets (inconsistent, time-consuming)
- No real-time visibility into combined earnings
- Unable to spot trends or patterns

### Where I'm Going (The Journey)
- Building a unified dashboard that aggregates all income streams
- Implementing automatic sync with exchanges and platforms
- Creating statistical analysis to understand income stability
- Developing projections based on historical data

### Where I'll Be (Target State)
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
- [ ] Project structure setup (Clean Architecture)
- [ ] Database schema and EF Core configuration
- [ ] Domain entities and value objects
- [ ] Basic Blazor Server UI scaffold

### Phase 2: Core Functionality
- [ ] Connection management module
- [ ] Manual income entry
- [ ] First exchange integration
- [ ] Basic dashboard with charts

### Phase 3: Analytics
- [ ] Statistical calculations
- [ ] Trend analysis
- [ ] Simple projections
- [ ] Multi-currency support

### Phase 4: Polish
- [ ] Additional integrations
- [ ] Advanced projections (Monte Carlo)
- [ ] Alerts and notifications
- [ ] Performance optimization

## Target Audience

While built for personal use, FlowMetrics addresses a common need among:

- **Active traders** with multiple exchange accounts
- **Content creators** with Patreon/YouTube/Substack income
- **Freelancers** juggling multiple clients
- **Side hustlers** balancing employment with projects
- **SaaS founders** tracking MRR alongside consulting

## License

This is a personal project. License TBD based on future direction.

---

*Built with Blazor Server, .NET 10, and a passion for financial clarity.*
