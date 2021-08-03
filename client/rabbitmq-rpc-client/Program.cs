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
            var factory = new ConnectionFactory() { HostName = "localhost" }; //Nesse factory que entra configur do server.
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
                    //Se o pacote estiver aberto esperando a mensagem.
                    if (correlationId == ea.BasicProperties.CorrelationId)
                    {
                        //Prepara a mensagem.
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        Console.WriteLine($"Received {message}");
                        return;
                    }

                    //Caso a mensagem não seja 1-1 com a da queue, ele descarta.
                    Console.WriteLine(
                        $"Menssagem descatada, identificadores de coreção inválidos, "
                            + $"original {correlationId} recebido {ea.BasicProperties.CorrelationId}");
                };

                //Consome algo esperando um ok, provavelmente do canal, exchange, etc.
                channel.BasicConsume(queue: replayQueue, autoAck: true, consumer: consumer);

                //Starta o cabeçalho do pacote.
                var pros = channel.CreateBasicProperties();
                pros.CorrelationId = correlationId;
                pros.ReplyTo = replayQueue;

                //Fica se comunicando em loop, adicionando pedidos na fila, desde que tenha uma ordem/pacote em aberto.
                while (true)
                {
                    Console.WriteLine("Informe o valor do pedido: ");

                    //Estrutura os dados para o pacote que irá na fila.
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
