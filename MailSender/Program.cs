using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

class Program
{
    
    public static void Main(string[]? args)
    {
        var address = "localhost";

        if( !(args is null || args.Length == 0) )
        {
            address = args[0];
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Connecting to {address}...");

        var factory = new ConnectionFactory
        {
            HostName = address,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(30)
            
        };
        
        using var connection = factory.CreateConnection(); 
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "MailQue",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        Console.WriteLine(" [*] Waiting for messages.");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($" {DateTime.Now}::: Received {message}");
        };
        channel.BasicConsume(queue: "MailQue",
                             autoAck: true,
                             consumer: consumer);

        Console.WriteLine("Type 'exit' to exit.");
        while(Console.ReadLine() != "exit")
        {

        }

        channel.Close();
        connection.Dispose();
        

    }
}
