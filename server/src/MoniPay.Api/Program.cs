using MoniPay.Api.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddMoniPay();

WebApplication app = builder.Build();
await app.UseMoniPayAsync();

await app.RunAsync();

public partial class Program;
