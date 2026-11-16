using Weichen_Checkliste;

string basis = Path.Combine(Path.GetTempPath(), "WeichenSyncTest-" + Guid.NewGuid().ToString("N"));
try
{
    string remoteAv = Path.Combine(basis, "remote-av");
    string lokalAvA = Path.Combine(basis, "lokal-av-a");
    string lokalAvB = Path.Combine(basis, "lokal-av-b");
    string remoteR = Path.Combine(basis, "remote-r");
    string lokalRA = Path.Combine(basis, "lokal-r-a");
    string lokalRB = Path.Combine(basis, "lokal-r-b");
    Directory.CreateDirectory(remoteAv);
    Directory.CreateDirectory(remoteR);

    File.WriteAllText(Path.Combine(remoteAv, "wartung.csv"), "Version 1");
    Ok(new("AV A", remoteAv, lokalAvA, true));
    Ok(new("AV B", remoteAv, lokalAvB, true));
    Assert(File.ReadAllText(Path.Combine(lokalAvA, "wartung.csv")) == "Version 1", "AV A");
    Assert(File.ReadAllText(Path.Combine(lokalAvB, "wartung.csv")) == "Version 1", "AV B");

    File.WriteAllText(Path.Combine(remoteAv, "wartung.csv"), "Version 2");
    Ok(new("AV A", remoteAv, lokalAvA, true));
    Assert(File.ReadAllText(Path.Combine(lokalAvA, "wartung.csv")) == "Version 2", "Update");
    File.Delete(Path.Combine(remoteAv, "wartung.csv"));
    Ok(new("AV A", remoteAv, lokalAvA, true));
    Assert(!File.Exists(Path.Combine(lokalAvA, "wartung.csv")), "Mirror deletion");

    File.WriteAllText(Path.Combine(lokalAvA, "offline.csv"), "cache");
    Directory.Delete(remoteAv);
    var offline = Run(new("AV offline", remoteAv, lokalAvA, true));
    Assert(!offline.Erfolgreich, "offline result");
    Assert(File.Exists(Path.Combine(lokalAvA, "offline.csv")), "offline cache persists");

    Directory.CreateDirectory(lokalRA);
    Directory.CreateDirectory(lokalRB);
    File.WriteAllText(Path.Combine(lokalRA, "123_20261116.xlsx"), "A");
    File.WriteAllText(Path.Combine(lokalRB, "123_20261116.xlsx"), "B");
    Ok(new("Rueckmeldung A", remoteR, lokalRA, false));
    Assert(!File.Exists(Path.Combine(lokalRA, "123_20261116.xlsx")), "A removed");
    Ok(new("Rueckmeldung B", remoteR, lokalRB, false));
    Assert(File.ReadAllText(Path.Combine(remoteR, "123_20261116.xlsx")) == "B", "last upload wins");
    Assert(!File.Exists(Path.Combine(lokalRB, "123_20261116.xlsx")), "B removed");
    Assert(Directory.GetFiles(remoteR).Length == 1, "no duplicate remote");

    File.WriteAllText(Path.Combine(lokalRB, "offline.xlsx"), "retain");
    Directory.Delete(remoteR, true);
    var failedUpload = Run(new("Upload offline", remoteR, lokalRB, false));
    Assert(!failedUpload.Erfolgreich, "upload fails without remote");
    Assert(File.Exists(Path.Combine(lokalRB, "offline.xlsx")), "local report persists");

    Console.WriteLine("Alle Synchronisations-Integrationstests bestanden.");
}
finally
{
    if (Directory.Exists(basis)) Directory.Delete(basis, true);
}

static SynchronisationsErgebnis Run(SynchronisationsAuftrag a) =>
    SynchronisationsDienst.Ausfuehren(new[] { a }).Single();

static void Ok(SynchronisationsAuftrag a)
{
    var r = Run(a);
    Assert(r.Erfolgreich, a.Name + ": " + r.Fehler);
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new Exception("Test fehlgeschlagen: " + message);
}
