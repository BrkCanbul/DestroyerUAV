using System;

namespace Seagull.City_03.Inspector {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class IgnoreInInspectorAttribute : Attribute {
        
    }
}