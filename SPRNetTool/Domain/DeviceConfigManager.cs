using ArtWiz.Domain.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtWiz.Domain
{
    internal class DeviceConfigManager : BaseDomain, IDeviceConfigManager
    {
        public bool IsDebugMode()
        {
#if DEBUG
            return true;
#endif
            return false;
        }
    }
}
