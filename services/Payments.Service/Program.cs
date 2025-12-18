using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Payments.Service.Data;
using Payments.Service.Messaging;
using Payments.Service.UseCases;
using Payments.Service.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PaymentsDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("PaymentsDb")));

builder.Services.AddScoped<AccountFacade>();
builder.Services.AddScoped<PaymentOrchestrator>();

builder.Services.AddSingleton<IProducer<string, string>>(_ =>
{
    var cfg = KafkaBusConfig.BuildProducer(builder.Configuration);
    return new ProducerBuilder<string, string>(cfg).Build();
});

builder.Services.AddSingleton<IConsumer<string, string>>(_ =>
{
    var cfg = KafkaBusConfig.BuildConsumer(builder.Configuration, groupId: "payments.payment-requests.v1");
    return new ConsumerBuilder<string, string>(cfg).Build();
});

builder.Services.AddHostedService<PaymentRequestConsumerWorker>();
builder.Services.AddHostedService<OutboxPublisherWorker>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Text("payments alive"));
app.MapControllers();

await WarmupDbAsync(app);

app.Run();
return;

static async Task WarmupDbAsync(WebApplication app)
{
    // ждем пока база станет доступна
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

    var tries = 0;
    while (true)
    {
        tries++;

        try
        {
            await db.Database.EnsureCreatedAsync();
            return;
        }
        catch when (tries < 30)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}