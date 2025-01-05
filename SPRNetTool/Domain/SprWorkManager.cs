using ArtWiz.Domain.Base;
using WizMachine;

namespace ArtWiz.Domain
{
    public class SprWorkManager : BaseDomain, ISprWorkManager
    {
        protected WizMachine.Services.Base.ISprWorkManagerAdvance sprWorkManagerService;

        WizMachine.Services.Base.ISprWorkManagerAdvance
            ISprWorkManager.SprWorkManagerService => sprWorkManagerService;

        public SprWorkManager()
        {
            sprWorkManagerService = EngineKeeper.GetSprWorkManagerService();
        }

    }
}
