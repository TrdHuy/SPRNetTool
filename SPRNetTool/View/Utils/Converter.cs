using ArtWiz.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ArtWiz.View.Utils
{
    public class RevertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public enum InvisibleType
    {
        /// <summary>
        /// If false, view will be collapsed
        /// </summary>
        COLLAPSED,
        /// <summary>
        /// If false, view will be hidden
        /// </summary>
        HIDDEN,
        /// <summary>
        /// If true, view will be collapsed
        /// </summary>
        REVERSE_COLLAPSED,
        /// <summary>
        /// If true, view will be hidden
        /// </summary>
        REVERSE_HIDDEN
    }
    public class BoolToObjectConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            object? compareOb = null;
            object? expectedValue = null;
            object? defaultValue = null;
            parameter?.IfIs<object[]>(it =>
            {
                compareOb = it[0];
                defaultValue = it[1];
                expectedValue = it[2];
            });

            if (value.Equals(compareOb))
            {
                return expectedValue;
            }
            else
            {
                return defaultValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ObjectToVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            var invisibleType = InvisibleType.HIDDEN;
            object? compareOb = null;
            parameter?.IfIs<object[]>(it =>
            {
                invisibleType = (InvisibleType)it[0];
                compareOb = it[1];
            });

            switch (invisibleType)
            {
                case InvisibleType.HIDDEN:
                    return !value.Equals(compareOb) ? Visibility.Hidden : Visibility.Visible;
                case InvisibleType.COLLAPSED:
                    return !value.Equals(compareOb) ? Visibility.Collapsed : Visibility.Visible;
                case InvisibleType.REVERSE_HIDDEN:
                    return value.Equals(compareOb) ? Visibility.Hidden : Visibility.Visible;
                case InvisibleType.REVERSE_COLLAPSED:
                    return value.Equals(compareOb) ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            var invisibleType = InvisibleType.HIDDEN;
            parameter?.IfIsThenAlso<InvisibleType>(it => invisibleType = it);

            switch (invisibleType)
            {
                case InvisibleType.HIDDEN:
                    return System.Convert.ToBoolean(value) == false ? Visibility.Hidden : Visibility.Visible;
                case InvisibleType.COLLAPSED:
                    return System.Convert.ToBoolean(value) == false ? Visibility.Collapsed : Visibility.Visible;
                case InvisibleType.REVERSE_HIDDEN:
                    return System.Convert.ToBoolean(value) == true ? Visibility.Hidden : Visibility.Visible;
                case InvisibleType.REVERSE_COLLAPSED:
                    return System.Convert.ToBoolean(value) == true ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var invisibleType = InvisibleType.COLLAPSED;
            parameter?.IfIsThenAlso<InvisibleType>(it => invisibleType = it);

            switch (invisibleType)
            {
                case InvisibleType.HIDDEN:
                    return value == null ? Visibility.Hidden : Visibility.Visible;
                case InvisibleType.COLLAPSED:
                    return value == null ? Visibility.Collapsed : Visibility.Visible;
                case InvisibleType.REVERSE_HIDDEN:
                    return value != null ? Visibility.Hidden : Visibility.Visible;
                case InvisibleType.REVERSE_COLLAPSED:
                    return value != null ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FormulaConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var input = parameter.ToString()?.Trim() ?? throw new Exception();
            return EvaluatePostfixExpression(
                InfixToPostfix(string.Format(input, values)));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static string InfixToPostfix(string infix)
        {
            Dictionary<string, int> precedence = new Dictionary<string, int>
            {
                { "+", 1 },
                { "-", 1 },
                { "*", 2 },
                { "/", 2 },
            };

            Stack<string> operators = new Stack<string>();
            List<string> output = new List<string>();

            string[] tokens = infix.Split(' ');

            foreach (string token in tokens)
            {
                if (double.TryParse(token, out double number))
                {
                    output.Add(token);
                }
                else if (token == "(")
                {
                    operators.Push(token);
                }
                else if (token == ")")
                {
                    while (operators.Count > 0 && operators.Peek() != "(")
                    {
                        output.Add(operators.Pop());
                    }
                    operators.Pop();
                }
                else if (operators.Count == 0 || operators.Peek() == "(" || precedence[token] > precedence[operators.Peek()])
                {
                    operators.Push(token);
                }
                else
                {
                    while (operators.Count > 0 && operators.Peek() != "(" && precedence[token] <= precedence[operators.Peek()])
                    {
                        output.Add(operators.Pop());
                    }
                    operators.Push(token);
                }
            }

            while (operators.Count > 0)
            {
                output.Add(operators.Pop());
            }

            return string.Join(" ", output);
        }

        private static double EvaluatePostfixExpression(string postfix)
        {
            Stack<double> stack = new Stack<double>();
            string[] tokens = postfix.Split(' ');

            foreach (string token in tokens)
            {
                if (double.TryParse(token, out double number))
                {
                    stack.Push(number);
                }
                else
                {
                    double operand2 = stack.Pop();
                    double operand1 = stack.Pop();

                    switch (token)
                    {
                        case "+":
                            stack.Push(operand1 + operand2);
                            break;
                        case "-":
                            stack.Push(operand1 - operand2);
                            break;
                        case "*":
                            stack.Push(operand1 * operand2);
                            break;
                        case "/":
                            stack.Push(operand1 / operand2);
                            break;
                        default:
                            throw new InvalidOperationException("Unsupported operator: " + token);
                    }
                }
            }

            return stack.Pop();
        }
    }

    public class NotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null; 
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
