using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace com.kafka.Interfaces
{
    public interface IKafkaProducer<in TKey, in TValue> where TValue : class
    {
        Task ProduceAsync(string topic, TKey key, TValue value);
    }
}
