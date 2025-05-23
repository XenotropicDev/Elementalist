﻿using System.Text.Json;
using System.Text.Json.Serialization;
using Elementalist.Models;

namespace Elementalist.DiscordUi;

public class UniqueCardIdentifier
{
    [JsonPropertyName("C")]
    [JsonInclude]
    public string Name { get; private set; }

    [JsonPropertyName("S")]
    [JsonInclude]
    public string Set { get; private set; }

    [JsonPropertyName("P")]
    [JsonInclude]
    public string Product { get; private set; }

    [JsonPropertyName("F")]
    [JsonInclude]
    public string Finish { get; private set; }


    [JsonConstructor]
    public UniqueCardIdentifier(string name, string set, string product, string finish)
    {
        Name = name;
        Set = set;
        Product = product;
        Finish = finish;
    }

    public UniqueCardIdentifier(string cardName, SetVariant setVariant)
    {
        Name = cardName;
        Set = setVariant.Set.Name;
        Product = setVariant.Variant.Product;
        Finish = setVariant.Variant.Finish;
    }

    public override string ToString() => $"{Name} - {Set} - {Product} - {Finish}";

    public string ToString(string spacer) => $"{Name}{spacer}{Set}{spacer}{Product}{spacer}{Finish}";

    public string ToNamelessString() => $"{Set} - {Product.Replace("_", " ")} - {Finish}";

    public string ToNamelessString(string spacer) => $"{Set}{spacer}{Product.Replace("_", " ")}{spacer}{Finish}";

    public string ToJson(JsonSerializerOptions? options = null)
    {
        options ??= new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = false
        };

        return JsonSerializer.Serialize(this, options);
    }
}
