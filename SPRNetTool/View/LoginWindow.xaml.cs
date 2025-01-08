using ArtWiz.View.Base;
using ArtWiz.View.Widgets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ArtWiz.View
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class LoginWindow : BaseArtWizWindow, INotifyPropertyChanged
    {
        private bool _isTitleBarHide;

        public bool IsTitleBarHide
        {
            get => _isTitleBarHide;
            set
            {
                if (_isTitleBarHide != value)
                {
                    _isTitleBarHide = value;
                    OnPropertyChanged(nameof(IsTitleBarHide));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public LoginWindow()
        {
            InitializeComponent();
            IsTitleBarHide = true;
            this.DataContext = this;
        }
    }
    
    
}
