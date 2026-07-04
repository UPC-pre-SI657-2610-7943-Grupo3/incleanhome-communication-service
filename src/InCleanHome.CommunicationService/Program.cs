using InCleanHome.CommunicationService.Configuration;
using InCleanHome.CommunicationService.Discovery;
using InCleanHome.CommunicationService.Infrastructure.ExternalServices.IamService;
using InCleanHome.CommunicationService.Infrastructure.ExternalServices.ProfileService;
using InCleanHome.CommunicationService.Infrastructure.Messaging.Consumers;
using InCleanHome.CommunicationService.Infrastructure.Persistence;
using InCleanHome.CommunicationService.Infrastructure.Pipeline;
using InCleanHome.CommunicationService.Messaging.Application.CommandServices;
using InCleanHome.CommunicationService.Messaging.Application.QueryServices;
using InCleanHome.CommunicationService.Messaging.Domain.Repositories;
using InCleanHome.CommunicationService.Messaging.Domain.Services;
using InCleanHome.CommunicationService.Messaging.Domain.Services.External;
using InCleanHome.CommunicationService.Messaging.Infrastructure.Repositories;
using InCleanHome.CommunicationService.Messaging.Infrastructure.Twilio;
using InCleanHome.CommunicationService.Notifications.Application.CommandServices;
using InCleanHome.CommunicationService.Notifications.Application.QueryServices;
using InCleanHome.CommunicationService.Notifications.Domain.Repositories;
using InCleanHome.CommunicationService.Notifications.Domain.Services;
using InCleanHome.CommunicationService.Notifications.Domain.Services.External;
using InCleanHome.CommunicationService.Notifications.Infrastructure.Firebase;
using InCleanHome.CommunicationService.Notifications.Infrastructure.Repositories;
using InCleanHome.CommunicationService.Shared;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().MinimumLevel.Information().CreateLogger();

try
{
    Log.Information("Starting InCleanHome Communication Service");
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    var consulAddress = Environment.GetEnvironmentVariable("CONSUL_HTTP_ADDR") ?? "http://consul:8500";
    var serviceName   = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "communication-service";
    var serviceHost   = Environment.GetEnvironmentVariable("SERVICE_HOST") ?? serviceName;
    var servicePort   = int.TryParse(Environment.GetEnvironmentVariable("SERVICE_PORT"), out var p) ? p : 5005;

    var dbConnection = Environment.GetEnvironmentVariable("COMMUNICATION_DB_CONNECTION")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("COMMUNICATION_DB_CONNECTION env var is required.");

    var rabbitMqUrl = Environment.GetEnvironmentVariable("RABBITMQ_URL") ?? string.Empty;
    var rabbitMqEnabled = !string.IsNullOrWhiteSpace(rabbitMqUrl)
                         && !rabbitMqUrl.Contains("placeholder", StringComparison.OrdinalIgnoreCase);

    var loadedFromConsul = await ConsulConfigurationLoader.LoadFromConsulAsync(
        builder.Configuration, consulAddress, serviceName);
    

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddSwaggerGen(opts =>
    {
        opts.SwaggerDoc("v1", new OpenApiInfo
        {
            Title       = "InCleanHome Communication Service", Version = "v1",
            Description = "Messaging (Twilio) + Notifications (FCM + in-app)"
        });
        opts.EnableAnnotations();
        opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header. Example: 'Bearer eyJhbGciOi...'",
            Name = "Authorization", In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey, Scheme = "Bearer"
        });
        opts.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
        });
    });

    builder.Services.AddDbContext<CommunicationDbContext>(opts => opts.UseNpgsql(dbConnection));

    // Shared
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // Notifications BC
    builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
    builder.Services.AddScoped<IUserDeviceRepository, UserDeviceRepository>();
    builder.Services.AddScoped<INotificationCommandService, NotificationCommandService>();
    builder.Services.AddScoped<INotificationQueryService, NotificationQueryService>();
    builder.Services.AddSingleton<IPushNotificationProvider, FirebaseCloudMessagingAdapter>();

    // Messaging BC
    builder.Services.AddScoped<IMessageRepository, MessageRepository>();
    builder.Services.AddScoped<IMessageCommandService, MessageCommandService>();
    builder.Services.AddScoped<IMessageQueryService, MessageQueryService>();
    builder.Services.AddSingleton<IRealtimeMessagingProvider, TwilioRealtimeMessagingAdapter>();

    // External HTTP clients
    builder.Services.AddHttpClient<IIamServiceClient, IamServiceClient>(c => c.Timeout = TimeSpan.FromSeconds(15));
    builder.Services.AddHttpClient<IProfileServiceClient, ProfileServiceClient>(c => c.Timeout = TimeSpan.FromSeconds(15));

    // MassTransit + RabbitMQ. Communication is the GREAT CONSUMER.
    builder.Services.AddMassTransit(x =>
    {
        // Projection consumer (special)
        x.AddConsumer<UserDeviceTokenUpdatedConsumer>();

        // IAM events
        x.AddConsumer<UserRegisteredConsumer>();
        x.AddConsumer<WorkerDocumentsApprovedConsumer>();
        x.AddConsumer<WorkerDocumentsRejectedConsumer>();
        x.AddConsumer<UserSuspendedConsumer>();
        x.AddConsumer<UserSuspensionClearedConsumer>();

        // Booking events
        x.AddConsumer<BookingCreatedConsumer>();
        x.AddConsumer<BookingConfirmedConsumer>();
        x.AddConsumer<BookingRescheduledConsumer>();
        x.AddConsumer<BookingRejectedConsumer>();
        x.AddConsumer<BookingCancelledConsumer>();
        x.AddConsumer<BookingCompletedConsumer>();

        // Payment events
        x.AddConsumer<PaymentProcessedConsumer>();
        x.AddConsumer<PaymentFailedConsumer>();

        // Reviews events
        x.AddConsumer<ReviewSubmittedConsumer>();
        x.AddConsumer<ReportSubmittedConsumer>();
        x.AddConsumer<ReportConfirmedConsumer>();
        x.AddConsumer<SuspensionAppealSubmittedConsumer>();
        x.AddConsumer<SuspensionAppealAcceptedConsumer>();
        x.AddConsumer<SuspensionAppealRejectedConsumer>();

        if (rabbitMqEnabled)
            x.UsingRabbitMq((context, cfg) => { cfg.Host(new Uri(rabbitMqUrl)); cfg.ConfigureEndpoints(context); });
        else
            x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
    });

    var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:8080" };
    builder.Services.AddCors(opts => opts.AddDefaultPolicy(
        p => p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

    var registrationOptions = new ConsulRegistrationOptions
    {
        ConsulAddress = consulAddress, ServiceName = serviceName,
        ServiceId = $"{serviceName}-{Environment.MachineName}",
        Host = serviceHost, Port = servicePort,
        Tags = new[] { "communication", "dotnet" },
        HealthCheckUrl = $"http://{serviceHost}:{servicePort}/health"
    };
    builder.Services.AddSingleton(Options.Create(registrationOptions));
    builder.Services.AddHttpClient<ConsulServiceRegistration>(c => c.Timeout = TimeSpan.FromSeconds(10));
    builder.Services.AddHostedService<ConsulRegistrationHostedService>();

    builder.Services.AddHealthChecks().AddDbContextCheck<CommunicationDbContext>("communication-db");

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<CommunicationDbContext>();
        await db.Database.EnsureCreatedAsync();
        Log.Information("Database schema ensured.");
    }

    app.UseSerilogRequestLogging();
    app.UseCors();
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = check => check.Name != "masstransit-bus"
    });
    app.MapGet("/", () => Results.Ok(new
    {
        service = serviceName, status = "running",
        configSource = loadedFromConsul ? "consul" : "appsettings.json",
        broker = rabbitMqEnabled ? "configured" : "disabled"
    }));
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Communication Service v1"); c.RoutePrefix = "swagger"; });
    app.UseJwtAuth();
    app.MapControllers();

    Log.Information("InCleanHome Communication Service ready on port {Port}", servicePort);
    await app.RunAsync();
}
catch (Exception ex) { Log.Fatal(ex, "Communication Service terminated unexpectedly"); throw; }
finally { Log.CloseAndFlush(); }
