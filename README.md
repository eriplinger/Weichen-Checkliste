# Weichen-Checkliste

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
