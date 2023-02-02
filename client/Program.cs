// See https://aka.ms/new-console-template for more information
using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var rpcClient = new RpcClient();

// Console.WriteLine(" [x] Requesting fib(30)");
var response = rpcClient.Call("{\"inpu\":9}");

Console.WriteLine(" [.] Got = '{0}'", response);
rpcClient.Close();



public class RpcClient
{
    private readonly IConnection connection;
    private readonly IModel channel;
    private readonly string replyQueueName;
    private readonly EventingBasicConsumer consumer;
    private readonly BlockingCollection<string> respQueue = new BlockingCollection<string>();
    private readonly IBasicProperties props;

    public  RpcClient()
    {
        // var factory = new ConnectionFactory() { HostName = "localhost" ,Port = 5672 , UserName = "user",Password = "mypass" };
        var factory = new ConnectionFactory() { HostName = "18.138.164.11", Port = 5672, UserName = "admin", Password = "admin2023" };

        connection = factory.CreateConnection();
        channel = connection.CreateModel();
        replyQueueName = channel.QueueDeclare().QueueName;
        Console.WriteLine($"Reply queue : {replyQueueName}");
        consumer = new EventingBasicConsumer(channel);

        props = channel.CreateBasicProperties();
        var correlationId = Guid.NewGuid().ToString();
        props.CorrelationId = correlationId;
        props.ReplyTo = replyQueueName;
        props.MessageId = "get";


        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var response = Encoding.UTF8.GetString(body);
            if (ea.BasicProperties.CorrelationId == correlationId)
            {
                respQueue.Add(response);
            }
        };

        channel.BasicConsume(
            consumer: consumer,
            queue: replyQueueName,
            autoAck: true);
    }//constructor

    public string Call(string message)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish(
            exchange: "",
            routingKey: "central_queue",
            basicProperties: props,
            body: messageBytes);

        return respQueue.Take();
    }//func

    public void Close()
    {
        connection.Close();
    }//func

}//class