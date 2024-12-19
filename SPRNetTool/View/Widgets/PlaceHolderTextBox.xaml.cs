﻿using ArtWiz.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ArtWiz.View.Widgets
{
    /// <summary>
    /// Interaction logic for PlaceHolderTextBox.xaml
    /// </summary>
    public partial class PlaceHolderTextBox : UserControl
    {
        // Dependency Property for Placeholder
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(PlaceHolderTextBox), new PropertyMetadata(string.Empty));

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        // Dependency Property for Text
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(PlaceHolderTextBox), new PropertyMetadata(string.Empty));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty CornerRadiusProperty
            = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(PlaceHolderTextBox),
                                          new FrameworkPropertyMetadata(
                                                new CornerRadius(),
                                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
                                          new ValidateValueCallback(IsCornerRadiusValid));
        private static bool IsCornerRadiusValid(object value)
        {
            CornerRadius cr = (CornerRadius)value;
            return (cr.IsValid(false, false, false, false));
        }

        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        public static readonly DependencyProperty PlaceholderForegroundProperty =
                DependencyProperty.Register("PlaceholderForeground", typeof(Brush),
                        typeof(PlaceHolderTextBox),
                        new FrameworkPropertyMetadata(SystemColors.ControlTextBrush,
                            FrameworkPropertyMetadataOptions.Inherits));

        public Brush PlaceholderForeground
        {
            get { return (Brush)GetValue(PlaceholderForegroundProperty); }
            set { SetValue(PlaceholderForegroundProperty, value); }
        }

        public PlaceHolderTextBox()
        {
            InitializeComponent();
        }
    }
}