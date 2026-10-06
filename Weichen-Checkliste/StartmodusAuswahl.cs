namespace Weichen_Checkliste
{
    public enum Startmodus
    {
        Inspektion,
        Wartung
    }

    public static class StartmodusAuswahl
    {
        public static Startmodus Bestimmen(string settingsWert, IEnumerable<string> argumente)
        {
            string? parameter = null;
            foreach (string argument in argumente)
            {
                if (argument.Equals("--modus", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Bitte --modus=inspektion oder --modus=wartung angeben.");
                if (!argument.StartsWith("--modus=", StringComparison.OrdinalIgnoreCase)) continue;
                if (parameter != null)
                    throw new ArgumentException("Den Startparameter --modus bitte nur einmal angeben.");
                parameter = argument.Substring("--modus=".Length);
            }

            string wert = (parameter ?? settingsWert).Trim();
            if (wert.Equals("Wartung", StringComparison.OrdinalIgnoreCase)) return Startmodus.Wartung;
            if (wert.Equals("Inspektion", StringComparison.OrdinalIgnoreCase)
                || (parameter == null && string.IsNullOrWhiteSpace(wert))) return Startmodus.Inspektion;
            throw new ArgumentException("Ungültiger Startmodus. Erlaubt sind Inspektion und Wartung.");
        }
    }
}
