using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Orders.Service.Data;
using Orders.Service.Messaging;
using Orders.Service.UseCases;
using Orders.Service.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<OrdersDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("OrdersDb")));

builder.Services.AddScoped<PlaceOrderFlow>();
builder.Services.AddScoped<ReadOrdersQuery>();

// настройка Kafka Producer и Consumer
builder.Services.AddSingleton<IProducer<string, string>>(_ =>
{
    var cfg = KafkaBusConfig.BuildProducer(builder.Configuration);
    return new ProducerBuilder<string, string>(cfg).Build();
});

builder.Services.AddSingleton<IConsumer<string, string>>(_ =>
{
    var cfg = KafkaBusConfig.BuildConsumer(builder.Configuration, groupId: "orders.payment-results.v1");
    return new ConsumerBuilder<string, string>(cfg).Build();
});

builder.Services.AddHostedService<OutboxPublisherWorker>();
builder.Services.AddHostedService<PaymentResultConsumerWorker>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Text("orders alive"));
app.MapControllers();

await WarmupDbAsync(app);

app.Run();
return;

static async Task WarmupDbAsync(WebApplication app)
{
    // ждем доступности базы данных с повторными попытками
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

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