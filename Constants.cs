using Microsoft.Xna.Framework;

namespace ichortower.SecretWoodsSnorlax
{
    internal class Constants
    {
        public static string id_Mod = "ichortower.SecretWoodsSnorlax";
        public static string id_Flute = $"{id_Mod}_StrangeFlute";
        public static string id_FluteCue = $"{id_Mod}_flutemelody";
        public static string id_FluteCueShort = $"{id_Mod}_fluteshort";
        public static string ct_Prefix = "SecretWoodsSnorlax_Hint";
        public static string mail_SnorlaxHints = "SecretWoodsSnorlax_HintsActive";
        public static string mail_SnorlaxMoved = "SecretWoodsSnorlax_Moved";
        public static Vector2 vec_BlockingPosition = new Vector2(1f, 6f);
        /* not actually constant; is changed if Lunna is installed */
        public static Vector2 vec_MovedPosition = new Vector2(3f, 4f);
        public static int msPerBeat = 432;
    }
}
