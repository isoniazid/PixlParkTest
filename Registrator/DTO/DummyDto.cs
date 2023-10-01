/* using System.Text.Json.Serialization;
using HallOfFame.Model;

namespace HallOfFame.DTO
{
    public class PersonDtoBaseInfo : IMapFrom<Person>
    {
        [JsonPropertyName("id")]
        public long Id {get; set;}

        [JsonPropertyName("name")]
        public string Name {get; set;} = null!;

        [JsonPropertyName("displayName")]
        public string DisplayName {get; set;} = null!;

        [JsonPropertyName("skills")]
        public List<SkillDtoBaseInfo>? Skills {get; set;}    

    }
} */