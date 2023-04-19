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
        [Builder.DefaultValue]
        [Builder.MethodName("WithName")]
        [Builder.IsRequired]
        public string Name { get; set; } = "Default name";

        [Builder.IsRequired]
        private int Age { get; set; }

        [Builder.IgnoreMember]
        private double Height { get; set; }

        public Person Parent { get; set; }

        public List<string> Names { get; set; }
    }
}
