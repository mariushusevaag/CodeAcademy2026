using CodeAcademy.DotnetConsumer.Common.Config;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

Console.WriteLine("Producer starting...");
// Establish connection to RabbitMQ
using var connection = await ConnectionHelper.ConnectAsync();
Console.WriteLine("Connected to RabbitMQ");

const string exchangeName = "demo.direct";
var publishInterval = TimeSpan.FromSeconds(2);

await using var channel = await connection.CreateChannelAsync();

await channel.ExchangeDeclareAsync(
    exchange: exchangeName,
    type: ExchangeType.Direct,
    durable: false,
    autoDelete: false);

Console.WriteLine($"Declared direct exchange '{exchangeName}'. Publishing every {publishInterval.TotalSeconds}s. Ctrl+C to stop.");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var counter = 0;
try
{
    while (!cts.IsCancellationRequested)
    {
        counter++;
        var message = new
        {
            id = counter,
            timestamp = DateTimeOffset.UtcNow,
            text = $"Hello from producer #{counter}"
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: string.Empty, // ignored by direct
            body: body,
            cancellationToken: cts.Token);

        Console.WriteLine($"Published #{counter}");

        await Task.Delay(publishInterval, cts.Token);
    }
}
catch (OperationCanceledException)
{
    // graceful shutdown
}

Console.WriteLine("Producer stopped.");
