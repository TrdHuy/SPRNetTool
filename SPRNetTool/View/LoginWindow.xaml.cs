using ArtWiz.View.Base;
using ArtWiz.View.Widgets;
using ArtWiz.ViewModel;
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
    public partial class LoginWindow : BaseArtWizWindow
    {
        private LoginWindowViewModel _loginWindowViewModel;

        public LoginWindow()
        {
            InitializeComponent();
            _loginWindowViewModel = new LoginWindowViewModel();
            DataContext = _loginWindowViewModel;
        }
    }
    
    
}
