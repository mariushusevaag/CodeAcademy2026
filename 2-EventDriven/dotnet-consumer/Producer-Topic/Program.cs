using CodeAcademy.DotnetConsumer.Common.Config;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

Console.WriteLine("Producer starting...");
// Establish connection to RabbitMQ
using var connection = await ConnectionHelper.ConnectAsync();
Console.WriteLine("Connected to RabbitMQ");

const string exchangeName = "demo.topic";
var publishInterval = TimeSpan.FromSeconds(5);

await using var channel = await connection.CreateChannelAsync();

await channel.ExchangeDeclareAsync(
    exchange: exchangeName,
    type: ExchangeType.Topic,
    durable: false,
    autoDelete: false);

Console.WriteLine($"Declared topic exchange '{exchangeName}'. Publishing every {publishInterval.TotalSeconds}s. Ctrl+C to stop.");

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

        // var counterIsEven = counter % 2 == 0;
        // var routingKey = counterIsEven ? "idem.public" : "idem.public.reply"; // must match binding key of consumer

        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: "idem.public.reply.mention", // required for topic
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
