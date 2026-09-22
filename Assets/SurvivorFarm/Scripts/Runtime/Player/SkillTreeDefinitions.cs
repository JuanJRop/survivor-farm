using System;
using System.Collections.Generic;

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
        public string EffectTag;

        public SkillDefinition(string id, string name, string description, SkillBranch branch,
            int tier, int cost, int requiredLevel, string effectTag, float value = 0f,
            float secondaryValue = 0f, params string[] prerequisites)
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
            var result = new List<SkillDefinition>(35);

            // Fuerza: the physical chain also drives the sword and execution fantasy.
            Add(result, "force_heavy_hit", "Golpe pesado", "Los ataques cargados hacen más daño.", SkillBranch.Fuerza, 1, 1, 1, "HeavyDamage", 0.20f, 0f);
            Add(result, "force_bleeding_edge", "Filo sangrante", "Los impactos de espada aplican una hemorragia breve.", SkillBranch.Fuerza, 2, 1, 2, "Bleed", 2f, 0f, "force_heavy_hit");
            Add(result, "force_armor_break", "Rompearmaduras", "Los golpes pesados reducen la resistencia del enemigo.", SkillBranch.Fuerza, 3, 2, 4, "ArmorBreak", 0.18f, 0f, "force_bleeding_edge");
            Add(result, "force_shockwave", "Onda de choque", "Un golpe cargado alcanza a los enemigos cercanos.", SkillBranch.Fuerza, 4, 2, 6, "Shockwave", 0.45f, 1.4f, "force_armor_break");
            Add(result, "force_execution", "Ejecución", "Los enemigos debilitados reciben una bonificación de remate.", SkillBranch.Fuerza, 5, 3, 8, "Execution", 0.25f, 0f, "force_shockwave");
            Add(result, "force_titan_wrath", "Ira del Titán", "El ataque final combina daño, área y retroceso.", SkillBranch.Fuerza, 8, 5, 12, "TitanWrath", 0.65f, 2.2f, "force_execution");

            // Magia: all nodes are data first; effects can be expanded without changing UI.
            Add(result, "magic_spark", "Chispa arcana", "Desbloquea el canal mágico básico.", SkillBranch.Magia, 1, 1, 1, "MagicPower", 0.10f, 0f);
            Add(result, "magic_fire", "Rama de fuego", "Los ataques cargados dejan una brasa.", SkillBranch.Magia, 2, 1, 2, "Burn", 2f, 1.2f, "magic_spark");
            Add(result, "magic_lightning", "Rayo encadenado", "Un crítico puede saltar a otro enemigo.", SkillBranch.Magia, 3, 2, 4, "ChainLightning", 0.35f, 3f, "magic_fire");
            Add(result, "magic_ice", "Hielo inmovilizador", "Los impactos ralentizan al objetivo.", SkillBranch.Magia, 3, 2, 4, "Freeze", 0.35f, 1.5f, "magic_spark");
            Add(result, "magic_void", "Vacío", "Ignora parte de la defensa enemiga.", SkillBranch.Magia, 4, 3, 7, "Void", 0.22f, 0f, "magic_lightning", "magic_ice");
            Add(result, "magic_apotheosis", "Apoteosis", "La magia amplifica cada evento de combate.", SkillBranch.Magia, 8, 5, 12, "Apotheosis", 0.35f, 0f, "magic_void");

            // Supervivencia: defensive progression works with the existing health system.
            Add(result, "survival_regen", "Regeneración", "Recupera vida lentamente después de una victoria.", SkillBranch.Supervivencia, 1, 1, 1, "Regen", 1f, 0f);
            Add(result, "survival_lifesteal", "Sed de vida", "Una parte del daño vuelve como vida.", SkillBranch.Supervivencia, 2, 1, 2, "Lifesteal", 0.08f, 0f, "survival_regen");
            Add(result, "survival_second_chance", "Segunda oportunidad", "Evita una muerte y restaura una reserva de vida.", SkillBranch.Supervivencia, 3, 2, 4, "SecondChance", 0.35f, 0f, "survival_lifesteal");
            Add(result, "survival_devourer", "Devorador", "Las bajas restauran más vida y otorgan experiencia.", SkillBranch.Supervivencia, 4, 2, 6, "Devourer", 2f, 1f, "survival_second_chance");
            Add(result, "survival_immortal", "Inmortal", "Reduce el daño recibido cuando la vida es baja.", SkillBranch.Supervivencia, 5, 3, 8, "Immortal", 0.2f, 0f, "survival_devourer");
            Add(result, "survival_god_of_war", "Dios de la guerra", "La defensa y el robo de vida escalan juntos.", SkillBranch.Supervivencia, 8, 5, 12, "GodOfWar", 0.2f, 0f, "survival_immortal");

            // Movilidad: movement reads the values through SkillTreeManager.
            Add(result, "mobility_dash", "Paso ligero", "Aumenta la distancia y velocidad del dash.", SkillBranch.Movilidad, 1, 1, 1, "Dash", 0.16f, 1.15f);
            Add(result, "mobility_double_dash", "Doble dash", "Permite encadenar dos dashes.", SkillBranch.Movilidad, 2, 1, 2, "DoubleDash", 1f, 0f, "mobility_dash");
            Add(result, "mobility_spectral", "Paso espectral", "El dash atraviesa enemigos sin quedar atrapado.", SkillBranch.Movilidad, 3, 2, 4, "Spectral", 0.2f, 0f, "mobility_double_dash");
            Add(result, "mobility_offensive_dash", "Dash ofensivo", "El final del dash golpea a los enemigos cercanos.", SkillBranch.Movilidad, 4, 2, 6, "OffensiveDash", 0.8f, 1.1f, "mobility_spectral");
            Add(result, "mobility_ghost_step", "Paso fantasma", "Reduce el enfriamiento de movimiento durante el combate.", SkillBranch.Movilidad, 5, 3, 8, "GhostStep", 0.22f, 0f, "mobility_offensive_dash");
            Add(result, "mobility_omnipresence", "Omnipresencia", "La movilidad se convierte en una oportunidad ofensiva.", SkillBranch.Movilidad, 8, 5, 12, "Omnipresence", 0.35f, 0f, "mobility_ghost_step");

            // Caos: kill chains are guarded by the event bus depth limit.
            Add(result, "chaos_death_aura", "Aura de muerte", "Los enemigos cercanos reciben presión constante.", SkillBranch.Caos, 1, 1, 1, "DeathAura", 0.04f, 1.25f);
            Add(result, "chaos_corpse_explosion", "Explosión de cadáver", "Una baja hiere a los enemigos próximos.", SkillBranch.Caos, 2, 1, 2, "CorpseExplosion", 0.55f, 1.5f, "chaos_death_aura");
            Add(result, "chaos_chain_reaction", "Reacción en cadena", "Las explosiones pueden iniciar otra baja una vez.", SkillBranch.Caos, 3, 2, 4, "ChainReaction", 0.35f, 0f, "chaos_corpse_explosion");
            Add(result, "chaos_death_mark", "Marca de muerte", "Los objetivos marcados reciben daño extra.", SkillBranch.Caos, 4, 2, 6, "DeathMark", 0.2f, 0f, "chaos_chain_reaction");
            Add(result, "chaos_domination", "Dominación", "Los enemigos debilitados pierden agresividad.", SkillBranch.Caos, 5, 3, 8, "Domination", 0.25f, 0f, "chaos_death_mark");
            Add(result, "chaos_divine_ascension", "Ascensión divina", "Combina todas las reacciones de caos.", SkillBranch.Caos, 8, 5, 12, "DivineAscension", 0.5f, 0f, "chaos_domination");

            return result.ToArray();
        }

        private static void Add(List<SkillDefinition> list, string id, string name, string description,
            SkillBranch branch, int tier, int cost, int requiredLevel, string effectTag,
            float value, float secondaryValue, params string[] prerequisites)
        {
            list.Add(new SkillDefinition(id, name, description, branch, tier, cost,
                requiredLevel, effectTag, value, secondaryValue, prerequisites));
        }
    }
}
