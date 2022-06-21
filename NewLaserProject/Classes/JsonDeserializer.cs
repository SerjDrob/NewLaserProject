﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace NewLaserProject.Classes
{
    internal class JsonDeserializer<TObject>
    {
        private List<Type> _knownTypes;
        public JsonDeserializer()
        {
            _knownTypes = new();
            _knownTypes.Add(typeof(TObject));
        }
        public JsonDeserializer<TObject> SetKnownType<TKnown>()
        {
            _knownTypes.Add(typeof(TKnown));
            return this;
        }
        public TObject Deserialize(string jsonTree)
        {
            TObject result;
            try
            {
                result = JsonConvert.DeserializeObject<TObject>(jsonTree, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects,
                    SerializationBinder = new TypesBinder
                    {
                        KnownTypes = _knownTypes
                    }
                }) ?? throw new ArgumentException($"Can not deserialize {nameof(jsonTree)}");
            }
            catch (Exception ex)
            {
                throw;
            }
            return result;
        }
    }
        
}