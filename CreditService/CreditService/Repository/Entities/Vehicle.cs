using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace CreditService.Repository.Entities
{
    public class Vehicle
    {
        public Vehicle()
        {
            Id = Guid.NewGuid();
        }

        [Key]
        public Guid Id { get; set; }
        public string Description { get; set; }
    }
}
