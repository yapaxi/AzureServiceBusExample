using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Bus
{
    public class EnvironmentNamespaceManager
    {
        public EnvironmentNamespaceManager(string rootNamespace)
        {
            RootNamespace = rootNamespace;
        }

        public string RootNamespace { get; }

        public string ResolvePath<T>()
        {
            return ResolvePath(typeof(T));
        }

        public string ResolvePath(Type t)
        {
            return $"{RootNamespace}.{t.FullName}";
        }
    }
}
