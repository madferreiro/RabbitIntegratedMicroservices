using Microservices.Common.MassTransit.Consumer;

namespace Microservices.Common.MassTransit
{
    public class ReceiveEndpointDefinition
    {
        public string Name { get; set; }

        public ConsumerCollection Consumers { get; set; } = new ConsumerCollection();
    }
}
