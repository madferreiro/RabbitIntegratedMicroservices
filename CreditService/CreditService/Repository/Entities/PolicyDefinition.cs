using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CreditService.Repository.Entities
{
    public class PolicyDefinition
    {
        public PolicyDefinition()
        {
            Id = Guid.NewGuid();
        }

        [Key]
        public Guid Id { get; set; }
        public string Description { get; set; }
    }
}
