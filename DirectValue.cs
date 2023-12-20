using System.Reflection.Metadata.Ecma335;
using System.Security;
namespace ObjectStoreE
{
    public class DirectValue : IDisposable
    {
        public string name;
        public string? value;
        public DirectValue(string name, string? value, bool decodeValue = false)
        {
            this.name = name;
            if (decodeValue)
                this.value = DirectValueClearify.DecodeInvalidCharCode(value);
            else
                this.value = value;
        }
        ~DirectValue() 
        {
            Dispose();
        }
        public void Dispose()
        {
            name = null!;
            value = null!;
            GC.SuppressFinalize(this);
        }
    }

}