using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pixata.AspNetCore.Auditing.Extensions;
using Pixata.AspNetCore.Extensions;
using Pixata.Blazor;
using Pixata.Blazor.Auditing.Extensions;
using Pixata.Blazor.Components;
using Pixata.Blazor.Containers;
using Pixata.Blazor.Sample.Data;
using Pixata.Email;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddTelerikBlazor();

SmtpSettings smtpSettings = builder.Configuration.GetSection("Smtp").Get<SmtpSettings>()!;
builder.Services.AddSingleton(smtpSettings);

builder.Services.AddTransient<PixataEmailServiceInterface, PixataEmailService>();
builder.Services.AddHttpClient();
builder.Services.AddPixataBlazor();
builder.Services.AddAuditing<SampleDbContext>();
builder.Services.AddPixataAuditViewer();
builder.Services.AddDbContext<SampleDbContext>((serviceProvider, options) =>
  options.UseInMemoryDatabase("SampleAuditDb")
    .AddAuditingInterceptor(serviceProvider));

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment()) {
  app.UseDeveloperExceptionPage();
} else {
  app.UseExceptionHandler("/Error");
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapPixataAspNetCoreApiEndpoints(["/_blazor", "/_framework", "/_content", "/hello"]);
app.MapGet("/hello", () => "Hello!");
app.MapGet("/bye", () => "Bye!");
app.MapGet("/hello-world", () => "Hello, world!");

using (IServiceScope scope = app.Services.CreateScope()) {
  SampleDbContext db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
  SampleDataSeeder.Seed(db);
}

ApiResponseViewConfig.LogInUrl = "LogIn";
SitePageTitle.SiteName = "Pixata.Blazor sample";

app.Run();