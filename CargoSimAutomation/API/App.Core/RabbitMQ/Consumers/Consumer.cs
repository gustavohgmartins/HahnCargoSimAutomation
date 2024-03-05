using System.Text;
using System.Threading.Channels;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

public class Consumer
{
    private IConnection _connection;
    private IModel _channel;
    private EventingBasicConsumer _consumer;

    private bool _isConsuming;
    private bool _stopRequested;
    private string _consumerTag;

    private readonly string _queueName = "HahnCargoSim_NewOrders";
    private readonly string _HostName = "localhost";

    public Consumer()
    {
        CreateConsumer();
    }

    private void CreateConsumer()
    {
        var factory = new ConnectionFactory()
        {
            HostName = _HostName
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _consumer = new EventingBasicConsumer(_channel);
        _consumer.Received += (model, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($"Received message: {message}");
        };
    }

    public async Task StartConsuming()
    {
        if (_isConsuming || _stopRequested)
        {
            _stopRequested = false;
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
            Console.WriteLine($"The queue'{_queueName}' does not exist. Unable to start consuming");
            _connection.Dispose();
            Task.Delay(3000).Wait();
            CreateConsumer();
            await StartConsuming();
        }
    }

    public void StopConsuming()
    {
        _connection.Dispose();
        _isConsuming = false;
        _stopRequested = true;
        Console.WriteLine("Consumer stopped");
    }

}