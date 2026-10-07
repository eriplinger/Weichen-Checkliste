# Weichen-Checkliste

## CSV- und Excel-Import

In `C:\ProgramData\Weichen\settings.txt` kann der CSV-Quellzeichensatz mit
`CsvEncoding = Auto` eingestellt werden. Auto erkennt UTF-8 und verwendet bei
nicht gültigem UTF-8 Windows-1252. Unicode-Dateien mit UTF-8-, UTF-16- oder
UTF-32-BOM werden anhand ihrer BOM erkannt. Die Kodierung wird auf die komplette
Datei angewendet; es gibt keine wortbezogenen Zeichenersetzungen.

Bei eindeutig bekanntem Quellzeichensatz sind auch `CsvEncoding = UTF8`,
`CsvEncoding = Windows1252` und `CsvEncoding = IBM850` möglich. Danach die
Anwendung neu starten. IBM850 ist für alte DOS-Exporte gedacht, bei denen z.B.
„ß“ beim Lesen als Windows-1252 zu „á“ wird. Windows-1252 und DOS-Kodierungen
lassen sich ohne Metadaten nicht zuverlässig unterscheiden; Auto errät keine
DOS-Kodierung. Die Quelldateien bleiben unverändert.

CSV-Anführungszeichen, Semikolons in zitierten Feldern und mehrzeilige Felder
werden unterstützt. XLSX speichert bereits Unicode und verwendet die CSV-
Einstellung nicht. Bei CSV-Feldern und Excel-Zelltexten werden bereits falsch
als Windows-1252 interpretierte UTF-8-Daten nur dann zurückgewandelt, wenn dies
für den ganzen Text verlustfrei möglich ist. Bereits verlorene Zeichen (etwa
„�“) lassen sich nicht rekonstruieren: Die Datei wird mit einer Fehlermeldung
abgelehnt, und die bisherige Liste bleibt erhalten. Ein gültiges „á“ in einer
XLSX-Datei kann nicht automatisch von einem früher beschädigten „ß“ unterschieden
werden. Leere Excel-Zellen behalten ihre Spaltenposition.

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
