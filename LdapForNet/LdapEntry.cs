﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace LdapForNet
{
    public class LdapEntry
    {
        public string Dn { get; set; }
        public Dictionary<string,List<string>> Attributes { get; set; }
    }

    public class DirectoryEntry
    {
        public string Dn { get; set; }
        public SearchResultAttributeCollection Attributes { get; set; }

        public LdapEntry ToLdapEntry()
        {
            return new LdapEntry
            {
                Dn = Dn,
                Attributes = Attributes.ToDictionary(_=>_.Name,_=>_.GetValues<string>().ToList())
            };
        }
    }

    public class LdapModifyEntry
    {
        public string Dn { get; set; }
        public List<LdapModifyAttribute> Attributes { get; set; }
    }

    public class LdapModifyAttribute
    {
        public string Type { get; set; }
        public List<string> Values { get; set; }
        public Native.Native.LdapModOperation LdapModOperation { get; set; } = Native.Native.LdapModOperation.LDAP_MOD_REPLACE;
    }

    public class DirectoryAttribute
    {
        private readonly List<object> _values = new List<object>();

        public string Name { get; set; }

        public IEnumerable<T> GetValues<T>() where T : class, IEnumerable
        {
            if (!_values.Any())
            {
                return Enumerable.Empty<T>();
            }

            var type = typeof(T);
            var valuesType = _values.First().GetType();
            if (type == valuesType)
            {
                return _values.Select(_ => _ as T);
            }

            if (type == typeof(string))
            {
                return _values.Select(_ => Utils.Encoder.Instance.GetString((byte[]) _))
                    .Select(_ => _ as T);
            }

            if (type == typeof(byte[]))
            {
                return _values.Select(_ => Utils.Encoder.Instance.GetBytes((string) _))
                    .Select(_ => _ as T);
            }

            throw new NotSupportedException($"Not supported type. You could specify 'string' or 'byte[]' of generic methods. Your type is {type.Name}");
        }

        internal List<object> GetRawValues() => _values;

        internal void Add<T>(T value) where T : class, IEnumerable
        {
            ThrowIfWrongType<T>();
            _values.Add(value);
        }

        internal void AddValues<T>(IEnumerable<T> values) where T : class, IEnumerable
        {
            ThrowIfWrongType<T>();
            _values.AddRange(values);
        }

        private void ThrowIfWrongType<T>() where T : class, IEnumerable
        {
            var type = typeof(T);
            if (type != typeof(string) && type != typeof(byte[]))
            {
                throw new NotSupportedException(
                    $"Not supported type. You could specify 'string' or 'byte[]' of generic methods. Your type is {type.Name}");
            }

            if (_values.Any() && _values.First().GetType() != type)
            {
                throw new NotSupportedException($"Not supported type. Type of values is {_values.First().GetType()}");
            }
        }
    }

    public class SearchResultAttributeCollection : KeyedCollection<string,DirectoryAttribute>
    {
        internal SearchResultAttributeCollection() { }

        public ICollection<string> AttributeNames => Dictionary.Keys;
        
        protected override string GetKeyForItem(DirectoryAttribute item)
        {
            return item.Name;
        }
    }
}