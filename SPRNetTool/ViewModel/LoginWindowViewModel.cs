using ArtWiz.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtWiz.ViewModel
{
    class LoginWindowViewModel : BaseParentsViewModel
    {

        private bool _isTitleBarHide;
        public bool IsTitleBarHide
        {
            get { return _isTitleBarHide; }
            set
            {
                _isTitleBarHide = value;
                Invalidate(nameof(IsTitleBarHide));
            }
        }

        public LoginWindowViewModel()
        {
            IsTitleBarHide = true;
        }

    }
}
