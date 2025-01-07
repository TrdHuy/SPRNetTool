using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtWiz.Data
{
    public record StateFlag<T>
        where T : StateFlag<T>
    {
        public int Value { get; }
        public StateFlag(int value)
        {
            Value = value;
        }

        public bool HasFlag(StateFlag<T> other)
        {
            return (this.Value & other.Value) == other.Value;
        }

        public bool HasAllFlagsOf(params StateFlag<T>[] others)
        {
            foreach (var other in others)
            {
                if ((this.Value & other.Value) != other.Value)
                {
                    return false;
                }
            }
            return true;
        }

        public static T operator |(StateFlag<T> a1, StateFlag<T> a2)
        {
            return (T)Activator.CreateInstance(typeof(T), a1.Value | a2.Value)!;
        }
    }
}
