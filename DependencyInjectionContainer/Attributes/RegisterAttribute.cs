namespace DependencyInjectionContainer.Attributes;
using System;
using Enums;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RegisterAttribute : Attribute
{
    public RegisterAttribute(ServiceLifetime lifetime, Type interfaceType)
    {
        InterfaceType = interfaceType;
        Lifetime = lifetime;
    }
    public RegisterAttribute(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
    }

    public bool IsRegisteredByInterface { get; init; }
    public Type InterfaceType { get;}
    public ServiceLifetime Lifetime { get;}
}
