using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace ArtWiz.View.Utils
{
    public static class ViewExtension
    {
        public static T? FindAncestor<T>(this DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            } while (current != null);

            return null;
        }

        public static IEnumerable<T> FindElementsByTag<T>(this DependencyObject parent, object tag) where T : FrameworkElement
        {
            if (parent == null) yield break;

            foreach (var child in LogicalTreeHelper.GetChildren(parent))
            {
                if (child is T typedChild && Equals(typedChild.Tag, tag))
                {
                    yield return typedChild;
                }
                else if (child is DependencyObject dependencyObject)
                {
                    foreach (var descendant in dependencyObject.FindElementsByTag<T>(tag))
                    {
                        yield return descendant;
                    }
                }
            }
        }

        public static IEnumerable<T> GetLogicalChildren<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            foreach (var child in LogicalTreeHelper.GetChildren(parent))
            {
                if (child is T typedChild)
                {
                    yield return typedChild;
                }
                else if (child is DependencyObject dependencyObject)
                {
                    foreach (var descendant in dependencyObject.GetLogicalChildren<T>())
                    {
                        yield return descendant;
                    }
                }
            }
        }
    }
}
