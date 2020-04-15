using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EventDrivenThinking.Utils
{
    public class TypeCollection : IEquatable<TypeCollection>, 
        ICollection<Type>,
        IReadOnlyCollection<Type> 
    {
        private readonly HashSet<Type> _types;
        private bool _isDirty;
        private Guid _hash;
        private bool _isReadonly;
        public Guid Hash
        {
            get
            {
                if (_isDirty)
                {
                    _hash = OnComputeHash();
                    _isDirty = false;
                }

                return _hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TypeCollection) obj);
        }

        public static bool operator ==(TypeCollection left, TypeCollection right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TypeCollection left, TypeCollection right)
        {
            return !Equals(left, right);
        }

        public override int GetHashCode()
        {
            return _hash.GetHashCode();
        }

        public void MakeReadonly()
        {
            _isReadonly = true;
            if (_isDirty)
            {
                _hash = OnComputeHash();
                _isDirty = false;
            }
        }
        public TypeCollection()
        {
            _types = new HashSet<Type>();
        }
        public TypeCollection(IEnumerable<Type> types)
        {
            _types = new HashSet<Type>(types);
        
        }
        public bool Contains<T>()
        {
            return Contains(typeof(T));
        }

        public void Add(Type item)
        {
            if(_isReadonly)
                throw new InvalidOperationException("Collection is readonly.");
            _types.Add(item);
            _isDirty = true;
        }

        public void Clear()
        {
            if (_isReadonly)
                throw new InvalidOperationException("Collection is readonly.");

            _types.Clear();
            _isDirty = true;
        }

        public bool Contains(Type t)
        {
            return _types.Contains(t);
        }

        public void CopyTo(Type[] array, int arrayIndex)
        {
            foreach (var t in this)
            {
                array[arrayIndex++] = t;
            }
        }

        public bool Remove(Type item)
        {
            if (_isReadonly)
                throw new InvalidOperationException("Collection is readonly.");

            if (_types.Remove(item))
            {
                _isDirty = true;
                return true;
            }

            return false;
        }

        public static explicit operator TypeCollection(Type[] data)
        {
            return new TypeCollection(data);
        }
        public static explicit operator TypeCollection(List<Type> data)
        {
            return new TypeCollection(data);
        }
        private Guid OnComputeHash()
        {
            return string.Concat(_types.Select(x => x.FullName).OrderBy(x => x)).ToGuid();
        }

        public IEnumerator<Type> GetEnumerator()
        {
            foreach (var i in _types)
                yield return i;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Equals(TypeCollection other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Hash.Equals(other.Hash);
        }

        public int Count => _types.Count;
        public bool IsReadOnly => _isReadonly;

        //public Type this[int index] => _types[index];
    }
}