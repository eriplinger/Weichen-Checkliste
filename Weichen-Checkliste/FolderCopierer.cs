using System;
using System.IO;

public class FolderCopierer
{
    //public static void CopyFolder(string sourcePath, string destinationPath)
    //{
    //    try
    //    {
    //        // Sicherstellen, dass der Zielordner existiert
    //        if (!Directory.Exists(destinationPath))
    //        {
    //            Directory.CreateDirectory(destinationPath);
    //        }

    //        // Dateien im Quellordner kopieren
    //        foreach (string filePath in Directory.GetFiles(sourcePath))
    //        {
    //            string fileName = Path.GetFileName(filePath);
    //            string destFilePath = Path.Combine(destinationPath, fileName);
    //            File.Copy(filePath, destFilePath, true); // `true` überschreibt vorhandene Dateien
    //        }

    //        // Unterordner rekursiv kopieren
    //        foreach (string directoryPath in Directory.GetDirectories(sourcePath))
    //        {
    //            string directoryName = Path.GetFileName(directoryPath);
    //            string destDirectoryPath = Path.Combine(destinationPath, directoryName);
    //            CopyFolder(directoryPath, destDirectoryPath);
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Fehler beim Kopieren des Ordners: {ex.Message}");
    //    }
    //}

    /// <summary>
    /// Kopiert alle Dateien und Unterordner aus einem Quellordner in einen Zielordner.
    /// </summary>
    public static void CopyFolder(string sourcePath, string destinationPath)
    {
        try
        {
            // Sicherstellen, dass der Zielordner existiert
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }

            // Dateien im Quellordner kopieren
            foreach (string filePath in Directory.GetFiles(sourcePath))
            {
                string fileName = Path.GetFileName(filePath);
                string destFilePath = Path.Combine(destinationPath, fileName);
                File.Copy(filePath, destFilePath, true); // `true` überschreibt vorhandene Dateien
            }

            // Unterordner rekursiv kopieren
            foreach (string directoryPath in Directory.GetDirectories(sourcePath))
            {
                string directoryName = Path.GetFileName(directoryPath);
                string destDirectoryPath = Path.Combine(destinationPath, directoryName);
                CopyFolder(directoryPath, destDirectoryPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Kopieren des Ordners: {ex.Message}");
        }
    }

    /// <summary>
    /// Verschiebt alle Dateien und Unterordner aus einem Quellordner in einen Zielordner.
    /// </summary>
    public static void MoveFolder(string sourcePath, string destinationPath)
    {
        try
        {
            // Sicherstellen, dass der Zielordner existiert
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }

            // Dateien im Quellordner verschieben
            foreach (string filePath in Directory.GetFiles(sourcePath))
            {
                string fileName = Path.GetFileName(filePath);
                string destFilePath = Path.Combine(destinationPath, fileName);
                // Datei im Ziel überschreiben
                if (File.Exists(destFilePath))
                {
                    File.Delete(destFilePath);
                }
                File.Move(filePath, destFilePath); // Datei verschieben
            }

            // Unterordner rekursiv verschieben
            foreach (string directoryPath in Directory.GetDirectories(sourcePath))
            {
                string directoryName = Path.GetFileName(directoryPath);
                string destDirectoryPath = Path.Combine(destinationPath, directoryName);
                MoveFolder(directoryPath, destDirectoryPath);
            }

            // Löschen des Quellordners nach erfolgreichem Verschieben
            Directory.Delete(sourcePath, true); // `true` löscht auch Unterverzeichnisse
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Verschieben des Ordners: {ex.Message}");
        }
    }
}

