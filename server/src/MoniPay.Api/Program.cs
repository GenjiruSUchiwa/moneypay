using MoniPay.Api.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddMoniPay();

WebApplication app = builder.Build();
await app.UseMoniPayAsync();

await app.RunAsync();

/// <summary>Named so the integration test harness can boot the real host through
/// <c>WebApplicationFactory&lt;Program&gt;</c>. It is public because xunit requires a public test
/// class, and a public test class cannot name an internal entry point; everything else the tests
/// reach stays internal, through the solution-wide InternalsVisibleTo.</summary>
public partial class Program;
