using ArtWiz.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace ArtWiz.ViewModel
{
    public enum DataContextGeneratorType
    {
        Reuse = 1,
        CreateNew = 2,
    }

    internal class ViewModelGeneratorHelper : MarkupExtension
    {
        private static Dictionary<Type, object?> DataContextCache = new Dictionary<Type, object?>();
        public Type? DataContextType { get; set; }
        public DataContextGeneratorType GeneratorType { get; set; }

        public override object? ProvideValue(IServiceProvider serviceProvider)
        {
            if (DataContextType != null)
            {
                if (GeneratorType == DataContextGeneratorType.Reuse)
                {
                    if (DataContextCache.ContainsKey(DataContextType))
                    {
                        return DataContextCache[DataContextType];
                    }
                    else
                    {
                        var context = Activator.CreateInstance(DataContextType);
                        DataContextCache[DataContextType] = context;
                        return context;
                    }
                }
                else
                {
                    // Đặt trong try catch để tránh lỗi null trong lúc design time
                    try
                    {
                        var context = Activator.CreateInstance(DataContextType);
                        return context;
                    }
                    catch { }

                }
            }
            return null;
        }
    }
}
