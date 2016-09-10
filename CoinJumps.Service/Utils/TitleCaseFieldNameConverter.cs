using Nancy.ModelBinding;
using ServiceStack.Text;

namespace CoinJumps.Service.Utils
{
    public class TitleCaseFieldNameConverter : IFieldNameConverter
    {
        public string Convert(string fieldName)
        {
            return fieldName.ToTitleCase();
        }
    }
}