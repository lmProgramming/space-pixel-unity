using System;
using System.Collections.Generic;

namespace LM
{
    public class DefaultDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        private readonly Func<TValue> _defaultValueFactory;

        public DefaultDictionary(Func<TValue> defaultValueFactory)
        {
            _defaultValueFactory = defaultValueFactory;
        }

        public new TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out var value)) return value;
                value = _defaultValueFactory();
                Add(key, value);

                return value;
            }
            set => base[key] = value;
        }
    }
}