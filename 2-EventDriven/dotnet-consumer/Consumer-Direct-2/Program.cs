using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CodeAcademy.DotnetConsumer.Common.Config;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

Console.WriteLine("Starting Consumer application...");

// Establish connection to RabbitMQ
using var connection = await ConnectionHelper.ConnectAsync();
Console.WriteLine("Connected to RabbitMQ");

// Implement a basic consumer here.
// Start with:
// - Create a channel
// - Declare a queue
// - Create a consumer and subscribe to the queue
// - Handle incoming messages by deserializing the JSON and printing the content to the console

const string exchangeName = "demo.direct";
const string queueName = "demo.consumer.queue";
const string routingKey = "demo.direct.routing"; // ignored by direct, required for fanout

await using var channel = await connection.CreateChannelAsync();
await channel.ExchangeDeclareAsync(
    exchange: exchangeName,
    type: ExchangeType.Direct,
    durable: false,
    autoDelete: false);
await channel.QueueDeclareAsync(
    queue: queueName,
    durable: false,
    exclusive: false,
    autoDelete: false);
await channel.QueueBindAsync(
    queue: queueName,
    exchange: exchangeName,
    routingKey: routingKey);

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (sender, e) =>
{
    var body = e.Body.ToArray();
    var messageJson = Encoding.UTF8.GetString(body);
    try
    {
        var message = JsonSerializer.Deserialize<JsonNode>(messageJson);
        Console.WriteLine($"Received message: {message}");
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"Failed to deserialize message: {ex.Message}");
    }
    await Task.Yield(); // simulate async work
};
await channel.BasicConsumeAsync(
    queue: queueName,
    autoAck: true,
    consumer: consumer);

Console.WriteLine($"Waiting for messages on queue '{queueName}'. Press Ctrl+C to exit.");
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};
try
{    await Task.Delay(Timeout.Infinite, cts.Token);
}catch (OperationCanceledException)
{
    // graceful shutdown
}