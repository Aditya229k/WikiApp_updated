using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace WikiApp.Converters
{
    public class SnippetHighlighterConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not string snippet || values[1] is not string keyword)
                return null;

            var result = new Span();
            int index = snippet.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);

            if (index < 0)
            {
                result.Inlines.Add(new Run(snippet));
                return result;
            }

            result.Inlines.Add(new Run(snippet.Substring(0, index)));

            var match = new Run(snippet.Substring(index, keyword.Length))
            {
                Background = Brushes.Yellow,
                FontWeight = FontWeights.Bold
            };
            result.Inlines.Add(match);

            int end = index + keyword.Length;
            if (end < snippet.Length)
            {
                result.Inlines.Add(new Run(snippet.Substring(end)));
            }

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
