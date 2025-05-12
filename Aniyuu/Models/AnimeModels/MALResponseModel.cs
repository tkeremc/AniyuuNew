using System.Text.Json.Serialization; // JsonPropertyName için
using System.Collections.Generic; // List için

// Ana MAL Anime Yanıt Modeli (Seçilen Alanlara Göre)
public class MALResponseModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("main_picture")]
    public MalMainPicture? MainPicture { get; set; }

    [JsonPropertyName("alternative_titles")]
    public MalAlternativeTitles? AlternativeTitles { get; set; }

    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; } // DateTime'a parse edilecek

    [JsonPropertyName("end_date")]
    public string? EndDate { get; set; } // DateTime'a parse edilecek

    [JsonPropertyName("synopsis")]
    public string? Synopsis { get; set; }

    [JsonPropertyName("mean")]
    public double? Mean { get; set; } // Senin AnimeModel.MALScore alanına karşılık gelebilir

    [JsonPropertyName("rank")]
    public int? Rank { get; set; }

    [JsonPropertyName("popularity")]
    public int? Popularity { get; set; }

    [JsonPropertyName("nsfw")]
    public string? Nsfw { get; set; }

    [JsonPropertyName("media_type")]
    public string? MediaType { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("genres")]
    public List<MalGenre>? Genres { get; set; }

    [JsonPropertyName("num_episodes")]
    public int? NumEpisodes { get; set; }

    [JsonPropertyName("start_season")]
    public MalStartSeason? StartSeason { get; set; }

    [JsonPropertyName("broadcast")]
    public MalBroadcast? Broadcast { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("rating")]
    public string? Rating { get; set; } // Örn: "pg_13"

    [JsonPropertyName("pictures")]
    public List<MalMainPicture>? Pictures { get; set; } // MalMainPicture ile aynı yapıda

    [JsonPropertyName("related_anime")]
    public List<MalRelatedAnimeEdge>? RelatedAnime { get; set; } // "Edge" yapısı genelde node ve ilişki tipi içerir

    [JsonPropertyName("studios")]
    public List<MalStudio>? Studios { get; set; }
}

// --- Alt Modeller (Yukarıdaki Ana Modelde Kullanılanlar) ---

public class MalMainPicture
{
    [JsonPropertyName("medium")]
    public string? Medium { get; set; }

    [JsonPropertyName("large")]
    public string? Large { get; set; }
}

public class MalAlternativeTitles
{
    [JsonPropertyName("synonyms")]
    public List<string>? Synonyms { get; set; }

    [JsonPropertyName("en")]
    public string? En { get; set; }

    [JsonPropertyName("ja")]
    public string? Ja { get; set; }
}

public class MalGenre
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class MalStartSeason
{
    [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("season")]
    public string? Season { get; set; } // Örn: "spring", "summer", "fall", "winter"
}

public class MalBroadcast
{
    [JsonPropertyName("day_of_the_week")]
    public string? DayOfTheWeek { get; set; }

    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; } // Örn: "22:30" (HH:mm formatında)
}

public class MalRelatedAnimeEdge // related_anime listesindeki her bir öğe için
{
    [JsonPropertyName("node")]
    public MalAnimeNode? Node { get; set; }

    // Eğer MAL API'si "relation_type" gibi ek bilgiler de veriyorsa buraya eklenebilir:
    // [JsonPropertyName("relation_type")]
    // public string? RelationType { get; set; }
    // [JsonPropertyName("relation_type_formatted")]
    // public string? RelationTypeFormatted { get; set; }
}

public class MalAnimeNode // related_anime içindeki "node" objesi için
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("main_picture")]
    public MalMainPicture? MainPicture { get; set; }
}

public class MalStudio
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}