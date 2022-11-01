using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;
using PasswordManagerApp.Models;

namespace PasswordManagerApp
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      /* services.Configure<CookiePolicyOptions>(options =>
      {
        // This lambda determines whether user consent for non-essential cookies is needed for a given request.
        options.CheckConsentNeeded = context => true;
        options.MinimumSameSitePolicy = SameSiteMode.None;
      }); */

      //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
      services.AddControllers();
      // Adding my SessionManager singleton for dependency injection:
      services.AddSingleton<ISessionManager, SessionManager>();
      // Adding the singleton responsible for notifications:
      services.AddSingleton<INotificationManager, NotificationManager>();
      // Periodic session clean up service:
      services.AddHostedService<CleanUpHostedService>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }

      app.UseForwardedHeaders(new ForwardedHeadersOptions
      {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
      });

      app.UseStatusCodePages();

      app.UseHttpsRedirection();
      app.UseStaticFiles();
      //app.UseCookiePolicy();

      // Used to be required for .NET 2:
      //app.UseMvc();

      app.UseRouting();
      app.UseEndpoints(opts =>
      {
        opts.MapControllers();
      });
    }
  }
}
