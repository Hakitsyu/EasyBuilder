using System;
using System.Collections.Generic;
using System.Text;

namespace EasyBuilder.NET.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class Builder : Attribute
    {
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
        public class DefaultValue : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
        public class IgnoreMember : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
        public class MethodName : Attribute
        {
            public MethodName(string name)
                => Name = name;

            public string Name { get; set; }
        }

        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
        public class IsRequired : Attribute
        {
        }
    }
}
