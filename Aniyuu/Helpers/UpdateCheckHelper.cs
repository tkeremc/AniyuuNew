namespace Aniyuu.Helpers
{
    public static class UpdateCheckHelper
    {
        public static T ReplaceNullToOldValues<T>(T oldModel, T newModel) where T : class
        {
            if (oldModel == null || newModel == null)
                return newModel ?? oldModel;

            var properties = typeof(T).GetProperties();

            foreach (var property in properties)
            {
                var oldValue = property.GetValue(oldModel);
                var newValue = property.GetValue(newModel);

                if (newValue == null || (newValue is string stringValue && string.IsNullOrEmpty(stringValue)))
                {
                    // Eğer yeni değer null veya boş string ise, eski değeri atıyoruz.
                    property.SetValue(newModel, oldValue);
                }
                else if (property.PropertyType == typeof(DateTime))
                {
                    // Eğer yeni DateTime değeri varsayılan bir tarihse, eski değeri ata.
                    if ((DateTime)newValue == default)
                    {
                        property.SetValue(newModel, oldValue);
                    }
                }
                else if (property.PropertyType == typeof(bool))
                {
                    // Eğer eski değer null değilse ve yeni değer false ise, güncelleme yapma.
                    if ((bool)newValue == false && oldValue is bool oldBool)
                    {
                        property.SetValue(newModel, oldBool);
                    }
                }
            }

            return newModel;
        }
    }
}