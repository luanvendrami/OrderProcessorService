
var builder = WebApplication.CreateBuilder(args);

var rabbitMQConsumer = new RabbitMQConsumer();
builder.Services.AddSingleton(rabbitMQConsumer);

var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(async () =>
{
    using var scope = app.Services.CreateScope();
    var consumer = scope.ServiceProvider.GetRequiredService<RabbitMQConsumer>();
    await consumer.StartListeningAsync(app.Lifetime.ApplicationStopping);
});

app.Lifetime.ApplicationStopping.Register(async () => await rabbitMQConsumer.DisposeAsync());

app.MapGet("/", () => "Order Processor Service Running...");

app.Run();
