using System.Collections.Generic;
using TestLibrary;

namespace SuperTestLibrary
{
    public class Car
    {
        public Motor Engine { get; set; }
        public Manufactor Manufactor { get; set; }
        public ICollection<Merchant> Merchants { get; set; }
        public int NumberOfWheels { get; set; }
        public Customer Owner { get; set; }
    }

    public class Motor
    {
        public decimal TraveledUnits { get; set; }
        public float HorsePower { get; set; }
    }

    public class Manufactor : Company
    {
        public List<Car> Cars { get; set; }
    }
    
    public class Merchant
    {
        public Car[] Cars { get; set; }
        public ICollection<Customer> Customers { get; set; }
    }

    public class Company
    {
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public Country Country { get; set; }
    }

    public class Country
    {
        public string Name { get; set; }
        public string CountryCodes { get; set; }
    }

    public class Customer : Person
    {
        public ICollection<decimal> Transactions { get; set; }
    }
}
