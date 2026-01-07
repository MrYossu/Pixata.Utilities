using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pixata.Blazor.Containers;
using Pixata.Blazor.Sample.Data;
using Pixata.Email;

namespace Pixata.Blazor.Sample {
  public class Startup(IConfiguration configuration) {
    public IConfiguration Configuration { get; } = configuration;

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services) {
      services.AddRazorPages();
      services.AddServerSideBlazor();
      services.AddSingleton<WeatherForecastService>();
      services.AddTelerikBlazor();
      SmtpSettings smtpSettings = Configuration.GetSection("Smtp").Get<SmtpSettings>();
      services.AddSingleton(smtpSettings);
      services.AddTransient<PixataEmailServiceInterface, PixataEmailService>();
      services.AddHttpClient();
      services.AddPixataBlazor();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
      if (env.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
      } else {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }

      app.UseHttpsRedirection();
      app.UseStaticFiles();

      app.UseRouting();

      app.UseEndpoints(endpoints => {
        endpoints.MapBlazorHub();
        endpoints.MapFallbackToPage("/_Host");
      });

      // As this sample does not use authentication, the following line is not actually needed, but is included here to show how to set up the link to your log-in page
      ApiResponseViewConfig.LogInUrl = "LogIn";
    }
  }
}