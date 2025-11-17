using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    private const string OutputFolder = "data";

    private static readonly Dictionary<string, string> TypeMap = new()
    {
        ["normal"]   = "Normal",
        ["fire"]     = "Feu",
        ["water"]    = "Eau",
        ["grass"]    = "Plante",
        ["electric"] = "Electrik",
        ["ice"]      = "Glace",
        ["rock"]     = "Roche",
        ["ground"]   = "Sol",
        ["steel"]    = "Acier",
        ["dragon"]   = "Dragon",
        ["flying"]   = "Vol",
        ["fighting"] = "Combat",
        ["poison"]   = "Poison",
        ["bug"]      = "Insecte",
        ["psychic"]  = "Psy",
        ["ghost"]    = "Spectre",
        ["dark"]     = "Tenebres",
        ["fairy"]    = "Fee"
    };

    static async Task Main()
    {
        Directory.CreateDirectory(OutputFolder);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        Console.WriteLine("Downloading Pokémon list...");
        using var http = new HttpClient();
        var indexJson = await http.GetStringAsync("https://pokeapi.co/api/v2/pokemon?limit=20000");
        var index = JsonSerializer.Deserialize<PokemonIndex>(indexJson, jsonOptions);

        if (index?.Results == null)
        {
            Console.WriteLine("Failed to load Pokémon index.");
            return;
        }

        var attacksDict = new Dictionary<string, AttackInfo>();
        var pokemonLines = new List<string> { "id,name,type,attack_ids" };
        int id = 1;

        foreach (var entry in index.Results)
        {
            Console.WriteLine($"Fetching {entry.Name}...");
            var pokemonJson = await http.GetStringAsync(entry.Url);
            var p = JsonSerializer.Deserialize<PokemonFull>(pokemonJson, jsonOptions);
            if (p == null || p.Types == null || p.Types.Count == 0) continue;

            string rawType = p.Types[0].Type.Name.ToLowerInvariant();
            string mappedType = TypeMap.TryGetValue(rawType, out var t) ? t : "Normal";

            var attackIds = new List<int>();

            if (p.Moves != null)
            {
                foreach (var moveEntry in p.Moves)
                {
                    string moveName = moveEntry.Move.Name;

                    // Simplifié: on ne va pas encore chercher les vrais stats des attaques
                    if (!attacksDict.ContainsKey(moveName))
                    {
                        attacksDict[moveName] = new AttackInfo
                        {
                            Name = moveName,
                            Type = "Normal", // TODO: améliorer plus tard
                            Power = 50       // TODO: améliorer plus tard
                        };
                    }

                    // ID stable simplifié: hash du nom
                    attackIds.Add(moveName.GetHashCode());
                }
            }

            pokemonLines.Add($"{id},{entry.Name},{mappedType},{string.Join("|", attackIds)}");
            id++;
        }

        File.WriteAllLines(Path.Combine(OutputFolder, "pokemon.csv"), pokemonLines);

        Console.WriteLine("Saving attacks...");
        var attackLines = new List<string> { "id,name,power,type" };
        int atkId = 1;

        foreach (var atk in attacksDict.Values)
        {
            attackLines.Add($"{atkId},{atk.Name},{atk.Power},{atk.Type}");
            atkId++;
        }

        File.WriteAllLines(Path.Combine(OutputFolder, "attacks.csv"), attackLines);

        Console.WriteLine("Done! All Pokémon downloaded.");
    }
}

// ---------- DTO classes ----------

public class PokemonIndex
{
    public List<PokemonRef> Results { get; set; }
}

public class PokemonRef
{
    public string Name { get; set; }
    public string Url  { get; set; }
}

public class PokemonFull
{
    public List<PokemonTypeSlot> Types { get; set; }
    public List<PokemonMoveSlot> Moves { get; set; }
}

public class PokemonTypeSlot
{
    public TypeName Type { get; set; }
}

public class PokemonMoveSlot
{
    public MoveName Move { get; set; }
}

public class TypeName
{
    public string Name { get; set; }
}

public class MoveName
{
    public string Name { get; set; }
}

public class AttackInfo
{
    public string Name { get; set; }
    public int    Power { get; set; }
    public string Type  { get; set; }
}
