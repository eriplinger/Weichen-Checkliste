# Weichen-Checkliste

## Zeichenkodierung beim Laden

CSV-Dateien mit Unicode-BOM werden entsprechend ihrer BOM gelesen. Ohne BOM
wird zunächst striktes UTF-8 geprüft; sind die Bytes kein gültiges UTF-8, wird
zwischen Windows-1252 (ANSI auf deutschen Windows-Systemen), IBM850 und IBM437
gewählt. Eine Sprachheuristik bewertet deutsche Umlaute, Steuerzeichen,
untypische Buchstaben und DOS-Rahmenzeichen. Bei schwacher Evidenz bleibt
Windows-1252 der Standard; IBM850 hat bei gleicher DOS-Bewertung Vorrang.
Eine ungültige Unicode-Datei mit BOM wird mit einer Fehlermeldung abgelehnt.
Die Anzeige verwendet anschließend Unicode. Es gibt keine Regex, Wortersetzung
oder Änderung der Quelldatei und keine zusätzliche Einstellung.

XLSX enthält bereits Unicode und wird weiterhin mit ClosedXML gelesen.
Bereits falsch gespeicherte Zeichen sind keine abweichende Dateikodierung und
werden nicht ersetzt. „ANSI“ ist kein weltweit einheitlicher Zeichensatz;
Die Unterscheidung zwischen Windows-1252 und DOS-Codepages ist eine Schätzung,
besonders bei kurzen Texten oder fremdsprachigen Namen. Andere lokale Windows-
Codepages werden nicht unterstützt. Ebenso sind manche Bytefolgen ohne BOM sowohl
gültiges UTF-8 als auch gültiges Windows-1252; in diesem Fall hat UTF-8 Vorrang.

## Startmodus und Verknüpfungen

In `C:\ProgramData\Weichen\settings.txt` bestimmt `Startmodus = Inspektion`
oder `Startmodus = Wartung` den Standard beim nächsten Start. Ohne Eintrag
startet die Anwendung im Inspektionsmodus. Dort bleibt der Bearbeiter leer;
Früh-, Spät- oder Nachtdienst wird anschließend ausgewählt. Im Wartungsmodus
ist „Weichenwartung“ vorausgewählt. Die Bearbeiter-Auswahl bleibt änderbar.

Ein Startparameter hat Vorrang vor der Settings-Datei. Für zwei Windows-
Verknüpfungen im Feld „Ziel“ den jeweiligen vollständigen EXE-Pfad verwenden:

```text
"C:\Pfad\Weichen-Checkliste.exe" --modus=inspektion
"C:\Pfad\Weichen-Checkliste.exe" --modus=wartung
```

Groß-/Kleinschreibung ist beim Modus egal. Ungültige oder mehrfach angegebene
Modusparameter führen zu einer Meldung und zum Inspektionsmodus ohne Bearbeiter.

Zum Testen in Visual Studio das Anwendungsprojekt als Startprojekt auswählen.
Unter **Projekt → Eigenschaften → Debuggen → Debugstartprofile-Benutzeroberfläche
öffnen** im Projektprofil bei **Befehlszeilenargumente** `--modus=wartung` oder
`--modus=inspektion` eintragen und mit F5 starten. In manchen Visual-Studio-
Versionen heißt das Feld „Anwendungsargumente“. Für den Settings-Standard das
Argumentfeld leeren und erneut starten.

## Weichenwartung

Bei Bearbeiter „Weichenwartung“ wird genau ein Excel-Befund mit dem Kommentar
„Wartung durchgeführt“ und Status „grün“ gespeichert. Ausgeblendete Kommentare
werden dabei nicht übernommen.

In der tatsächlich verwendeten Datei `C:\ProgramData\Weichen\settings.txt`
die folgenden Einträge ergänzen und die Anwendung neu starten:

```text
WeichenwartungArbeitsvorratPath = C:\ProgramData\Weichen\50_WeichenwartungAV
WeichenwartungRückmeldungsPath = C:\ProgramData\Weichen\60_WeichenwartungR
WeichenwartungSyncPath = \\server\freigabe\Weichenwartung
```

`WeichenwartungArbeitsvorratPath` ist der lokale Wartungs-Arbeitsvorrat;
„Laden“ öffnet diesen Ordner nur im Wartungsmodus.
`WeichenwartungRückmeldungsPath` ist der lokale Speicherordner für Wartungsbefunde
und wird beim Speichern angelegt. `WeichenwartungSyncPath` ist jetzt der
Remote-Basisordner mit den Unterordnern `50_WeichenwartungAV` und
`60_WeichenwartungR`. Beide Remote-Unterordner müssen bereits existieren.
Alle Pfade müssen absolut sein und von den Inspektionsordnern getrennt bleiben.

## Synchronisation (Inspektion und Wartung)

Beim Start der WPF-Anwendung, dann alle 60 Sekunden und über **aktualisieren**
werden **alle fünf Datenflüsse** bearbeitet, unabhängig von der Bearbeiterauswahl:

| Daten | Remote | Lokal | Richtung |
| --- | --- | --- | --- |
| Inspektions-Arbeitsvorrat | `SyncPath\\10_Arbeitsvorbereitung` | `ArbeitsvorratPath` | Remote -> Lokal |
| Inspektionsbefunde | `SyncPath\\20_Arbeitsnachbereitung` | `RückmeldungsPath` | Lokal -> Remote |
| Bilder | `SyncPath\\40_Bilder` | `BilderPath` | Lokal -> Remote |
| Wartungs-Arbeitsvorrat | `WeichenwartungSyncPath\\50_WeichenwartungAV` | `WeichenwartungArbeitsvorratPath` | Remote -> Lokal |
| Wartungsbefunde | `WeichenwartungSyncPath\\60_WeichenwartungR` | `WeichenwartungRückmeldungsPath` | Lokal -> Remote |

Die Arbeitsvorräte werden gespiegelt: Die Remote-Dateien sind führend, fehlende
Dateien werden lokal entfernt, und geänderte Dateien werden ersetzt. Bei fehlendem
Remote-Zugriff bleibt der lokale Bestand erhalten. Änderungen werden über
SHA-256-Inhaltsvergleich festgestellt.

Rückmeldungen und Bilder werden zunächst in eine temporäre Datei auf dem
Remote-Share kopiert, anhand ihrer Prüfsumme kontrolliert und anschließend
unter dem **unveränderten Dateinamen** veröffentlicht. Gleichnamige Dateien
werden durch den zuletzt **abgeschlossenen Upload** ersetzt. Erst nach der
Veröffentlichung wird die lokale Quelldatei gelöscht. Das lokale Stammverzeichnis
bleibt bestehen. Bei Fehlern bleibt die noch nicht erfolgreich übertragene lokale
Datei erhalten. Ein Absturz nach dem Remote-Upload, aber vor der lokalen
Löschung kann zur erneuten Übertragung derselben Datei führen; das ist für
den vorgegebenen Nachweis-Usecase akzeptiert.

Alle Datenflüsse haben unabhängige Fehlerbehandlung. Statusleiste und Tooltip
zeigen unvollständige Synchronisation an. Eine laufende Synchronisation wird
nicht parallel gestartet. Die Synchronisation läuft nur, solange die
WPF-Anwendung geöffnet ist.

Alle Remote-Unterverzeichnisse müssen bereits vorhanden und mit den
notwendigen Rechten erreichbar sein. Die Pfade aus `settings.txt` müssen
absolut sein; Quell- und Zielverzeichnisse dürfen sich nicht überlappen.
Bei gemappten Netzlaufwerken (z. B. U:) muss das Mapping in der
Windows-Anmeldesitzung der Anwendung verfügbar sein.

**Installations- und Produktivkonfiguration:** Die MSI-Vorlagendatei wird
nicht als produktive `settings.txt` verwendet. Die Anwendung liest weiterhin
`C:\ProgramData\Weichen\settings.txt`. Diese Datei darf bei einem Update
nicht überschrieben werden. Bestehende Dateien werden nicht automatisch
migriert; insbesondere müssen die Wartungsschlüssel aus der obenstehenden
Tabelle vorhanden sein.

### Lokale Integrationstests

Auf einem Rechner mit installiertem .NET 8 SDK:

```powershell
dotnet run --project SyncTests/SyncTests.csproj
```

Diese Tests decken Spiegelung mit zwei Clients, Aktualisierung, Löschungen,
Offlineerhalt und den Upload gleichnamiger Befunde ab. Zusätzlich sind
manuelle Tests mit echtem Netzlaufwerk und geöffneten Excel-Dateien erforderlich.
Die Tests wurden noch nicht in einer Windows-/SMB-Umgebung ausgeführt.

Attribution
<a href="https://www.flaticon.com/free-icons/train" title="train icons">Train icons created by Aranagraphics - Flaticon</a>
