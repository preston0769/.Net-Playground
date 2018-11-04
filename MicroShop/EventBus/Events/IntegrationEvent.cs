using System;

namespace EventBus
{
    public class IntegrationEvent
    {
        public IntegrationEvent()
        {
            ID = Guid.NewGuid();
            CreatedUTCTime = DateTime.UtcNow;

        }

        public Guid ID { get; set; }
        public DateTime CreatedUTCTime { get; set; }
    }
}
