using System;
using System.Collections;
using System.Collections.Generic;

namespace Inedo.BuildMasterExtensions.LeanKit
{
    internal sealed class JavaScriptArray : IEnumerable
    {
        private ITypedList list;

        public JavaScriptArray()
        {
        }

        public object this[int index]
        {
            get { return this.list[index]; }
            set { this.list[index] = value; }
        }

        public int Length
        {
            get { return this.list == null ? 0 : this.list.Count; }
        }

        public void Add(object value)
        {
            if (value != null)
            {
                if (this.list == null)
                    this.list = (ITypedList)Activator.CreateInstance(typeof(TypedList<>).MakeGenericType(value.GetType()));
                else if (!this.list.ItemType.IsAssignableFrom(value.GetType()))
                {
                    var newList = new TypedList<object>();

                    foreach (var item in this.list)
                        newList.Add(item);

                    this.list = newList;
                }
            }
            else
            {
                if (this.list == null)
                    this.list = new TypedList<object>();
                else
                {
                    if (this.list.ItemType.IsValueType && Nullable.GetUnderlyingType(this.list.ItemType) != null)
                    {
                        var nullableList = (ITypedList)Activator.CreateInstance(typeof(TypedList<>).MakeGenericType(typeof(Nullable<>).MakeGenericType(this.list.ItemType)));

                        foreach (var item in this.list)
                            nullableList.Add(item);

                        this.list = nullableList;
                    }
                }
            }

            this.list.Add(value);
        }

        public override string ToString()
        {
            if (this.list == null)
                return "[]";
            else
                return string.Format("{0}[{1}]", this.list.ItemType.Name, this.list.Count);
        }

        private interface ITypedList : IEnumerable
        {
            object this[int index] { get; set; }

            int Count { get; }
            Type ItemType { get; }

            void Add(object value);
        }

        private sealed class TypedList<T> : List<T>, ITypedList
        {
            public TypedList()
            {
            }

            object ITypedList.this[int index]
            {
                get { return this[index]; }
                set { this[index] = (T)value; }
            }

            int ITypedList.Count
            {
                get { return this.Count; }
            }
            Type ITypedList.ItemType
            {
                get { return typeof(T); }
            }

            void ITypedList.Add(object value)
            {
                this.Add((T)value);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        public IEnumerator GetEnumerator()
        {
            if (this.list == null)
                return new object[0].GetEnumerator();
            else
                return this.list.GetEnumerator();
        }
    }
}
