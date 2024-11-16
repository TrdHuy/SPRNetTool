using ArtWiz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ArtWiz.View.Utils
{
    public enum AppMenuTag
    {
        HomeMenu = 10000,
        SprEditorPageMenu = 10001,
        PakEditorPageMenu = 10002,
        DevPageMenu = 10003,

        SupportMenu = 10100,

        ToolMenu = 10200,
    }

    public class PreProcessMenuItemInfo
    {
        public Visibility Visibility { get; set; }
        public AppMenuTag MenuTag { get; set; }

        public static PreProcessMenuItemInfo? MakeFromMenuItem(MenuItem menuItem)
        {
            return menuItem.Tag.IfIsThenLet<AppMenuTag, PreProcessMenuItemInfo>(it =>
            {
                return new PreProcessMenuItemInfo()
                {
                    Visibility = menuItem.Visibility,
                    MenuTag = it
                };
            }) ?? null;

        }
    }

    public static class Definition
    {

    }
}
