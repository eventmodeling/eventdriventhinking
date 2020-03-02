using System;
using System.Collections.Generic;

namespace EventDrivenUi.Example
{
    public class Model
    {
        public Model()
        {
            NestedItems = new List<ClassX>();
        }
        public virtual string StringArg { get; set; }
        public virtual DateTime DateTimeArg { get; set; }
        public virtual Int32 IntArg { get; set; }
        public virtual long LongArg { get; set; }
        public virtual float FloatArg { get; set; }
        public virtual double DoubleArg { get; set; }
        public virtual decimal DecimalArg { get; set; }
        public virtual Guid GuidArg { get; set; }
        public virtual TimeSpan TimeSpanArg { get; set; }

        public virtual Int32? NullableIntArg { get; set; }
        public virtual long? NullableLongArg { get; set; }
        public virtual float? NullableFloat { get; set; }
        public virtual double? NullableDouble { get; set; }
        public virtual Guid? NullableGuid { get; set; }
        public virtual TimeSpan? NullableTimeSpanArg { get; set; }
        public virtual ClassX XArg { get; set; }
        public virtual StructX XStructArg { get; set; }
        public virtual byte[] ArrayArg { get; set; }

        public virtual ICollection<ClassX> NestedItems { get; }
    }
}
