using CreditService.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CreditService.Repository
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> optionContext)
            : base(optionContext)
        {
        }

        #region Insurance
        public DbSet<Claim> Claims { get; set; }
        public DbSet<Policy> Policy { get; set; }
        public DbSet<Insurance> Insurance { get; set; }
        public DbSet<Insured> Insured { get; set; }
        public DbSet<Vehicle> Vehicle { get; set; }
        #endregion


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            DescribeCompositeKeys(modelBuilder);
            DescribeTableRelations(modelBuilder);
            DescribeIndexes(modelBuilder);

            base.OnModelCreating(modelBuilder);

            Seeder.Seed(modelBuilder);
        }

        private void DescribeCompositeKeys(ModelBuilder modelBuilder)
        {
        }

        private void DescribeTableRelations(ModelBuilder modelBuilder)
        {

        }

        /// <summary>
        /// Describe the indexes here. 
        /// </summary>
        /// <param name="modelBuilder"></param>
        private void DescribeIndexes(ModelBuilder modelBuilder)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
    }
}
