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

## Core Features

| Feature | Description |
|---------|-------------|
| **Multi-Source Aggregation** | Connect multiple income sources (exchanges, platforms, manual entries) |
| **Plugin-Based Connectors** | Extensible connector system for adding new integrations |
| **Real-Time Sync** | Background jobs sync data every 5 minutes with live Activity Log |
| **Daily Normalization** | Convert all income to daily rates for fair comparison |
| **Stacked Area Charts** | Azure Cost Explorer-style visualization of income streams |
| **Statistical Analysis** | Mean, median, standard deviation, trend detection |
| **Financial Projections** | 6-month projections with confidence scoring |
| **Live Dashboard** | Auto-refresh UI updates every 10 seconds |

### Supported Connectors

| Connector | Type | Description |
|-----------|------|-------------|
| **Blofin Exchange** | Syncable | Crypto exchange - syncs total balance in USD |
| **Recurring Income** | Recurring | Manual entry for salary, rent, subscriptions |

## Technical Stack

| Layer | Technology |
|-------|------------|
| **Frontend** | Blazor Server (.NET 10) + MudBlazor |
| **Charts** | ApexCharts.Blazor |
| **Database** | PostgreSQL |
| **ORM** | Entity Framework Core |
| **Background Jobs** | Native BackgroundService (IHostedService) |
| **Architecture** | Clean Architecture + Modular Monolith |
| **Results** | FluentResults for railway-oriented programming |

### Architecture

```
Income/src/
├── Connectors/                    # Plugin-based connector system
│   └── Connectors.Blofin/         # Blofin exchange connector
├── Host/
│   └── Dashboard/                 # Blazor Server UI
└── Modules/
    ├── Income/                    # Income streams, snapshots, providers
    │   ├── Income.Application/    # Services, interfaces, DTOs
    │   ├── Income.Domain/         # Entities, value objects
    │   ├── Income.Infrastructure/ # EF Core, jobs, services
    │   ├── Income.Contracts/      # Cross-module DTOs
    │   └── Income.Installer/      # DI registration
    └── Analytics/                 # Statistics and projections
        ├── Analytics.Application/
        └── Analytics.Infrastructure/
```

### Background Jobs

| Job | Interval | Purpose |
|-----|----------|---------|
| **SyncJob** | 5 min | Syncs data from API-based connectors (Blofin) |
| **RecurringJob** | 5 min | Generates snapshots for recurring income streams |
| **TestDataGeneratorJob** | 5 min | Generates random test data (dev only) |

### Modern C# Features (.NET 10)

- Primary constructors
- Collection expressions
- Required members
- File-scoped types
- Pattern matching
- Records for DTOs and Value Objects

## Roadmap

### Phase 1: Foundation
- [x] Project structure setup (Clean Architecture)
- [x] Database schema and EF Core configuration
- [x] Domain entities and value objects
- [x] Blazor Server UI with MudBlazor

### Phase 2: Core Functionality
- [x] Provider and stream management
- [x] Manual/recurring income entry
- [x] Plugin-based connector architecture
- [x] Blofin exchange integration
- [x] Real-time Activity Log
- [x] Auto-refresh dashboard

### Phase 3: Analytics
- [x] Statistical calculations (daily rate, trends)
- [x] Stream health analysis
- [x] 6-month projections with confidence
- [ ] Multi-currency support with live rates

### Phase 4: Expansion
- [ ] Additional exchange connectors (Binance, Bybit)
- [ ] Bank integrations (Plaid)
- [ ] Advanced projections (Monte Carlo)
- [ ] Alerts and notifications
- [ ] Mobile PWA support

## Getting Started

### Prerequisites
- .NET 10 SDK
- Docker & Docker Compose
- PostgreSQL (or use Docker)

### Running with Docker

```bash
cd Income
docker-compose up -d
```

Access the dashboard at `http://localhost:5000`

### Development

```bash
cd Income
dotnet restore
dotnet run --project src/Host/Dashboard
```

## Target Audience

- **Active traders** with multiple exchange accounts
- **Content creators** with Patreon/YouTube/Substack income
- **Freelancers** juggling multiple clients
- **Side hustlers** balancing employment with projects
- **SaaS founders** tracking MRR alongside consulting

## License

MIT License - See LICENSE file for details.

---

*Built with Blazor Server, .NET 10, MudBlazor, and Clean Architecture principles.*
