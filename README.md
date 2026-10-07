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

Alle 60 Sekunden sowie über „aktualisieren“ wird der Wartungs-Arbeitsvorrat aus
`WeichenwartungSyncPath\50_WeichenwartungAV` lokal kopiert (mit Aktualisierung
vorhandener Dateien). Lokale Wartungs-Rückmeldungen werden separat nach
`WeichenwartungSyncPath\60_WeichenwartungR` verschoben. Vorhandene Remote-Befunde
bleiben erhalten; gleiche Namen erhalten einen nummerierten Zusatz. Scheitert
eine Richtung, wird die andere trotzdem versucht. Offline bleiben lokale
Dateien erhalten. Ein leerer `WeichenwartungSyncPath` deaktiviert die Wartungs-
Synchronisation. Sie läuft nur bei ausgewähltem Bearbeiter „Weichenwartung“.

Befundzähler und „Befunde im Ordner“ verwenden nur im Wartungsmodus den Wartungs-
Rückmeldungsordner. Inspektionspfade und Inspektions-Synchronisation bleiben
unverändert. Der Wartungsstatus steht separat in der Statusleiste; Fehlerdetails
stehen im Tooltip.

Bestehende `settings.txt` werden nicht automatisch geändert. Den bisherigen
`WeichenwartungPath` durch die beiden neuen lokalen Pfade ersetzen und die
Anwendung neu starten. Bereits gespeicherte Wartungsbefunde bei geschlossener
Anwendung in den neuen lokalen Rückmeldungsordner verschieben, damit sie weiter
synchronisiert werden. Auch vorhandene Remote-Wartungsbefunde gehören in den
neuen Remote-Unterordner `60_WeichenwartungR`.

Attribution
<a href="https://www.flaticon.com/free-icons/train" title="train icons">Train icons created by Aranagraphics - Flaticon</a>
