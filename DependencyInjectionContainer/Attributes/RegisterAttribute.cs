namespace DependencyInjectionContainer.Attributes;
using System;
using Enums;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RegisterAttribute : Attribute
{
    public RegisterAttribute(LifetimeOfService lifetime, Type interfaceType)
    {
        InterfaceType = interfaceType;
        Lifetime = lifetime;
        IsRegisteredByInterface = true;
    }

    public RegisterAttribute(LifetimeOfService lifetime)
    {
        Lifetime = lifetime;
        IsRegisteredByInterface = false;
    }

    public bool IsRegisteredByInterface { get; init; }
    public Type? InterfaceType { get;}
    public LifetimeOfService Lifetime { get;}
}
