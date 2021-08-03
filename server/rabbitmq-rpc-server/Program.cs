using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using rabbitmq_rpc_server.Domain;
using rabbitmq_rpc_server.Services;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.Json;

namespace rabbitmq_rpc_server
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" }; //Detalhes da conexão.
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                //Inicializa a escuta na porta RPC, aguardando um pacote com a mensagem dentro.
                var consumer = InitializerConsumer(channel, nameof(Order));

                //Adiciona ao evento que executa quando disparado.
                consumer.Received += (model, ea) =>
                {
                    try
                    {
                        //Abaixo serializa os dados da mensagem.
                        var incommingMessage = Encoding.UTF8.GetString(ea.Body.ToArray());
                        Console.WriteLine($"{DateTime.Now:o} Incomming => {incommingMessage}");

                        var order = JsonSerializer.Deserialize<Order>(incommingMessage);
                        order.SetStatus(ProcessOrderStatus(order.Amount));

                        var replyMessage = JsonSerializer.Serialize(order);
                        Console.WriteLine($"{DateTime.Now:o} Reply => {replyMessage}");

                        //Envia o pacote solicitado.
                        SendReplyMessage(replyMessage, channel, ea);
                    }catch
                    {
                        // efetuar log e tratar fluxo
                        // você também pode retornar uma mensagem com status erro com a mensagem
                    }
                };

                Console.ReadLine();
            }
        }

        private static OrderStatus ProcessOrderStatus(decimal amount)
        {
            return OrderService.OnStore(amount);
        }

        private static void SendReplyMessage(string replyMessage, IModel channel, BasicDeliverEventArgs ea)
        {
            var props = ea.BasicProperties;
            var replyProps = channel.CreateBasicProperties();
            replyProps.CorrelationId = props.CorrelationId;

            var responseBytes = Encoding.UTF8.GetBytes(replyMessage);

            channel.BasicPublish(exchange: "", routingKey: props.ReplyTo,
                basicProperties: replyProps, body: responseBytes);

            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        }

        

        private static EventingBasicConsumer InitializerConsumer(IModel channel, string queueName)
        {
            //Abre a fila
            channel.QueueDeclare(queue: queueName, durable: false,
                exclusive: false, autoDelete: false, arguments: null);

            //Inicio, tamanho, posiçoes na fila
            channel.BasicQos(0, 1, false);

            //Cria o objeto do tipo EventingBasicConsumer, que será enviado ao listener, provavelmente.
            var consumer = new EventingBasicConsumer(channel);
            channel.BasicConsume(queue: queueName,
                autoAck: false, consumer: consumer);

            return consumer;
        }
    }
}
