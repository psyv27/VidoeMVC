using CloudinaryDotNet;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using vidoeMVC.DAL;
using vidoeMVC.Enums;
using vidoeMVC.Models;
using vidoeMVC.Services;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();

        // Ensure roles are created
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            await EnsureRolesCreated(roleManager);
        }

        // Configure the HTTP request pipeline.
        Configure(app);

        await app.RunAsync();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllersWithViews();

        // Configure DbContext
        services.AddDbContext<VidoeDBContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("CodeFirst")));

        // Configure Identity
        services.AddIdentity<AppUser, IdentityRole>(opt =>
        {
            opt.User.RequireUniqueEmail = true;
            opt.Password.RequiredLength = 6;
            opt.Password.RequireNonAlphanumeric = false;
            opt.Password.RequireDigit = false;
            opt.Password.RequireLowercase = false;
            opt.Password.RequireUppercase = false;
        })
        .AddEntityFrameworkStores<VidoeDBContext>()
        .AddDefaultTokenProviders();

        // Add Cloudinary configuration
        var cloudinaryConfig = configuration.GetSection("Cloudinary");
        var cloudinaryAccount = new Account(
            cloudinaryConfig["CloudName"],
            cloudinaryConfig["ApiKey"],
            cloudinaryConfig["ApiSecret"]
        );
        services.AddSingleton(new Cloudinary(cloudinaryAccount));
        services.AddTransient<CloudinaryService>();

        // Configure SMTP settings
        services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));

        // Add EmailService
        services.AddTransient<EmailService>();

        // Add PremiumAccessService
        services.AddTransient<IPremiumAccessService, PremiumAccessService>();
    }

    private static async Task EnsureRolesCreated(RoleManager<IdentityRole> roleManager)
    {
        foreach (UserRole role in Enum.GetValues(typeof(UserRole)))
        {
            if (!await roleManager.RoleExistsAsync(role.ToString()))
            {
                await roleManager.CreateAsync(new IdentityRole(role.ToString()));
            }
        }
    }

    private static void Configure(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "areas",
            pattern: "{area:exists}/{controller=Category}/{action=Index}/{id?}");
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
    }
}
