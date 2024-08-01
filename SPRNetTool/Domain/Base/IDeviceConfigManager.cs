using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtWiz.Domain.Base
{
    public interface IDeviceConfigManager : IObservableDomain, IDomainAdapter
    {
        bool IsDebugMode();
    }
}
