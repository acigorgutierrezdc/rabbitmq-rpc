using System;
using System.Collections.Generic;
using System.Text;

namespace rabbitmq_rpc_client.Domain
{
    //Classe só para os status da mensagem.
    public enum OrderStatus
    {
        Processing = 0, Aproved, Declined
    }
}
