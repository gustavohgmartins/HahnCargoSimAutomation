using System.Text;
using System.Threading.Channels;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class Consumer
{
    private readonly IConnection connection;
    private readonly IModel channel;
    private readonly EventingBasicConsumer consumer;

    private bool _isConsuming;
    private string? _consumerTag;

    private readonly string _queueName = "HahnCargoSim_NewOrders";
    private readonly string _HostName = "localhost";

    public Consumer()
    {
        var factory = new ConnectionFactory() 
        { 
            HostName = _HostName
        };

        connection = factory.CreateConnection();
        channel = connection.CreateModel();

        var teste = 0;

        consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($"Received message: {message}");
        };
    }

    public void StartConsuming()
    {
        if (_isConsuming)
        {
            return;
        }

        _consumerTag =  channel.BasicConsume(queue: _queueName, autoAck: true, consumer: consumer);
        Console.WriteLine("Consumer started");
    }

    public void StopConsuming()
    {
        if (!_isConsuming)
        {
            return;
        }

        channel.BasicCancel(_consumerTag);
        Console.WriteLine("Consumer stopped");
    }

}