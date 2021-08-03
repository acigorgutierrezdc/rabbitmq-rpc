using rabbitmq_rpc_server.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace rabbitmq_rpc_server.Services
{
    public sealed class OrderService
    {
        //Se true, armazena na fila(objeto) orderstatus
        public static OrderStatus OnStore(decimal amount)
        {
            return (amount < 0 || amount > 1000) ? OrderStatus.Declined : OrderStatus.Aproved;
        }
    }
}
