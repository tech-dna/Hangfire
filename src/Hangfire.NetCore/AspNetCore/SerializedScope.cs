using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Hangfire.AspNetCore;

public class SerializedScopes : IEnumerable<SerializedScope>
{
    private readonly List<SerializedScope> _scopes = new();

    public SerializedScopes() { }

    public SerializedScopes(IEnumerable<SerializedScope> scopes)
    {
        if (scopes == null) return;
        if (scopes is List<SerializedScope> list)
        {
            _scopes = list;
        }
        else
        {
            _scopes.AddRange(scopes);
        }
    }

    public void Add<T>(object implementer, IEnumerable<Type> mirrors = null)
    {
        Add(new SerializedScope(implementer, [typeof(T)]));
        if (mirrors == null) return;
        foreach (var mirror in mirrors)
        {
            Add(new SerializedScope(implementer, [mirror]));
        }
    }
    public void Add(SerializedScope scope)
    {
        if (scope == null) throw new ArgumentNullException(nameof(scope));
        _scopes.Add(scope);
    }


    public IEnumerator<SerializedScope> GetEnumerator()
    {
        return _scopes.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public virtual object GetServiceScope(Type t)
    {
        return _scopes.Find(s => s.ImplementedInterfaces.Contains(t))?
            .BackingObject();
    }

    public IEnumerable<SerializedScope> GetAll()
    {
        return _scopes.AsReadOnly();
    }
    public IEnumerable<T> GetAll<T>() where T : class
    {
        //var implementers = _scopes.Where(s => s.Implementer == typeof(T))
        //    .Select(z => z.BackingObject<T>());
        return GetAll(typeof(T))?.Cast<T>();
    }
    public IEnumerable GetAll(Type t)
    {
        var implementers = _scopes.Where(s => s.Implementer == t)
            .Select(z => z.BackingObject()).ToList();

        return implementers.Count > 0? implementers : null;
    }
    public T GetServiceScope<T>()
        where T: class
    {
        /*
         var enumerableType = enumerable.GetType();
           if (enumerableType.IsGenericType && enumerableType.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
               return enumerableType.GetGenericArguments()[0];
           }
         *
         */
        return _scopes.Find(s => s.ImplementedInterfaces.Contains(typeof(T)))?
            .BackingObject<T>();
    }

    public SerializedScopes Merge(SerializedScopes secondScopes)
    {
        _scopes.AddRange(secondScopes._scopes);
        return this;
    }
}
public class SerializedScope
{
    public IEnumerable<Type> ImplementedInterfaces
    {
        get;
        set;
    }
    private Type _implementer;
    private object _backingObject;

    public object BackingObject()
    {
        return Convert.ChangeType(_backingObject, Implementer, CultureInfo.InvariantCulture) ??
               (Serialization != null ? JsonConvert.DeserializeObject(Serialization, Implementer) : null);
    }
    public T BackingObject<T>(object value = null)
        where T : class
    {
        if (value != null)
        {
#if !NETSTANDARD1_3
                if (!typeof(T).IsAssignableFrom(value.GetType()))
#else
            if (typeof(T).GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo()))
#endif
                throw new ArgumentException($"value must be of type {typeof(T).FullName} or derive from it");
            _backingObject = value;
        }

        //var c = Convert.ChangeType(_backingObject, Implementer, CultureInfo.InvariantCulture);
        return _backingObject as T ?? 
               (Serialization !=null? JsonConvert.DeserializeObject<T>(Serialization):null);
    }
    public string Serialization { get; set; }
    public Type Implementer
    {
        get => _implementer ?? Type.GetType(ImplementerName);
        private set => _implementer = value ?? throw new ArgumentNullException(nameof(Implementer));
    }
    private string _implementerName;
    public string ImplementerName
    {
        get => _implementerName ?? _implementer.AssemblyQualifiedName;
        set => _implementerName = value ?? throw new ArgumentNullException(nameof(ImplementerName));
    }
    public SerializedScope(){}
    public SerializedScope(object implementer)
        : this(implementer,
#if !NETSTANDARD1_3
            implementer.GetType().GetInterfaces()
#else
            implementer.GetType().GetTypeInfo().ImplementedInterfaces
#endif
        )
    {

    }
    public SerializedScope(object implementer, IEnumerable<Type> implementedInterfaces)
    {
        _backingObject = implementer;
        Implementer = implementer.GetType();
        var interfaceList = implementedInterfaces.ToList();
        interfaceList.Add(Implementer);
        ImplementedInterfaces = interfaceList;
        Serialization = JsonConvert.SerializeObject(implementer);
    }
    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}