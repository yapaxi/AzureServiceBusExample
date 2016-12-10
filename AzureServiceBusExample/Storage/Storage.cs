using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureServiceBusExample.Bus.Messages;

namespace AzureServiceBusExample.Storages
{
    public class Storage : IStorage
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, Dictionary<string, object>> _storage = new Dictionary<string, Dictionary<string, object>>();

        public async Task<T[]> GetAll<T>() where T : IMessage
        {
            lock (_lock)
            {
                return Get<T>().Values.Cast<T>().ToArray();
            }
        }

        public async Task<T> Load<T>(string key) where T : IMessage
        {
            lock (_lock)
            {
                return (T)Get<T>()[key];
            }
        }

        public async Task Save<T>(T t) where T : IMessage
        {
            lock (_lock)
            {
                Get<T>()[t.Key] = t;
            }
        }

        private Dictionary<string, object> Get<T>()
        {
            Dictionary<string, object> o;
            if (!_storage.TryGetValue(typeof(T).FullName, out o))
            {
                o = new Dictionary<string, object>();
                _storage[typeof(T).FullName] = o;
            }
            return o;
        }
    }
}
