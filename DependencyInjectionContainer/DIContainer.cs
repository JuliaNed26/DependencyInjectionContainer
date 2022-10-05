using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.Reflection;

namespace DependencyInjectionContainer
{
    public sealed class DIContainer
    {
        private List<Service> _services;
        private DIContainer _curContainerParent;

        internal DIContainer(List<Service> services, DIContainer containerParent)
        {
            _services = services;
            _curContainerParent = containerParent;
        }

        public DIContainerBuilder CreateAChildContainer()
        {
            return new DIContainerBuilder(this);
        }

        public TResolveType Resolve<TResolveType>(ImportSource importSource = ImportSource.Any) where TResolveType : class?
        {
            return (TResolveType)Resolve(typeof(TResolveType), importSource);
        }

        public IEnumerable<TResolveType> ResolveMany<TResolveType>(ImportSource importSource = ImportSource.Any) where TResolveType : class?
        {
            List<TResolveType> resolvedServices = new List<TResolveType>();
            var resolveLocal = (ImportSource _impSrcType) =>
                    resolvedServices.AddRange(_services.Where(service => IsServiceOfGivenType(service, typeof(TResolveType)))
                                            .Select(service => (TResolveType)Resolve(service.ImplementationType, _impSrcType)).ToList());
            var resolveNonLocal = (ImportSource _impSrcType) => 
            {if (_curContainerParent != null) { resolvedServices.AddRange(_curContainerParent.ResolveMany<TResolveType>(_impSrcType)); }};

            switch (importSource)
            {
                case ImportSource.Any:
                    resolveLocal(importSource);
                    resolveNonLocal(importSource);
                    break;
                case ImportSource.Local:
                    resolveLocal(importSource);
                    break;
                case ImportSource.NonLocal:
                    if (_curContainerParent == null)
                        throw new ArgumentException("This container does not have a parent");
                    resolveNonLocal(ImportSource.Any);
                    break;
                default:
                    break;
            }

            return resolvedServices;
        }

        private object Resolve(Type typeToResolve, ImportSource importSource)
        {
            if(importSource == ImportSource.NonLocal)
            {
                if(_curContainerParent == null)
                    throw new ArgumentException("This container does not have a parent");

                return _curContainerParent.Resolve(typeToResolve, ImportSource.Any);
            }

            var servicesToResolve = _services.Where(service => IsServiceOfGivenType(service, typeToResolve));

            if (servicesToResolve.Count() > 1)
            {
                throw new ArgumentException($"Many services with type {typeToResolve} was registered. Use ResolveMany to resolve them all");
            }

            if (servicesToResolve.Count() == 0)
            {
                if (_curContainerParent != null && importSource == ImportSource.Any)
                {
                    return _curContainerParent.Resolve(typeToResolve,importSource);
                }
                else
                {
                    throw new NullReferenceException($"Do not have registrated services of type {typeToResolve.FullName}");
                }
            }

            Service serviceToResolve = servicesToResolve.First();

            if (serviceToResolve.Implementation != null)
            {
                return serviceToResolve.Implementation;
            }

            var implementation = GetCreatedImplementationForService(serviceToResolve);

            if (serviceToResolve.Lifetime == ServiceLifetime.Singleton)
            {
                serviceToResolve.Implementation = implementation;
            }

            return implementation;

            object GetCreatedImplementationForService(Service service)
            {
                IEnumerable<ConstructorInfo> constructors;
                constructors = service.ImplementationType.GetConstructors()
                    .Where(constructor => constructor.GetCustomAttribute<ImportingConstructorAttribute>() != null);

                if (constructors.Count() == 0)
                {
                    constructors = service.ImplementationType.GetConstructors();
                }

                var parameters = constructors.First().
                                 GetParameters()
                                 .Select(parameter => Resolve(parameter.ParameterType,GetParamAttributeImportSource(parameter))).ToArray();

                return Activator.CreateInstance(service.ImplementationType, parameters);

                ImportSource GetParamAttributeImportSource(ParameterInfo parameter)
                {
                    var impManyAttribute = parameter.GetCustomAttribute<ImportManyAttribute>();
                    return impManyAttribute != null ? impManyAttribute.Source : ImportSource.Any;
                }
            }
        }


        private bool IsServiceOfGivenType(Service service, Type type) => service.InterfaceType == type ||
            service.ImplementationType == type;
    }
}
