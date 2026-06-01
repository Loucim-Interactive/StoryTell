namespace DialogueSystem.Scripts {
    public static class DialogueFormatter
    {
        public static string Format(string input, bool remove = false) {
            input = input.Replace("<danger>", !remove ? "<color=#FF4C4C><b>" : ""); 
            input = input.Replace("</danger>", !remove ? "</b></color>" : "");

            input = input.Replace("<emphasis>", !remove ? "<color=#FFD700>" : "");
            input = input.Replace("</emphasis>", !remove ? "</color>" : "");

            input = input.Replace("<whisper>", !remove ? "<color=#AAAAAA><size=80%><i>" : "");
            input = input.Replace("</whisper>", !remove ? "</i></size></color>" : "");

            return input;
        }
    }
}