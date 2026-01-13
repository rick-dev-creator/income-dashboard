using Dashboard.Components;
using Dashboard.Services;
using Income.Installer;
using Analytics.Installer;
using Connectors.Blofin;
using Connectors.Toobit;
using MudBlazor;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=flowmetrics;Username=postgres;Password=postgres";

// Modern theme configuration
var theme = new MudTheme
{
    PaletteLight = new PaletteLight
    {
        Primary = "#6366f1",
        PrimaryDarken = "#4f46e5",
        PrimaryLighten = "#818cf8",
        Secondary = "#ec4899",
        SecondaryDarken = "#db2777",
        SecondaryLighten = "#f472b6",
        Tertiary = "#14b8a6",
        Success = "#10b981",
        Warning = "#f59e0b",
        Error = "#ef4444",
        Info = "#3b82f6",
        Background = "#f8fafc",
        Surface = "#ffffff",
        DrawerBackground = "#ffffff",
        DrawerText = "#1e293b",
        AppbarBackground = "#ffffff",
        AppbarText = "#1e293b",
        TextPrimary = "#0f172a",
        TextSecondary = "#64748b",
        ActionDefault = "#64748b",
        ActionDisabled = "#94a3b8",
        Divider = "#e2e8f0",
        DividerLight = "#f1f5f9",
        TableLines = "#e2e8f0",
        LinesDefault = "#e2e8f0",
        LinesInputs = "#cbd5e1",
        HoverOpacity = 0.04,
        GrayDefault = "#64748b",
        GrayLight = "#94a3b8",
        GrayLighter = "#cbd5e1",
        GrayDark = "#475569",
        GrayDarker = "#334155"
    },
    PaletteDark = new PaletteDark
    {
        Primary = "#818cf8",
        PrimaryDarken = "#6366f1",
        PrimaryLighten = "#a5b4fc",
        Secondary = "#f472b6",
        SecondaryDarken = "#ec4899",
        SecondaryLighten = "#f9a8d4",
        Tertiary = "#2dd4bf",
        Success = "#34d399",
        Warning = "#fbbf24",
        Error = "#f87171",
        Info = "#60a5fa",
        Black = "#0f172a",
        Background = "#0f172a",
        Surface = "#1e293b",
        DrawerBackground = "#0f172a",
        DrawerText = "#e2e8f0",
        AppbarBackground = "rgba(15, 23, 42, 0.8)",
        AppbarText = "#f1f5f9",
        TextPrimary = "#f1f5f9",
        TextSecondary = "#94a3b8",
        ActionDefault = "#94a3b8",
        ActionDisabled = "#475569",
        Divider = "#334155",
        DividerLight = "#1e293b",
        TableLines = "#334155",
        LinesDefault = "#334155",
        LinesInputs = "#475569",
        HoverOpacity = 0.08,
        GrayDefault = "#94a3b8",
        GrayLight = "#cbd5e1",
        GrayLighter = "#e2e8f0",
        GrayDark = "#64748b",
        GrayDarker = "#475569"
    },
    Typography = new Typography
    {
        Default = new DefaultTypography
        {
            FontFamily = ["Inter", "system-ui", "-apple-system", "sans-serif"],
            FontSize = "0.875rem",
            FontWeight = "400",
            LineHeight = "1.5",
            LetterSpacing = "normal"
        },
        H1 = new H1Typography { FontWeight = "700", FontSize = "2.5rem", LineHeight = "1.2" },
        H2 = new H2Typography { FontWeight = "700", FontSize = "2rem", LineHeight = "1.25" },
        H3 = new H3Typography { FontWeight = "600", FontSize = "1.75rem", LineHeight = "1.3" },
        H4 = new H4Typography { FontWeight = "600", FontSize = "1.5rem", LineHeight = "1.35" },
        H5 = new H5Typography { FontWeight = "600", FontSize = "1.25rem", LineHeight = "1.4" },
        H6 = new H6Typography { FontWeight = "600", FontSize = "1rem", LineHeight = "1.5" },
        Subtitle1 = new Subtitle1Typography { FontWeight = "500", FontSize = "1rem" },
        Subtitle2 = new Subtitle2Typography { FontWeight = "500", FontSize = "0.875rem" },
        Body1 = new Body1Typography { FontSize = "0.9375rem", LineHeight = "1.6" },
        Body2 = new Body2Typography { FontSize = "0.875rem", LineHeight = "1.5" },
        Caption = new CaptionTypography { FontSize = "0.75rem", FontWeight = "400" },
        Overline = new OverlineTypography { FontSize = "0.6875rem", FontWeight = "600", LetterSpacing = "0.08em" }
    },
    LayoutProperties = new LayoutProperties
    {
        DefaultBorderRadius = "12px",
        DrawerWidthLeft = "280px"
    }
};

builder.Services
    .AddIncomeModule(connectionString)
    .AddAnalyticsModule()
    .AddBlofinConnector()
    .AddToobitConnector()
    .AddMudServices(config =>
    {
        config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
        config.SnackbarConfiguration.ShowCloseIcon = true;
        config.SnackbarConfiguration.VisibleStateDuration = 3000;
        config.SnackbarConfiguration.HideTransitionDuration = 300;
        config.SnackbarConfiguration.ShowTransitionDuration = 300;
        config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
    })
    .AddSingleton(theme)
    .AddScoped<AppState>(); // App-wide state for mode switching

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });

var app = builder.Build();

// Initialize database and seed data
await app.Services.InitializeIncomeDatabaseAsync();
await app.Services.SeedIncomeDatabaseAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
