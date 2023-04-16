using EasyBuilder.NET.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBuilders.Tests
{
    [Builder]
    public partial class Person
    {
        public string name;

        private int age;

        [BuilderIgnoreMember]
        private double height;

        public string A { get; set; }
    }
}
