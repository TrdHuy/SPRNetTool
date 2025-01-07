using ArtWiz.Domain;
using ArtWiz.Domain.Base;
using ArtWiz.Utils;
using ArtWiz.ViewModel.Base;
using ArtWiz.ViewModel.Widgets;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using static ArtWiz.Domain.BitmapDisplayMangerChangedArg.BitmapDisplayChangedEvent;
using static ArtWiz.Domain.BitmapDisplayMangerChangedArg.SprAnimationChangedEvent;
using static ArtWiz.Domain.SprPaletteChangedArg.ChangedEvent;

namespace ArtWiz.ViewModel.SprEditor
{
    internal class PaletteEditorColorItemViewModel : BaseViewModel, IPaletteEditorColorItemViewModel
    {
        private SolidColorBrush mBrush;
        public PaletteEditorColorItemViewModel(SolidColorBrush brush)
        {
            mBrush = brush;
        }

        public SolidColorBrush ColorBrush
        {
            get => mBrush;
            set
            {
                mBrush = value;
                Invalidate();
            }
        }
    }

    internal class SprPaletteEditorViewModel : BaseSubViewModel, IPaletteEditorViewModel
    {
        private ObservableCollection<IPaletteEditorColorItemViewModel>? _paletteColorItemSource;

        [Bindable(true)]
        public ObservableCollection<IPaletteEditorColorItemViewModel>? PaletteColorItemSource
        {
            get
            {
                return _paletteColorItemSource;
            }
            set
            {
                if (_paletteColorItemSource != value)
                {
                    _paletteColorItemSource = value;
                    Invalidate();
                }
            }
        }

        public SprPaletteEditorViewModel(BaseParentsViewModel parents) : base(parents)
        {
        }

        public override void OnArtWizViewModelOwnerCreate(IArtWizViewModelOwner owner)
        {
            base.OnArtWizViewModelOwnerCreate(owner);
            BitmapDisplayManager.RegisterObserver(this);
        }

        public override void OnArtWizViewModelOwnerDestroy()
        {
            base.OnArtWizViewModelOwnerDestroy();
            BitmapDisplayManager.UnregisterObserver(this);
        }

        protected override void OnDomainChanged(IDomainChangedArgs args)
        {
            if (IsOwnerDestroyed) return;

            switch (args)
            {
                case BitmapDisplayMangerChangedArg castArgs:
                    if (castArgs.Event.HasFlag(SPR_WORKSPACE_RESET))
                    {
                        PaletteColorItemSource = null;
                    }
                    else if (!castArgs.Event.HasFlag(IS_PLAYING_ANIMATION_CHANGED))
                    {
                        if (castArgs.Event.HasFlag(SPR_FILE_PALETTE_CHANGED))
                        {
                            castArgs.PaletteChangedArg?.Apply(it =>
                            {
                                if (it.Event.HasFlag(NEWLY_ADDED))
                                {
                                    ViewModelOwner?.ViewDispatcher.Invoke(() =>
                                    {
                                        PaletteColorItemSource = new ObservableCollection<IPaletteEditorColorItemViewModel>();

                                        it.Palette?.Data?.FoEach(pColor =>
                                        {
                                            PaletteColorItemSource.Add(new PaletteEditorColorItemViewModel(
                                               new SolidColorBrush(Color.FromRgb(pColor.Red,
                                               pColor.Green, pColor.Blue))));
                                        });
                                    });
                                }

                                if (it.Event.HasFlag(COLOR_CHANGED) && PaletteColorItemSource != null)
                                {
                                    PaletteColorItemSource[(int)it.ColorChangedIndex].ColorBrush.Color = it.NewColor;
                                }
                            });
                        }
                    }
                    break;
            }
        }

    }
}
