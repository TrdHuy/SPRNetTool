using ArtWiz.Utils;
using System;
using System.Collections.Generic;

namespace ArtWiz.Domain.Base
{
    public interface IDomainAccessors
    {
        protected class DomainContext
        {
            public static DomainContext ApplicationDomainContext = new DomainContext();

            private Dictionary<Type, object?[]> domainsList;

            DomainContext()
            {
                domainsList = new Dictionary<Type, object?[]>
                {
                    {
                        typeof(IBitmapDisplayManager), BuildValue(null, () => new BitmapDisplayManager())
                    },
                    {
                        typeof(ISprWorkManager), BuildValue(null, () => new SprWorkManager())
                    },
                    {
                        typeof(IDeviceConfigManager), BuildValue(null, () => new DeviceConfigManager())
                    },
                    {
                        typeof(IPakWorkManager), BuildValue(null, () => new PakWorkManagerImpl())
                    },
                };
            }

            ~DomainContext()
            {
                domainsList?.Clear();
            }

            private object?[] BuildValue(params object?[] values)
            {
                return values;
            }

            public static T GetDomain<T>() where T : IObservableDomain
            {
                var type = typeof(T);
                if (ApplicationDomainContext.domainsList.ContainsKey(type))
                {
                    var item = ApplicationDomainContext.domainsList[type];
                    if (item[0] != null && item[0] is Type)
                    {
                        var referType = item[0] as Type;
                        return (T)GetDomain(referType!);
                    }
                    return (T)(item[0] ?? ((Func<IObservableDomain>)item[1]!)().Also((it) =>
                    {
                        item[0] = it;
                    }));
                }
                else
                {
                    throw new InvalidOperationException("Domain was not registered.");
                }
            }

            private static object GetDomain(Type type)
            {
                if (ApplicationDomainContext.domainsList.ContainsKey(type))
                {
                    var item = ApplicationDomainContext.domainsList[type];
                    return (item[0] ?? ((Func<IObservableDomain>)item[1]!)().Also((it) =>
                    {
                        item[0] = it;
                    }));
                }
                else
                {
                    throw new InvalidOperationException("Domain was not registered.");
                }
            }
        }


    }
}
