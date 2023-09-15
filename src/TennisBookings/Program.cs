#region Global Usings
global using Microsoft.AspNetCore.Identity;

global using TennisBookings;
global using TennisBookings.Data;
global using TennisBookings.Domain;
global using TennisBookings.Extensions;
global using TennisBookings.Configuration;
global using TennisBookings.Caching;
global using TennisBookings.Shared.Weather;
global using TennisBookings.DependencyInjection;
global using TennisBookings.Services.Bookings;
global using TennisBookings.Services.Greetings;
global using TennisBookings.Services.Unavailability;
global using TennisBookings.Services.Bookings.Rules;
global using TennisBookings.Services.Notifications;
global using TennisBookings.Services.Time;
global using TennisBookings.Services.Staff;
global using TennisBookings.Services.Courts;
global using TennisBookings.Services.Security;
global using Microsoft.EntityFrameworkCore;
#endregion

using Microsoft.Data.Sqlite;
using TennisBookings.BackgroundServices;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Amazon.S3;

var builder = WebApplication.CreateBuilder(args);

// DATABASE - it is using in memory database ( "SqliteConnection": "Filename=:memory:")
// The DB context is defined by TennisBookings.Data.TennisBookingsDbContext and is
// implementing Identity db context, adding security. It initialize DB information
// Down below it defines a background service to initalize users using IHostedService
using var connection = new SqliteConnection(builder.Configuration
	.GetConnectionString("SqliteConnection"));

await connection.OpenAsync();

// SERVICES

builder.Services.AddOptions<HomePageConfiguration>()
	.Bind(builder.Configuration.GetSection("Features:HomePage"))
	.ValidateOnStart();

builder.Services.TryAddEnumerable(
	ServiceDescriptor.Singleton<IValidateOptions<HomePageConfiguration>,
		HomePageConfigurationValidation>());
builder.Services.TryAddEnumerable(
	ServiceDescriptor.Singleton<IValidateOptions<ExternalServicesConfiguration>,
		ExternalServicesConfigurationValidation>());

builder.Services.Configure<GreetingConfiguration>(builder.Configuration.
	GetSection("Features:Greeting"));

builder.Services.AddOptions<ExternalServicesConfiguration>(
	ExternalServicesConfiguration.WeatherApi)
	.Bind(builder.Configuration.GetSection("ExternalServices:WeatherApi"))
	.ValidateOnStart();
builder.Services.AddOptions<ExternalServicesConfiguration>(
	ExternalServicesConfiguration.ProductsApi)
	.Bind(builder.Configuration.GetSection("ExternalServices:ProductsApi"))
.ValidateOnStart();

builder.Services.Configure<ScoreProcesingConfiguration>(
	builder.Configuration.GetSection("ScoreProcessing"));

builder.Services.AddAWSService<IAmazonS3>();    // <<-- use amazon resources

var useLocalStack = builder.Configuration.GetValue<bool>("AWS:UseLocalStack");   // <<-- confguration switch

if (builder.Environment.IsDevelopment() && useLocalStack)    // <<-- for DEV envrionments and swtich ON, use local W3 resources
{
	builder.Services.AddSingleton<IAmazonS3>(sp =>
	{
		var s3Client = new AmazonS3Client(new AmazonS3Config
		{
			ServiceURL = "http://localhost:4566",
			ForcePathStyle = true,
			AuthenticationRegion = builder.Configuration.GetValue<string>("AWS:Region") ?? "eu-west-2"
		});

		return s3Client;
	});
}

// Services extensions can be concatenated
// services extensions must state 'using Microsoft.Extensions.DependencyInjection.Extensions;'
// and defines an STATIC class with an STATIC method that returns a IServiceCollection
// and has as parameter for teh extension 'this IServiceCollection services'
// All services extensions are organized under teh folder 'Dependency injection'
builder.Services
	.AddAppConfiguration(builder.Configuration)
	.AddBookingServices()
	.AddBookingRules()
	.AddCourtUnavailability()
	.AddMembershipServices()
	.AddStaffServices()
	.AddCourtServices()
	.AddWeatherForecasting(builder.Configuration)
	.AddProducts()
	.AddNotifications()
	.AddGreetings()
	.AddCaching()
	.AddTimeServices()
	.AddProfanityValidationService()
	.AddAuditing()
	.AddTennisResultProcessing(builder.Configuration);

// Uses for MVC pattern but also include funcionality for APIs
builder.Services.AddControllersWithViews();

// uses Razor pages
builder.Services.AddRazorPages(options =>
{
	options.Conventions.AuthorizePage("/Bookings");
	options.Conventions.AuthorizePage("/BookCourt");
	options.Conventions.AuthorizePage("/FindAvailableCourts");
	options.Conventions.Add(new PageRouteTransformerConvention(new SlugifyParameterTransformer()));
});

// Add services to the container.
builder.Services.AddDbContext<TennisBookingsDbContext>(options =>
	options.UseSqlite(connection));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<TennisBookingsUser, TennisBookingsRole>(options => options.SignIn.RequireConfirmedAccount = false)
	.AddEntityFrameworkStores<TennisBookingsDbContext>()
	.AddDefaultUI()
	.AddDefaultTokenProviders();

// Background service to initalize users in the database
// AdminEmail = "admin@example.com";
// MemberEmail = "member@example.com";
// both uses as password = 'password'
builder.Services.AddHostedService<InitialiseDatabaseService>();

builder.Services.ConfigureApplicationCookie(options =>
{
	options.AccessDeniedPath = "/identity/account/access-denied";
});

// add a way to display in the console the final settings loaded in memory
// WARNING: This will expose all configuration and there could be sesnitive data 
// that should not be exposed. Secrets are exposed in plan text.
//if (builder.Environment.IsDevelopment())
//{
//	var debugView = builder.Configuration.GetDebugView();
//	Console.WriteLine(debugView);
//}


// MIDDLEWAR COMPONENTS

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseMigrationsEndPoint();
}
else
{
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// MVC Routing, including API, with id as optional, similar to default routing
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

// RAZOR pages routing 
app.MapRazorPages();

// A DB initializer could be used here (e.g. DbInitializer.Seed(app);),
// but in this example the DB initialization occurs in the DBContext class and
// through a background service stated by the AddHostedService

app.Run();
