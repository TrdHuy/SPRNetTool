using ArtWiz.Utils;
using ArtWiz.View.Utils;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ArtWiz.View.Widgets
{
    public class CollapsibleControl : UserControl
    {
        public static string TAG_COLAPSE_BUTTON = "ColapsibleControl_ColapseButton";
        public static readonly DependencyProperty CollapseVelocityProperty =
           DependencyProperty.Register(
               "CollapseVelocity",
               typeof(uint),
               typeof(CollapsibleControl),
               new PropertyMetadata(2200u));

        public uint CollapseVelocity
        {
            get { return (uint)GetValue(CollapseVelocityProperty); }
            set { SetValue(CollapseVelocityProperty, value); }
        }

        public static readonly DependencyProperty IsCollapseProperty =
           DependencyProperty.Register(
               "IsCollapse",
               typeof(bool),
               typeof(CollapsibleControl),
               new PropertyMetadata(false, OnChanged));

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Kiểm tra chế độ thiết kế
            //if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(d) && d is CollapsibleControl c)
            //{
            //    if (e.NewValue.Equals(true))
            //    {
            //        c.mainBoderContainer.Height = 0;
            //    }
            //    else
            //    {
            //        c.mainBoderContainer.Height = c.oldHeightCache;
            //    }
            //}
        }

        public bool IsCollapse
        {
            get { return (bool)GetValue(IsCollapseProperty); }
            set { SetValue(IsCollapseProperty, value); }
        }

        public static readonly DependencyProperty CustomHeaderContentProperty =
           DependencyProperty.Register(
               "CustomHeaderContent",
               typeof(object),
               typeof(CollapsibleControl),
               new PropertyMetadata(default(object), OnCustomHeaderContentChanged));

        private static void OnCustomHeaderContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CollapsibleControl control)
            {
                control.UpdateHeaderRowHeight();
                control.OnCustomHeaderContentChanged(e.OldValue, e.NewValue);
            }
        }

        public object CustomHeaderContent
        {
            get { return GetValue(CustomHeaderContentProperty); }
            set { SetValue(CustomHeaderContentProperty, value); }
        }

        public static readonly DependencyProperty ExtraHeaderContentProperty =
           DependencyProperty.Register(
               "ExtraHeaderContent",
               typeof(object),
               typeof(CollapsibleControl),
               new PropertyMetadata(default(object)));

        public object ExtraHeaderContent
        {
            get { return GetValue(ExtraHeaderContentProperty); }
            set { SetValue(ExtraHeaderContentProperty, value); }
        }

        public static readonly DependencyProperty HeaderProperty =
           DependencyProperty.Register(
               "Header",
               typeof(string),
               typeof(CollapsibleControl),
               new PropertyMetadata(default(string), propertyChangedCallback: OnHeaderChanged));

        private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.IfIs<CollapsibleControl>(it =>
            {
                it.headerTextBlock?.Apply(it => it.Text = e.NewValue.ToString());
            });
        }

        public string Header
        {
            get { return GetValue(HeaderProperty)?.ToString() ?? ""; }
            set { SetValue(HeaderProperty, value); }
        }

        private TextBlock? headerTextBlock;
        private Border? mainBoderContainer;
        private RowDefinition? fixedRowDef;
        private Grid? fixedRowPanel;
        private ContentPresenter? customHeaderPresenter;
        public CollapsibleControl()
        {
        }

        public override void OnApplyTemplate()
        {
            customHeaderPresenter = GetTemplateChild("CustomHeaderPresenter") as ContentPresenter;
            fixedRowPanel = GetTemplateChild("FixedRowPanel") as Grid;
            fixedRowDef = GetTemplateChild("FixedHeaderRow") as RowDefinition;
            headerTextBlock = GetTemplateChild("Header") as TextBlock;
            mainBoderContainer = GetTemplateChild("MainBorderContainer") as Border;
            headerTextBlock?.Apply(it => it.Text = Header);
            GetTemplateChild("CollapseButton").IfIs<Button>(it =>
            {
                it.Click -= CollapseButton_Click;
                it.Click += CollapseButton_Click;
            });
            GetTemplateChild("CollapseButton").IfIs<IconToggle>(it =>
            {
                it.Click -= CollapseButton_Click;
                it.Click += CollapseButton_Click;
            });
            UpdateHeaderRowHeight();
            OnCustomHeaderContentChanged(null, CustomHeaderContent);
        }

        private void OnCustomHeaderContentChanged(object? oldContent, object? newContent)
        {
            if (customHeaderPresenter == null) return;

            // Gán sự kiện cho các nút trong nội dung mới
            if (newContent is UIElement newUIContent)
            {
                var buttons = newUIContent.FindElementsByTag<Button>(TAG_COLAPSE_BUTTON);
                foreach (var button in buttons)
                {
                    button.Click -= CollapseButton_Click;
                    button.Click += CollapseButton_Click;
                }
                var iconbuttons = newUIContent.FindElementsByTag<IconToggle>(TAG_COLAPSE_BUTTON);
                foreach (var button in iconbuttons)
                {
                    button.Click -= CollapseButton_Click;
                    button.Click += CollapseButton_Click;
                }
            }

            // Hủy sự kiện nếu nội dung cũ có
            if (oldContent is UIElement oldUIContent)
            {
                var buttons = oldUIContent.FindElementsByTag<Button>(TAG_COLAPSE_BUTTON);
                foreach (var button in buttons)
                {
                    button.Click -= CollapseButton_Click;
                }
                var iconbuttons = oldUIContent.FindElementsByTag<IconToggle>(TAG_COLAPSE_BUTTON);
                foreach (var button in iconbuttons)
                {
                    button.Click -= CollapseButton_Click;
                }
            }
        }

        private void CollapseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsCollapse)
            {
                BeginCollapseAnimation();
            }
            else
            {
                BeginExpandAnimation();
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (Double.IsNaN(oldHeightCache))
            {
                mainBoderContainer?.Apply(it =>
                {
                    it.Measure(constraint);
                    oldHeightCache = it.DesiredSize.Height;
                    uiContentCache = mainBoderContainer.Child;
                    if (IsCollapse)
                    {
                        mainBoderContainer.Height = 0;
                        mainBoderContainer.Child = null;
                    }
                });
            }

            return base.MeasureOverride(constraint);
        }

        #region colapse animation
        private double oldHeightCache = Double.NaN;
        private object? uiContentCache;
        private void BeginCollapseAnimation()
        {
            if (mainBoderContainer != null && !IsCollapse)
            {
                oldHeightCache = mainBoderContainer.ActualHeight;
                uiContentCache = mainBoderContainer.Child;
                var animationStoryboard = new Storyboard();
                var time = oldHeightCache / CollapseVelocity;
                DoubleAnimation collapseAnim = new DoubleAnimation(oldHeightCache, 0, TimeSpan.FromSeconds(time));
                Storyboard.SetTarget(collapseAnim, mainBoderContainer);
                Storyboard.SetTargetProperty(collapseAnim, new PropertyPath("(Border.Height)"));
                animationStoryboard.Children.Add(collapseAnim);
                animationStoryboard.FillBehavior = FillBehavior.HoldEnd;
                animationStoryboard.Completed += (_, _) =>
                {
                    mainBoderContainer.Child = null;
                    IsCollapse = !IsCollapse;
                };
                this.BeginStoryboard(animationStoryboard);
            }
        }

        private void BeginExpandAnimation()
        {
            if (mainBoderContainer != null && IsCollapse)
            {
                var animationStoryboard = new Storyboard();
                var time = oldHeightCache / CollapseVelocity;
                DoubleAnimation collapseAnim = new DoubleAnimation(0, oldHeightCache, TimeSpan.FromSeconds(time));
                Storyboard.SetTarget(collapseAnim, mainBoderContainer);
                Storyboard.SetTargetProperty(collapseAnim, new PropertyPath("(Border.Height)"));
                animationStoryboard.Children.Add(collapseAnim);
                animationStoryboard.FillBehavior = FillBehavior.HoldEnd;
                animationStoryboard.Completed += (_, _) =>
                {
                    uiContentCache?.IfIs<UIElement>(it => mainBoderContainer.Child = it);
                    IsCollapse = !IsCollapse;
                };
                this.BeginStoryboard(animationStoryboard);
            }
        }
        #endregion

        private void UpdateHeaderRowHeight()
        {
            if (fixedRowDef == null || fixedRowPanel == null || customHeaderPresenter == null) return;


            if (CustomHeaderContent == null)
            {
                fixedRowDef.Height = new GridLength(40);
                fixedRowPanel.Visibility = Visibility.Visible;
                customHeaderPresenter.Visibility = Visibility.Collapsed;
            }
            else
            {
                fixedRowDef.Height = new GridLength(0);
                fixedRowPanel.Visibility = Visibility.Collapsed;
                customHeaderPresenter.Visibility = Visibility.Visible;
            }
        }
    }
}
