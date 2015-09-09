using System;
using System.Collections.Generic;

namespace TestLibrary
{
    public class Address
    {
        public string Street { get; set; }
        public Int16 ZipCode { get; set; }
        public string City { get; set; }

    }

    public class Person
    {
        public string Name { get; set; }
        public string Age { get; set; }

        public string Address { get; set; } 
    }

    public class Parent
    {
        public Person Person { get; set; }
        public Dictionary<long, Child> Children { get; set; }
    }

    public class Child
    {
        public Person Person { get; set; }
    }

    public class Nursery
    {
        public string Name { get; set; }
        public NurseryType Type { get; set; }
    }

    public enum NurseryType
    {
        Kindergarten,
        ElementarySchool,
        HighSchool
    }

    public class BaseClass
    {
        public int BaseProp1 { get; set; }
        public byte[] BaseProp2 { get; set; }
        public decimal BaseProp3 { get; set; }
    }

    public class SubClass : BaseClass
    {
        public void Test()
        {
            base.BaseProp1 -= base.BaseProp1;
        }
    }

    public class SubSubClass : SubClass
    {
        public void Test()
        {
            base.BaseProp1 = base.BaseProp1 << 5;
        }
    }

    public class SuperHero : Person
    {
        public long PowerLevel { get; set; }
        public void Test() { }
    }
}
