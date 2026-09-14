using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public static class VillageResidents
    {
        public static bool ProjectComplete(ValleyData data, string id) => id switch
        {
            "village:elder" => data.maraPantryStocked,
            "village:blacksmith" => data.nicoWorkshopRepaired,
            "village:farmer" => data.daliaGardenRestored,
            "village:merchant" => data.roloMarketOpened,
            "village:guard" => data.guardPostBuilt,
            _ => false
        };

        public static string Name(string id) => id switch
        {
            "village:elder" => "Mara",
            "village:blacksmith" => "Nico",
            "village:farmer" => "Dalia",
            "village:merchant" => "Rolo",
            "village:guard" => "Iria",
            _ => "Vecino"
        };

        public static int Period(float hour) => hour >= 5 && hour < 10 ? 0 : hour >= 10 && hour < 18 ? 1 : 2;

        public static Vector3 Offset(string id, float hour, bool restored)
        {
            // Keep every resident within reach of the original interaction anchor.
            int period = Period(hour);
            float side = id == "village:elder" || id == "village:merchant" ? -1f : 1f;
            if (period == 0) return new Vector3(side * .22f, .1f);
            if (period == 1) return new Vector3(side * (restored ? .3f : .1f), restored ? .3f : .05f);
            return new Vector3(-side * .18f, 0);
        }

        public static string Activity(ValleyData data, string id, float hour)
        {
            int period = Period(hour);
            if (period == 2) return id == "village:guard" ? "vigila el camino" : "descansa junto a su puesto";
            if (period == 0) return id switch
            {
                "village:elder" => "revisa la despensa",
                "village:blacksmith" => "revisa sus herramientas",
                "village:farmer" => "revisa las provisiones",
                "village:merchant" => "cuenta suministros",
                _ => "inspecciona la entrada"
            };
            if (!ProjectComplete(data, id)) return id switch
            {
                "village:elder" => "ordena estantes vacios",
                "village:blacksmith" => "separa los herrajes rotos",
                "village:farmer" => "ordena la cocina en ruinas",
                "village:merchant" => "prepara el puesto cerrado",
                _ => "vigila sin relevo"
            };
            return id switch
            {
                "village:elder" => "organiza las provisiones",
                "village:blacksmith" => "trabaja los herrajes",
                "village:farmer" => "prepara raciones",
                "village:merchant" => "prepara los trueques",
                _ => "revisa las defensas"
            };
        }

        public static string Dialogue(ValleyData data, string id)
        {
            if (!data.note && !data.maraPantryStocked && id == "village:elder") return "Tu abuelo guardo una nota en el cofre junto a casa. Empieza por ahi; el pueblo puede esperar a que encuentres tu sitio.";
            if (!ProjectComplete(data, id)) return id switch
            {
                "village:elder" => data.camp ? "Con dos raciones abriremos la despensa. Me importa que manana tambien haya alguien en esta mesa." : "Primero comida, fogata y una cama para la noche. El humo nos dira que has decidido quedarte.",
                "village:blacksmith" => data.camp ? "Trae 12 madera y 8 piedra. Quitaremos las tablas de la puerta y podre preparar hierro para tus salidas." : "Con las vigas asi no puedo trabajar. Sobrevive la primera noche y levantaremos el taller juntos.",
                "village:farmer" => data.note ? "Con 2 frutas y 4 madera recuperaremos la cocina. Hay ingredientes en los suministros del pueblo y en mi puesto." : "Lee la nota junto al refugio y luego hablamos de recuperar la cocina.",
                "village:merchant" => data.nicoWorkshopRepaired && data.daliaGardenRestored ? "El taller y la cocina funcionan. Con 20 oro y 8 madera puedo abrir el puesto." : "Abrire cuando Nico tenga taller y Dalia tenga cocina. Quiero ofrecer cosas que el pueblo pueda reponer.",
                "village:guard" => data.sealStone || data.guardian ? "Ya conocemos la amenaza. Con 6 hierro y 2 raciones podemos sostener el puesto." : "Recupera un sello o libera el bosque; entonces sabremos donde hacen falta las defensas.",
                _ => "Raizclara sigue en pie."
            };
            if (data.restored) return id switch
            {
                "village:elder" => "Hoy he puesto una silla mas. No es para una visita: es la tuya.",
                "village:blacksmith" => "La puerta abre sin atascarse y vuelven los encargos. Tus proximas herramientas saldran de un taller vivo.",
                "village:farmer" => "El fuego vuelve a encenderse cada manana. Tenemos provisiones para recibir a quienes vuelvan al valle.",
                "village:merchant" => "Por primera vez preparo pedidos para manana. Eso tambien es recuperar un pueblo.",
                _ => "La reliquia esta en casa. Sigo de guardia, pero esta noche hay luces detras de mi."
            };
            return id switch
            {
                "village:elder" => data.nicoWorkshopRepaired ? "He vuelto a oir herramientas en casa de Nico. Lleva comida antes de salir; aqui te esperamos." : "La despensa esta lista. Nico sigue mirando esa puerta cerrada; quizas puedas ayudarlo.",
                "village:blacksmith" => !data.sealStone ? "He encontrado herrajes aprovechables. Dejame materiales en el banco y te preparare hierro para el equipo de la cantera." : !data.guardian ? "Ese sello estaba protegiendo algo. Lleva buen equipo al bosque; te guardo un encargo diario de hierro." : "El bosque ya respira. Antes del santuario, recoge tus herrajes en el banco y prepara el equipo.",
                "village:farmer" => data.guardian ? "El camino al santuario esta abierto. Lleva raciones para curarte si te hieren." : "La cocina vuelve a funcionar. Guarda provisiones para recuperarte durante el viaje.",
                "village:merchant" => data.portal ? "Has abierto una ruta que nadie conocia. Antes de cruzarla, cambia madera por una racion si vas corto." : "Nico aporta herramientas y Dalia alimentos. Ahora si puedo cambiarte 5 madera por una racion.",
                "village:guard" => data.boss ? "La amenaza ha caido. Regresa con la reliquia; mantendre la entrada despejada." : data.guardian ? "Has liberado el bosque. Si cruzas el portal, mira donde va a caer el enemigo antes de acercarte." : "El puesto ya aguanta. Ante el guardian, deja espacio para apartarte antes de devolver el golpe.",
                _ => "Gracias por ayudar a Raizclara."
            };
        }
    }
}
