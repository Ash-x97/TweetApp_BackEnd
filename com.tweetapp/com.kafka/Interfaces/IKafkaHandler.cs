using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace com.kafka.Interfaces
{
    public interface IKafkaHandler<Tk, Tv>
    {
        Task HandleAsync(Tk key, Tv value);
    }
}
