using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;

namespace rabbitmq_rpc_client
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection()) //Conecta
            using (var channel = connection.CreateModel()) //Alimenta as propriedades do cliente queque
            {
                var replayQueue = $"{nameof(Order)}_return";
                var correlationId = Guid.NewGuid().ToString();

                channel.QueueDeclare(queue: replayQueue, durable: false,
                    exclusive: false, autoDelete: false, arguments: null); //Prioridade da mensagem na fila
                channel.QueueDeclare(queue: nameof(Order), durable: false,
                    exclusive: false, autoDelete: false, arguments: null); //O Objeto dentro da fila

                var consumer = new EventingBasicConsumer(channel); //Abre um listener

                //Adição de codigo pra trabalhar a mensagem no listener
                consumer.Received += (model, ea) =>
                {
                    if (correlationId == ea.BasicProperties.CorrelationId)
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        Console.WriteLine($"Received {message}");
                        return;
                    }


                    Console.WriteLine(
                        $"Menssagem descatada, identificadores de coreção inválidos, "
                            + $"original {correlationId} recebido {ea.BasicProperties.CorrelationId}");
                };

                channel.BasicConsume(queue: replayQueue, autoAck: true, consumer: consumer);

                var pros = channel.CreateBasicProperties();

                pros.CorrelationId = correlationId;
                pros.ReplyTo = replayQueue;

                while (true)
                {
                    Console.WriteLine("Informe o valor do pedido: ");

                    var amount = decimal.Parse(Console.ReadLine());

                    var order = new Order(amount);
                    var message = JsonSerializer.Serialize(order);
                    var body = Encoding.UTF8.GetBytes(message);

                    channel.BasicPublish(exchange: "",
                        routingKey: nameof(Order), basicProperties: pros, body: body); //Adiciona mensagem na fila do servidor.

                    Console.WriteLine($"Published: {message}\n\n");
                    Console.ReadKey();
                    Console.Clear();
                }
            }
        }
    }
}
