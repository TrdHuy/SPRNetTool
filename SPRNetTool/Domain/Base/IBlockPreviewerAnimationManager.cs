using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WizMachine.Data;

namespace ArtWiz.Domain.Base
{

    public interface ISprAnimationCallback
    {
        void OnAnimationEventChange(object args);
    }
    public interface IBlockPreviewerAnimationManager : IObservableDomain, IDomainAdapter
    {
        void StartSprAnimation(ISprAnimationCallback callback);

        void StopSprAnimation(ISprAnimationCallback callback);

        void SetCurrentSprData(SprFileHead fileHead, FrameRGBA[] frameData);

        bool IsReadyToPlayAnimation();
    }
}
