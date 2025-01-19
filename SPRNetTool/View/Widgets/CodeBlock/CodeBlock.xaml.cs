using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ArtWiz.View.Widgets.CodeBlock
{
    /// <summary>
    /// Interaction logic for CodeBlock.xaml
    /// </summary>
    public partial class CodeBlock : VerticalVirtualizingPanel
    {
        public CodeBlock()
        {
            InitializeComponent();
        }

        public override Canvas PART_ContentCanvasContainer => ContentCanvasContainer;

        public override Canvas PART_MainCanvasContainer => MainCanvasContainer;
    }
}
