using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CreditService.Repository.Entities
{
    public class Insurance
    {
        public Insurance()
        {
            Id = Guid.NewGuid();
        }

        [Key]
        public Guid Id { get; set; }
        public string Description { get; set; }
    }
}
