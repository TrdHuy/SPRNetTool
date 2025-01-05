using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtWiz.View.Base
{
    internal interface IPageViewerChild : IViewerElement
    {
        IPageViewer OwnerPage { get; set; }
    }
}
