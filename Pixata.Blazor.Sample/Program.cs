using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pixata.AspNetCore.Auditing.Extensions;
using Pixata.AspNetCore.Extensions;
using Pixata.AspNetCore.Upload;
using Pixata.Blazor;
using Pixata.Blazor.Auditing.Extensions;
using Pixata.Blazor.Components;
using Pixata.Blazor.Containers;
using Pixata.Blazor.Sample;
using Pixata.Blazor.Sample.Data;
using Pixata.Email;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
// The editor sample's documents (eg with a pasted image) can be bigger than SignalR's default limit of 32 KB per message
builder.Services.AddServerSideBlazor()
  .AddHubOptions(options => options.MaximumReceiveMessageSize = 1024 * 1024);
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddScoped<GridSampleEmployeeService>();
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

// The endpoints the file upload sample posts to. The files go to a temp folder (never wwwroot, where anyone could download
// them), and as this sample is public, they are limited to 5 MB and deleted after a few minutes
string uploadFolder = Path.Combine(Path.GetTempPath(), "Pixata.Blazor.Sample.Uploads");
app.MapPost(RoutesHelper.UploadSave, async (HttpRequest request, int? delay) => {
  DeleteOldUploads(uploadFolder);
  UploadReceiveResult result = await request.ReceiveUpload(uploadFolder, o => o.MaxFileSize = 5 * 1024 * 1024);
  if (delay is > 0) {
    await Task.Delay(Math.Min(delay.Value, 2000));
  }
  return result.Success
    ? Results.Ok(new { uploaded = result.Complete, files = result.Files.Select(f => f.FileName) })
    : Results.BadRequest(result.Error);
});
app.MapPost(RoutesHelper.UploadRemove, async (HttpRequest request) =>
  Results.Ok(await request.RemoveUploads(uploadFolder)));
app.MapPost(RoutesHelper.UploadFail, () =>
  Results.Problem("This endpoint always fails, so you can see what the upload components do when the server reports an error"));

using (IServiceScope scope = app.Services.CreateScope()) {
  SampleDbContext db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
  SampleDataSeeder.Seed(db);
  SampleDataSeeder.SeedEmployees(db);
}

ApiResponseViewConfig.LogInUrl = "LogIn";
SitePageTitle.SiteName = "Pixata.Blazor sample";

app.Run();

static void DeleteOldUploads(string folder) {
  if (!Directory.Exists(folder)) {
    return;
  }
  DateTime cutOff = DateTime.UtcNow.AddMinutes(-10);
  foreach (string path in Directory.EnumerateFiles(folder).Where(p => File.GetLastWriteTimeUtc(p) < cutOff)) {
    try {
      File.Delete(path);
    }
    catch (IOException) {
      // Being written to by another request, so leave it for next time
    }
  }
}