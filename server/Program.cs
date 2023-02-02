// In the name of Allah

using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

//var factory = new ConnectionFactory() { HostName = "localhost", Port = 5672, UserName = "user", Password = "mypass" };
var factory = new ConnectionFactory() { HostName = "18.138.164.11", Port = 5672, UserName = "admin", Password = "admin2023" };
var connection = factory.CreateConnection();
var channel = connection.CreateModel();
    channel.QueueDeclare(
        queue: "restaurant_queue",
        durable: false,
        exclusive: false,
        autoDelete: false,
        arguments: null
        );

channel.BasicQos(
    prefetchSize: 0,
    prefetchCount: 1,
    global: false
    );

var consumer = new EventingBasicConsumer(channel);

channel.BasicConsume(
    queue: "restaurant_queue",
    autoAck: false,
    consumer: consumer
    );

Console.WriteLine(" [x] Awaiting RPC requests");

consumer.Received += (model, ea) =>
{

    //response
    string response = string.Empty;

    var body = ea.Body.ToArray();
    var props = ea.BasicProperties;
    var route = props.MessageId;

    //reply
    var replyProps = channel.CreateBasicProperties();
    replyProps.CorrelationId = props.CorrelationId;

    try
    {
        var request_body = Encoding.UTF8.GetString(body);
        Parameter? parameter = JsonSerializer.Deserialize<Parameter>(request_body);
        if (parameter is null) response = "Invalid request";
        int input = parameter!.input;

        Console.WriteLine($"route : {route}");
        Console.WriteLine(" [.] fib({0})", request_body);

        if (route == "fib")
        {
            response = fib(input).ToString();
        }//if
        else
        {
            response = "Not found route!";
        }//else

    }//try
    catch (Exception e)
    {
        Console.WriteLine(" [.] " + e.Message);
        response = e.Message;
    }//catch
    finally
    {
        var responseBytes = Encoding.UTF8.GetBytes(response);

        channel.BasicPublish(
            exchange: "",
            routingKey: props.ReplyTo,
            basicProperties: replyProps,
            body: responseBytes
            );
        channel.BasicAck(
            deliveryTag: ea.DeliveryTag,
            multiple: false
          );
    }//finally
};

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();

int fib(int n)
{
    if (n == 0 || n == 1)
    {
        return n;
    }

    return fib(n - 1) + fib(n - 2);
}//func


class Parameter
{
    public int input { get; set; }
}//class