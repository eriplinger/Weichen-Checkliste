# Weichen-Checkliste

## Zeichenkodierung beim Laden

CSV-Dateien mit Unicode-BOM werden entsprechend ihrer BOM gelesen. Ohne BOM
wird zunächst striktes UTF-8 geprüft; sind die Bytes kein gültiges UTF-8, wird
Windows-1252 verwendet (der übliche ANSI-Zeichensatz deutscher Windows-Systeme).
Eine ungültige Unicode-Datei mit BOM wird mit einer Fehlermeldung abgelehnt.
Die Anzeige verwendet anschließend Unicode. Es gibt keine Regex, Wortersetzung
oder Änderung der Quelldatei und keine zusätzliche Einstellung.

XLSX enthält bereits Unicode und wird weiterhin mit ClosedXML gelesen.
Bereits falsch gespeicherte Zeichen sind keine abweichende Dateikodierung und
werden nicht ersetzt. „ANSI“ ist kein weltweit einheitlicher Zeichensatz;
andere lokale Windows-Codepages und DOS-Codepages sind nicht eindeutig von
Windows-1252 unterscheidbar. Ebenso sind manche Bytefolgen ohne BOM sowohl
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
WeichenwartungPath = C:\ProgramData\Weichen\30_Weichenwartung
WeichenwartungSyncPath = \\server\freigabe\Weichenwartung
```

Der erste Pfad ist der lokale Speicherordner und wird beim Speichern angelegt.
Der zweite ist der vollständige Remote-Zielordner und muss bereits erreichbar
sein. Beide Pfade müssen absolute, getrennte Ordner sein und dürfen sich nicht
mit den bisherigen Befund-, Bilder- oder Arbeitsordnern überschneiden.
Bestehende Einstellungsdateien werden nicht automatisch ergänzt.

Alle 60 Sekunden sowie über „aktualisieren“ werden lokale Wartungs-Exceldateien
in den eigenen Remote-Ordner verschoben, unabhängig vom ausgewählten Bearbeiter
und vom bisherigen Inspektions-Remote. Bereits vorhandene Remote-Dateien bleiben
erhalten; bei gleichen Namen erhält die neue Datei einen nummerierten Namen.
Bei fehlender Verbindung bleiben die Dateien lokal bis zum nächsten Versuch.
Ein leerer `WeichenwartungSyncPath` deaktiviert nur die Wartungs-Synchronisation.
Der Wartungsstatus wird separat in der Statusleiste angezeigt; Fehlerdetails
stehen im Tooltip. „Befunde im Ordner“ öffnet im Wartungsmodus den Wartungsordner.

Attribution
<a href="https://www.flaticon.com/free-icons/train" title="train icons">Train icons created by Aranagraphics - Flaticon</a>
