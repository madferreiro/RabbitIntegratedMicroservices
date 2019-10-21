using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditService.Repository.Entities
{
    public class Claim
    {
        public Claim()
        {
            Id = Guid.NewGuid();
        }

        [Key]
        public Guid Id { get; set; }
        public string Description { get; set; }


        public Guid InsuredId { get; set; }
        public Guid VehicleId { get; set; }


        // Navigation Properties

        [ForeignKey(nameof(InsuredId))]
        public virtual Insured Insured { get; set; }
        [ForeignKey(nameof(VehicleId))]
        public virtual Vehicle Vehicle { get; set; }
        public virtual ICollection<Policy> Policies { get; set; }
    }
}
