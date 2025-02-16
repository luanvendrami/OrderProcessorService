using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessorService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class RabbitMQConsumer : IAsyncDisposable
{
    private readonly IConnection? _connection;
    private readonly IChannel? _channel;

    public RabbitMQConsumer()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                AutomaticRecoveryEnabled = true // Habilita reconexão automática
            };

            Console.WriteLine("🔄 Tentando conectar ao RabbitMQ...");

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            // Declara Exchange e Queue
            _channel.ExchangeDeclareAsync("order_exchange", ExchangeType.Fanout).GetAwaiter().GetResult();
            _channel.QueueDeclareAsync("order_queue", durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
            _channel.QueueBindAsync("order_queue", "order_exchange", "").GetAwaiter().GetResult();

            Console.WriteLine("✅ Conectado ao RabbitMQ com sucesso!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao conectar ao RabbitMQ: {ex.Message}");
            throw;
        }
    }

    public async Task StartListeningAsync(CancellationToken stoppingToken)
    {
        if (_channel is null)
        {
            Console.WriteLine("❌ Canal RabbitMQ não inicializado!");
            return;
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var order = JsonSerializer.Deserialize<Order>(message);

                Console.WriteLine($"📥 Pedido recebido: {order?.Id} - {order?.Description} - {order?.Amount:C}");

                // Simulando um pequeno delay no processamento
                await Task.Delay(2000, stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Erro ao processar mensagem: {ex.Message}");
            }
        };

        await _channel.BasicConsumeAsync(queue: "order_queue", autoAck: true, consumer: consumer);
        Console.WriteLine("🎧 Consumidor RabbitMQ está escutando mensagens...");
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_channel is not null)
            {
                await _channel.DisposeAsync();
                Console.WriteLine("🔌 Canal RabbitMQ fechado.");
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                Console.WriteLine("🔌 Conexão com RabbitMQ fechada.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Erro ao fechar RabbitMQ: {ex.Message}");
        }
    }
}
