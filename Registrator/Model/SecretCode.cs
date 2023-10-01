using System.Text.Json.Serialization;

namespace Registrator.Model
{
    public class SecretCode
    {
        public string CodeString { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime TimeOut { get; set; }
    }
}