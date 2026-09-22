using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    public enum SkillBranch
    {
        Fuerza,
        Magia,
        Supervivencia,
        Movilidad,
        Caos
    }

    public enum SkillStatus
    {
        Locked,
        Available,
        Unlocked,
        Maxed
    }

    [Serializable]
    public sealed class SkillDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        public SkillBranch Branch;
        public int Tier;
        public int Cost;
        public int RequiredPlayerLevel;
        public int MaxLevel = 1;
        public string IconId;
        public string[] Prerequisites;
        public float Value;
        public float SecondaryValue;
        public float Cooldown;
        public float Range;
        public float Radius;
        public float Duration;
        public int RequiredBranchInvestment;
        public string EffectTag;

        public SkillDefinition(string id, string name, string description, SkillBranch branch,
            int tier, int cost, int requiredLevel, string effectTag, float value = 0f,
            float secondaryValue = 0f, params string[] prerequisites)
            : this(id, name, description, branch, tier, cost, requiredLevel, effectTag,
                value, secondaryValue, 0f, 0f, 0f, 0f, 0, prerequisites)
        {
        }

        public SkillDefinition(string id, string name, string description, SkillBranch branch,
            int tier, int cost, int requiredLevel, string effectTag, float value,
            float secondaryValue, float cooldown, float range, float radius, float duration,
            int requiredBranchInvestment, params string[] prerequisites)
        {
            Id = id;
            Name = name;
            Description = description;
            Branch = branch;
            Tier = tier;
            Cost = cost;
            RequiredPlayerLevel = requiredLevel;
            EffectTag = effectTag;
            IconId = effectTag;
            Value = value;
            SecondaryValue = secondaryValue;
            Cooldown = Mathf.Max(0f, cooldown);
            Range = Mathf.Max(0f, range);
            Radius = Mathf.Max(0f, radius);
            Duration = Mathf.Max(0f, duration);
            RequiredBranchInvestment = Mathf.Max(0, requiredBranchInvestment);
            Prerequisites = prerequisites ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// Runtime catalog for the skill tree. It is immutable after construction, so a
    /// scene can share one definition set without creating one ScriptableObject per node.
    /// </summary>
    public static class SkillTreeCatalog
    {
        private static readonly SkillDefinition[] definitions = Build();
        private static readonly Dictionary<string, SkillDefinition> byId = BuildIndex();

        public static IReadOnlyList<SkillDefinition> All => definitions;
        public static IReadOnlyList<SkillBranch> Branches { get; } = new[]
        {
            SkillBranch.Fuerza, SkillBranch.Magia, SkillBranch.Supervivencia,
            SkillBranch.Movilidad, SkillBranch.Caos
        };

        public static SkillDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            byId.TryGetValue(id, out SkillDefinition value);
            return value;
        }

        private static Dictionary<string, SkillDefinition> BuildIndex()
        {
            var result = new Dictionary<string, SkillDefinition>(StringComparer.Ordinal);
            foreach (SkillDefinition definition in definitions) result[definition.Id] = definition;
            return result;
        }

        private static SkillDefinition[] Build()
        {
            var result = new List<SkillDefinition>(52);

            // Fuerza: the physical chain also drives the sword and execution fantasy.
            Add(result, "force_heavy_hit", "Golpe pesado", "Los ataques cargados infligen un 150% de daño.", SkillBranch.Fuerza, 1, 1, 1, "HeavyDamage", 0.50f, 0f);
            Add(result, "force_bleeding_edge", "Filo sangrante", "Los impactos aplican una hemorragia breve.", SkillBranch.Fuerza, 2, 1, 2, "Bleed", 2.5f, 1f, "force_heavy_hit");
            Add(result, "force_armor_break", "Rompearmaduras", "Los golpes fuertes debilitan al objetivo y amplifican el siguiente impacto.", SkillBranch.Fuerza, 3, 2, 4, "ArmorBreak", 0.25f, 3f, "force_bleeding_edge");
            Add(result, "force_shockwave", "Onda de choque", "Los golpes cargados alcanzan y empujan enemigos cercanos.", SkillBranch.Fuerza, 4, 2, 6, "Shockwave", 0.45f, 1.4f, "force_armor_break");
            Add(result, "force_execution", "Ejecución", "Los enemigos por debajo del 20% de vida reciben daño de remate.", SkillBranch.Fuerza, 5, 3, 8, "Execution", 1.5f, 0.20f, "force_shockwave");
            Add(result, "force_brutal_combo", "Combo brutal", "Los golpes seguidos ganan daño durante tres segundos.", SkillBranch.Fuerza, 5, 2, 8, "BrutalCombo", 0.12f, 3f, "force_shockwave");
            Add(result, "force_earthquake", "Terremoto", "Un golpe cargado crea una onda expansiva que daña y empuja.", SkillBranch.Fuerza, 6, 3, 10, "Earthquake", 0.8f, 2.6f, "force_execution");
            Add(result, "force_carnage", "Carnicería", "Las bajas reducen el enfriamiento del dash y de los golpes fuertes.", SkillBranch.Fuerza, 6, 3, 10, "Carnage", 0.18f, 0f, "force_brutal_combo");
            Add(result, "force_monster_strength", "Fuerza monstruosa", "Los golpes cargados lanzan a los enemigos con violencia.", SkillBranch.Fuerza, 7, 4, 11, "MonsterStrength", 1.7f, 0.35f, "force_earthquake");
            Add(result, "force_titan_wrath", "Ira del Titán", "Los golpes atraviesan grupos y desatan impactos secundarios.", SkillBranch.Fuerza, 8, 5, 12, "TitanWrath", 0.65f, 2.2f, "force_execution", "force_earthquake", "force_monster_strength", "force_carnage");

            // Magia: the four elemental paths share Spark, then grow into distinct chains.
            Add(result, "magic_spark", "Chispa arcana", "Desbloquea el canal mágico básico.", SkillBranch.Magia, 1, 1, 1, "MagicPower", 0.10f, 0f);
            Add(result, "magic_fire", "Bola de fuego", "Los golpes cargados queman al objetivo durante varios segundos.", SkillBranch.Magia, 2, 1, 2, "Burn", 2f, 1.2f, "magic_spark");
            Add(result, "magic_fire_explosion", "Explosión", "Los objetivos en llamas explotan al caer.", SkillBranch.Magia, 3, 2, 4, "FireExplosion", 0.5f, 1.5f, "magic_fire");
            Add(result, "magic_meteor", "Meteoro", "Los impactos cargados invocan un meteoro periódicamente.", SkillBranch.Magia, 4, 2, 6, "Meteor", 1.5f, 2.1f, "magic_fire_explosion");
            Add(result, "magic_cataclysm", "Cataclismo", "Las bajas ígneas provocan explosiones mayores.", SkillBranch.Magia, 6, 4, 10, "Cataclysm", 1.1f, 2.5f, "magic_meteor");
            Add(result, "magic_lightning", "Cadena eléctrica", "Una descarga salta entre hasta tres enemigos cercanos.", SkillBranch.Magia, 3, 2, 4, "ChainLightning", 0.35f, 3f, "magic_spark");
            Add(result, "magic_thunderstorm", "Tormenta", "La electricidad alcanza automáticamente a un enemigo cercano.", SkillBranch.Magia, 4, 2, 6, "Thunderstorm", 0.45f, 3f, "magic_lightning");
            Add(result, "magic_overload", "Sobrecarga", "Los enemigos electrificados explotan al morir.", SkillBranch.Magia, 5, 3, 8, "Overload", 0.65f, 1.7f, "magic_thunderstorm");
            Add(result, "magic_thunder_god", "Dios del trueno", "Los rayos automáticos saltan más veces y golpean con más fuerza.", SkillBranch.Magia, 7, 4, 11, "ThunderGod", 0.7f, 5f, "magic_overload");
            Add(result, "magic_ice", "Hielo inmovilizador", "Los impactos ralentizan al objetivo.", SkillBranch.Magia, 3, 2, 4, "Freeze", 0.35f, 1.5f, "magic_spark");
            Add(result, "magic_ice_shatter", "Fragmentación", "Golpear a un enemigo congelado lo fragmenta y daña alrededor.", SkillBranch.Magia, 4, 2, 6, "IceShatter", 0.7f, 1.4f, "magic_ice");
            Add(result, "magic_frost_nova", "Nova de escarcha", "Los golpes cargados congelan a todos los enemigos cercanos.", SkillBranch.Magia, 5, 3, 8, "FrostNova", 0.4f, 2f, "magic_ice_shatter");
            Add(result, "magic_glacier", "Glaciar", "Los enemigos congelados explotan en una reacción de hielo.", SkillBranch.Magia, 7, 4, 11, "Glacier", 0.9f, 2.2f, "magic_frost_nova");
            Add(result, "magic_void", "Vacío", "Absorbe vida y concentra enemigos con gravedad.", SkillBranch.Magia, 4, 3, 7, "Void", 0.22f, 0f, "magic_lightning", "magic_ice");
            Add(result, "magic_void_gravity", "Gravedad", "Los ataques cargados atraen a los enemigos próximos.", SkillBranch.Magia, 5, 3, 8, "VoidGravity", 0.45f, 2.1f, "magic_void");
            Add(result, "magic_black_hole", "Agujero negro", "Los impactos cargados atraen y dañan a grupos.", SkillBranch.Magia, 6, 4, 10, "BlackHole", 0.8f, 2.7f, "magic_void_gravity");
            Add(result, "magic_singularity", "Singularidad", "Una concentración de vacío termina en una explosión.", SkillBranch.Magia, 7, 4, 11, "Singularity", 1.25f, 3.2f, "magic_black_hole");
            Add(result, "magic_apotheosis", "Apoteosis", "La magia amplifica daño y reacciones de todos los elementos.", SkillBranch.Magia, 8, 5, 12, "Apotheosis", 0.35f, 0f, "magic_void", "magic_cataclysm", "magic_thunder_god", "magic_glacier", "magic_singularity");

            // Supervivencia: defensive progression works with the existing health system.
            Add(result, "survival_regen", "Regeneración", "Recupera vida de forma lenta y constante.", SkillBranch.Supervivencia, 1, 1, 1, "Regen", 1f, 0f);
            Add(result, "survival_lifesteal", "Sed de sangre", "Una parte del daño infligido vuelve como vida.", SkillBranch.Supervivencia, 2, 1, 2, "Lifesteal", 0.08f, 0f, "survival_regen");
            Add(result, "survival_second_chance", "Segunda oportunidad", "Evita una muerte y restaura una reserva de vida.", SkillBranch.Supervivencia, 3, 2, 4, "SecondChance", 0.35f, 0f, "survival_lifesteal");
            Add(result, "survival_devourer", "Devorador", "Las bajas restauran más vida y otorgan experiencia.", SkillBranch.Supervivencia, 4, 2, 6, "Devourer", 2f, 1f, "survival_second_chance");
            Add(result, "survival_immortal", "Inmortalidad", "Al morir, provoca una explosión, revive y obtiene invulnerabilidad.", SkillBranch.Supervivencia, 5, 3, 8, "Immortal", 0.2f, 0f, "survival_devourer");
            Add(result, "survival_god_of_war", "Dios de la guerra", "La regeneración y la resistencia crecen cuando hay enemigos cerca.", SkillBranch.Supervivencia, 8, 5, 12, "GodOfWar", 0.2f, 0f, "survival_immortal");

            // Movilidad: movement reads the values through SkillTreeManager.
            Add(result, "mobility_dash", "Paso ligero", "Aumenta la distancia y velocidad del dash.", SkillBranch.Movilidad, 1, 1, 1, "Dash", 0.16f, 1.15f);
            Add(result, "mobility_double_dash", "Doble dash", "Permite encadenar dos dashes.", SkillBranch.Movilidad, 2, 1, 2, "DoubleDash", 1f, 0f, "mobility_dash");
            Add(result, "mobility_spectral", "Paso espectral", "El dash atraviesa enemigos sin quedar atrapado.", SkillBranch.Movilidad, 3, 2, 4, "Spectral", 0.2f, 0f, "mobility_double_dash");
            Add(result, "mobility_offensive_dash", "Dash ofensivo", "El trayecto del dash golpea a los enemigos que atraviesa.", SkillBranch.Movilidad, 4, 2, 6, "OffensiveDash", 0.8f, 1.1f, "mobility_spectral");
            Add(result, "mobility_shadow_burst", "Sombra explosiva", "El dash deja una sombra que explota con retraso.", SkillBranch.Movilidad, 5, 3, 8, "ShadowBurst", 0.75f, 1.3f, "mobility_offensive_dash");
            Add(result, "mobility_ghost_step", "Paso fantasma", "El dash concede una breve fase y reduce su enfriamiento.", SkillBranch.Movilidad, 6, 3, 9, "GhostStep", 0.22f, 0f, "mobility_shadow_burst");
            Add(result, "mobility_offensive_teleport", "Teletransporte ofensivo", "El dash puede aparecer junto a un enemigo cercano.", SkillBranch.Movilidad, 7, 4, 11, "OffensiveTeleport", 1f, 1.4f, "mobility_ghost_step");
            Add(result, "mobility_omnipresence", "Omnipresencia", "Los golpes pueden teletransportarte al siguiente enemigo.", SkillBranch.Movilidad, 8, 5, 12, "Omnipresence", 0.35f, 0f, "mobility_offensive_teleport");

            // Caos: kill chains are guarded by the event bus depth limit.
            Add(result, "chaos_death_aura", "Aura de muerte", "Los enemigos cercanos reciben daño automáticamente.", SkillBranch.Caos, 1, 1, 1, "DeathAura", 0.04f, 1.25f);
            Add(result, "chaos_corpse_explosion", "Explosión de cadáver", "Una baja daña a todos los enemigos próximos.", SkillBranch.Caos, 2, 1, 2, "CorpseExplosion", 0.55f, 1.5f, "chaos_death_aura");
            Add(result, "chaos_chain_reaction", "Reacción en cadena", "Las muertes por explosión pueden iniciar otra explosión limitada.", SkillBranch.Caos, 3, 2, 4, "ChainReaction", 0.35f, 0f, "chaos_corpse_explosion");
            Add(result, "chaos_death_mark", "Marca de muerte", "Los objetivos marcados reciben daño extra.", SkillBranch.Caos, 4, 2, 6, "DeathMark", 0.2f, 0f, "chaos_chain_reaction");
            Add(result, "chaos_domination", "Dominación", "Los enemigos débiles pueden ser aturdidos brevemente al recibir daño.", SkillBranch.Caos, 5, 3, 8, "Domination", 0.25f, 0f, "chaos_death_mark");
            Add(result, "chaos_divine_ascension", "Ascensión divina", "Combina las reacciones y potencia al jugador cerca de grupos.", SkillBranch.Caos, 8, 5, 12, "DivineAscension", 0.5f, 0f, "chaos_domination");

            Configure(result, "force_bleeding_edge", duration: 2.5f);
            Configure(result, "force_armor_break", duration: 3f);
            Configure(result, "force_shockwave", radius: 1.4f);
            Configure(result, "force_brutal_combo", duration: 3f);
            Configure(result, "force_earthquake", cooldown: 1.2f, radius: 2.6f);
            Configure(result, "force_monster_strength", duration: .35f);
            Configure(result, "force_titan_wrath", cooldown: .25f, radius: 2.2f);
            Configure(result, "magic_fire", duration: 3f);
            Configure(result, "magic_fire_explosion", radius: 1.5f);
            Configure(result, "magic_meteor", cooldown: 5f, radius: 2.1f);
            Configure(result, "magic_cataclysm", radius: 2.5f);
            Configure(result, "magic_lightning", range: 2.6f);
            Configure(result, "magic_thunderstorm", cooldown: 4f, range: 3f);
            Configure(result, "magic_overload", radius: 1.7f);
            Configure(result, "magic_thunder_god", cooldown: 2.3f, range: 4f);
            Configure(result, "magic_ice", duration: 1.5f);
            Configure(result, "magic_ice_shatter", radius: 1.4f);
            Configure(result, "magic_frost_nova", radius: 2f, duration: 1.5f);
            Configure(result, "magic_glacier", radius: 2.2f, duration: 1.4f);
            Configure(result, "magic_void_gravity", radius: 2.1f);
            Configure(result, "magic_black_hole", radius: 2.7f);
            Configure(result, "magic_singularity", cooldown: .4f, radius: 3.2f);
            Configure(result, "survival_regen", cooldown: 4f);
            Configure(result, "survival_second_chance", cooldown: 45f);
            Configure(result, "survival_immortal", cooldown: 90f);
            Configure(result, "mobility_offensive_teleport", cooldown: 1.1f, range: 4f);
            Configure(result, "mobility_omnipresence", cooldown: .4f, range: 3.25f);
            Configure(result, "chaos_death_aura", cooldown: 1.5f, radius: 1.25f);
            Configure(result, "chaos_corpse_explosion", radius: 1.5f);
            Configure(result, "chaos_death_mark", duration: 5f, radius: 2f);
            Configure(result, "chaos_divine_ascension", cooldown: .85f, radius: 1.7f, requiredBranchInvestment: 5);

            return result.ToArray();
        }

        private static void Add(List<SkillDefinition> list, string id, string name, string description,
            SkillBranch branch, int tier, int cost, int requiredLevel, string effectTag,
            float value, float secondaryValue, params string[] prerequisites)
        {
            list.Add(new SkillDefinition(id, name, description, branch, tier, cost,
                requiredLevel, effectTag, value, secondaryValue, prerequisites));
        }

        private static void Configure(List<SkillDefinition> definitions, string id, float cooldown = 0f,
            float range = 0f, float radius = 0f, float duration = 0f, int requiredBranchInvestment = 0)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                SkillDefinition definition = definitions[i];
                if (definition.Id != id) continue;
                definition.Cooldown = Mathf.Max(0f, cooldown);
                definition.Range = Mathf.Max(0f, range);
                definition.Radius = Mathf.Max(0f, radius);
                definition.Duration = Mathf.Max(0f, duration);
                definition.RequiredBranchInvestment = Mathf.Max(0, requiredBranchInvestment);
                return;
            }
        }
    }
}
