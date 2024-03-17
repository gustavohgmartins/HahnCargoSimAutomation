using System.Text;
using System.Threading.Channels;
using App.Domain.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

public class Consumer
{
    private IConnection _connection;
    private IModel _channel;
    private EventingBasicConsumer _consumer;

    public List<Order> _availableOrders = new List<Order>();
    public List<Order> _consumedOrders = new List<Order>();

    private bool _isConsuming;
    private string _consumerTag;

    private readonly string _queueName;
    private readonly string _HostName;

    public Consumer(IConfiguration configuration)
    {
        _queueName = configuration.GetSection("RabbitMQ:QueueName").Value;
        _HostName = configuration.GetSection("RabbitMQ:HostName").Value;
        CreateConsumer();
    }

    private void CreateConsumer()
    {
        var factory = new ConnectionFactory()
        {
            HostName = _HostName
        };

        try
        {
            _connection = factory.CreateConnection();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error | Consumer | Unable to create connection | {e.Message}");

            Task.Delay(3000).Wait();

            CreateConsumer();
        }

        _channel = _connection.CreateModel();

        _consumer = new EventingBasicConsumer(_channel);
        _consumer.Received += (model, eventArgs) =>
        {
            try
            {
                var body = eventArgs.Body.ToArray();
                var orderJson = Encoding.UTF8.GetString(body);
                var orderObject = JsonConvert.DeserializeObject<Order>(orderJson);

                _consumedOrders.Add(orderObject);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        };
    }

    public async Task StartConsuming()
    {
        if (_isConsuming)
        {
            return;
        }

        try
        {
            _consumerTag = _channel.BasicConsume(queue: _queueName, autoAck: true, consumer: _consumer);
            _isConsuming = true;
            Console.WriteLine("Consumer started");
        }
        catch (OperationInterruptedException)
        {
            Console.WriteLine($"Error | Consumer | The queue'{_queueName}' does not exist. Unable to start consuming");
            _connection.Dispose();
            Task.Delay(1000).Wait();
            CreateConsumer();
            await StartConsuming();
        }
    }

    public void StopConsuming()
    {
        if (!_isConsuming)
        {
            return;
        }

        _connection.Dispose();
        _isConsuming = false;
        Console.WriteLine("Consumer stopped");
    }

}