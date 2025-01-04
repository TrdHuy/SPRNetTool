using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WizMachine.Data;
using WizMachine.Services.Base;

namespace ArtWiz.Domain.Utils
{
    internal static class SprUtil
    {
        public static bool ParseSprData(byte[] sprData, out SprFileHead sprFileHead,
            out Palette palette,
            out int frameDataBeginPos,
            out FrameRGBA[] frameRGBA)
        {
            var initResult = ISprWorkManagerCore.ParseSprData(sprData, out sprFileHead,
                               out palette,
                               out frameDataBeginPos,
                               out frameRGBA);
            return initResult;
        }
    }
}
