using System;
using System.Collections.Generic;
using System.Text;

namespace EasyBuilder.NET.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class BuilderIgnoreMemberAttribute : Attribute
    {
    }
}
