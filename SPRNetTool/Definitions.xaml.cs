using System.Runtime.CompilerServices;
using System.Windows;

namespace ArtWiz
{
    public partial class Definitions : ResourceDictionary
    {
        public static ResourceKey BackgroundColorLevel0 { get; } = CreateResourceKey();
        public static ResourceKey BackgroundColorLevel1 { get; } = CreateResourceKey();
        public static ResourceKey BackgroundColorLevel2 { get; } = CreateResourceKey();
        public static ResourceKey BackgroundColorLevel5 { get; } = CreateResourceKey();
        public static ResourceKey BackgroundColorLevel8 { get; } = CreateResourceKey();
        public static ResourceKey BackgroundColorLevel10 { get; } = CreateResourceKey();
        public static ResourceKey BackgroundColorLevel15 { get; } = CreateResourceKey();
        public static ResourceKey ForegroundColorLevel0 { get; } = CreateResourceKey();
        public static ResourceKey ForegroundColorLevel0_1 { get; } = CreateResourceKey();
        public static ResourceKey ForegroundColorLevel1 { get; } = CreateResourceKey();
        public static ResourceKey ForegroundEffectColorLevel1 { get; } = CreateResourceKey();
        public static ResourceKey ForegroundEffectColorLevel2 { get; } = CreateResourceKey();
        public static ResourceKey ForegroundEffectColorLevel2_OP1 { get; } = CreateResourceKey();
        public static ResourceKey ForegroundEffectColorLevel2_OP2 { get; } = CreateResourceKey();
        public static ResourceKey ForegroundEffectColorLevel3 { get; } = CreateResourceKey();
        public static ResourceKey ForegroundEffectColorLevel4 { get; } = CreateResourceKey();

        public static ResourceKey BackgroundLevel0 { get; } = CreateResourceKey();
        public static ResourceKey BackgroundLevel1 { get; } = CreateResourceKey();
        public static ResourceKey BackgroundLevel2 { get; } = CreateResourceKey();
        public static ResourceKey BackgroundLevel5 { get; } = CreateResourceKey();
        public static ResourceKey BackgroundLevel8 { get; } = CreateResourceKey();
        public static ResourceKey BackgroundLevel10 { get; } = CreateResourceKey();
        public static ResourceKey BackgroundLevel15 { get; } = CreateResourceKey();
        public static ResourceKey ForegroundLevel0 { get; } = CreateResourceKey();
        public static ResourceKey ForegroundLevel0_1 { get; } = CreateResourceKey();
        public static ResourceKey ForegroundLevel1 { get; } = CreateResourceKey();
        public static ResourceKey ForegroundEffectLevel1 { get; } = CreateResourceKey();
        public static ResourceKey ForegroundEffectLevel2 { get; } = CreateResourceKey();
        public static ResourceKey ForegroundEffectLevel2_OP1 { get; } = CreateResourceKey();
        public static ResourceKey ForegroundEffectLevel2_OP2 { get; } = CreateResourceKey();
        public static ResourceKey ForegroundEffectLevel3 { get; } = CreateResourceKey();
        public static ResourceKey ForegroundEffectLevel4 { get; } = CreateResourceKey();
        public static ResourceKey MenuItemBackground { get; } = CreateResourceKey();
        public static ResourceKey MenuItemBorderBrush { get; } = CreateResourceKey();
        public static ResourceKey MouseOverMenuItemBackgroundBrush { get; } = CreateResourceKey();

        public static ResourceKey FitToScreenIconGeometry { get; } = CreateResourceKey();
        public static ResourceKey UnFitToScreenIconGeometry { get; } = CreateResourceKey();
        public static ResourceKey GlobalUnTransparentBackgroundIconGeometry { get; } = CreateResourceKey();
        public static ResourceKey GlobalTransparentBackgroundIconGeometry { get; } = CreateResourceKey();
        public static ResourceKey FrameUnTransparentBackgroundIconGeometry { get; } = CreateResourceKey();
        public static ResourceKey FrameTransparentBackgroundIconGeometry { get; } = CreateResourceKey();
        public static ResourceKey LayoutBoundIconGeometry { get; } = CreateResourceKey();
        public static ResourceKey ResetIconGeometry { get; } = CreateResourceKey();
        public static ResourceKey MagnifyIconGeometry { get; } = CreateResourceKey();
        public static ResourceKey ParamSubtracterIconGeometry { get; } = CreateResourceKey();
        public static ResourceKey ParamAdditionIconGeometry { get; } = CreateResourceKey();
        public static ResourceKey OpenFileGeometry { get; } = CreateResourceKey();
        public static ResourceKey PlayIconGeometry { get; } = CreateResourceKey();
        public static ResourceKey PauseIconGeometry { get; } = CreateResourceKey();
        public static ResourceKey SaveFileGeometry { get; } = CreateResourceKey();
        public static ResourceKey DropDownIconGeometry { get; } = CreateResourceKey();
        public static ResourceKey FileImportIconGeometry { get; } = CreateResourceKey();
        public static ResourceKey DeveloperModeIconGeometry { get; } = CreateResourceKey();
        public static ResourceKey SprWorkSpaceIconGeometry { get; } = CreateResourceKey();
        public static ResourceKey ArtWizLogoGeometry { get; } = CreateResourceKey();
        public static ResourceKey ArtWizTextLogoGeometry { get; } = CreateResourceKey();
        public static ResourceKey RightArrowIconGeometry { get; } = CreateResourceKey();
        public static ResourceKey LeftArrowIconGeometry { get; } = CreateResourceKey();
        
        public static ResourceKey ArtWizDefaultWindowStyle { get; } = CreateResourceKey();
        public static ResourceKey UnidentifiedPreviewFrameSourceGeometry { get; } = CreateResourceKey();

        public static Definitions? Instance { get; private set; }

        public Definitions()
        {
            InitializeComponent();
            Instance = this;
        }

        protected override void OnGettingValue(object key, ref object value, out bool canCache)
        {
            base.OnGettingValue(key, ref value, out canCache);
        }

        private static ComponentResourceKey CreateResourceKey([CallerMemberName] string? caller = null)
        {
            return new ComponentResourceKey(typeof(Definitions), caller); ;
        }
    }
}
