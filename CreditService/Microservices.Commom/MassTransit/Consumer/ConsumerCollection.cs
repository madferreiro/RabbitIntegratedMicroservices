using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MassTransit;
using Microservices.Common.Extensions.Internal;

namespace Microservices.Common.MassTransit.Consumer
{
    public class ConsumerCollection
    {
        private IDictionary<string, HashSet<Type>> AssemblyConsumers { get; } = new Dictionary<string, HashSet<Type>>();

        private HashSet<Type> Consumers { get; } = new HashSet<Type>();

        private HashSet<Type> IgnoreConsumers { get; } = new HashSet<Type>();

        public ConsumerCollection AddConsumer<TConsumer>() where TConsumer : IConsumer
        {
            Consumers.Add(typeof(TConsumer));

            return this;
        }

        public ConsumerCollection AddConsumersInheritingFrom<TConsumer>(Assembly[] assembliesToScan) where TConsumer : IConsumer
        {
            if (!assembliesToScan.Any())
                throw new ArgumentException("At least one assembly must be provided!");

            var consumerBaseType = typeof(TConsumer);
            EnsureAssembliesScanned(assembliesToScan);

            foreach (var assembly in assembliesToScan)
            {
                var assemblyConsumers = AssemblyConsumers[assembly.FullName];
                foreach (var eligibleConsumer in assemblyConsumers.Where(t => consumerBaseType.IsAssignableFrom(t)))
                {
                    Consumers.Add(eligibleConsumer);
                }
            }

            return this;
        }

        /// <summary>
        /// Given an array of events, this method creates new consumer types from a generic base type
        /// by replacing the generic parameter with the given event types.
        /// </summary>
        public ConsumerCollection AddGenericConsumersFromEvents(Type genericConsumerType, params Type[] eventTypes)
        {
            // Verify that the genericConsumerType is indeed a generic type. Moreover,
            // it should have only one generic parameter, and be assignable to IConsumer
            if (!typeof(IConsumer).IsAssignableFrom(genericConsumerType))
            {
                throw new ArgumentException($"Consumer type must implement {nameof(IConsumer)}");
            }

            if (!genericConsumerType.IsGenericType || genericConsumerType.GetGenericArguments().Length != 1)
            {
                throw new ArgumentException(
                    "Consumer type should be generic and accept only a single generic argument.");
            }

            foreach (var eventType in eventTypes)
            {
                var consumerType = genericConsumerType.MakeGenericType(eventType);
                Consumers.Add(consumerType);
            }

            return this;
        }

        public ConsumerCollection AddGenericConsumersFromBaseEvent(Type genericConsumerType, Type eventBaseType, IEnumerable<Assembly> assembliesToScan)
        {
            if (!assembliesToScan.Any())
                throw new ArgumentException("At least one assembly must be provided!");

            var eventTypes = new List<Type>();

            foreach (var assembly in assembliesToScan)
            {
                var assemblyTypes = assembly.GetLoadableTypes();
                eventTypes.AddRange(assemblyTypes.Where(t => eventBaseType.IsAssignableFrom(t) && t != eventBaseType));
            }

            AddGenericConsumersFromEvents(genericConsumerType, eventTypes.ToArray());
            return this;
        }

        public ConsumerCollection AddGenericConsumersFromBaseEvent(Type genericConsumerType, Type eventBaseType)
        {
            // Use the event base type's assembly
            var assemblies = new[] { Assembly.GetAssembly(eventBaseType) };
            return AddGenericConsumersFromBaseEvent(genericConsumerType, eventBaseType, assemblies);

        }

        public ConsumerCollection IgnoreConsumer<TConsumer>() where TConsumer : IConsumer
        {
            IgnoreConsumers.Add(typeof(TConsumer));

            return this;
        }

        public IEnumerable<Type> GetCollectedConsumers()
        {
            return Consumers.Where(c => !IgnoreConsumers.Contains(c));
        }

        private void EnsureAssembliesScanned(IEnumerable<Assembly> assembliesToScan)
        {
            foreach (var assemblyToScan in assembliesToScan)
            {
                if (!AssemblyConsumers.ContainsKey(assemblyToScan.FullName))
                {
                    AssemblyConsumers[assemblyToScan.FullName] = new HashSet<Type>(GetAssemblyConsumers(assemblyToScan));
                }
            }
        }

        private IEnumerable<Type> GetAssemblyConsumers(Assembly assembly)
        {
            var assemblyTypes = assembly.GetLoadableTypes();
            var consumerBaseType = typeof(IConsumer);

            return assemblyTypes.Where(t => consumerBaseType.IsAssignableFrom(t)
                                            && !t.IsInterface
                                            && !t.IsEnum && !t.IsAbstract);
        }
    }
}
