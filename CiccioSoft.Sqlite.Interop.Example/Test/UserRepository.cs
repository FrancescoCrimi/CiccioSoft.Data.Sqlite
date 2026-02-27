using System;
using System.Collections.Generic;

namespace CiccioSoft.Sqlite.Interop.Example;

public record UserRow(int Id, string? Nome);

public class UserRepository
{
    private readonly Sqlite3 _connection;

    public UserRepository(string? nomeDaInserire)
    {

        Console.WriteLine("Tentativo di apertura database...");

        _connection = Sqlite3.Open("test.db");

        // Crea la tabella se non esiste
        _connection.Execute("CREATE TABLE IF NOT EXISTS Utenti (Id INTEGER PRIMARY KEY, Nome TEXT)");

        if (nomeDaInserire != null)
        {
            Console.WriteLine($"Inserimento di: {nomeDaInserire}...");
            Add(nomeDaInserire);
        }

        // Lettura e stampa di tutti i record presenti
        Console.WriteLine("\nElenco Utenti nel Database:");
        Console.WriteLine("---------------------------");
        var list = GetAll();
        foreach (UserRow user in list)
        {
            Console.WriteLine("Id: {0} Name: {1}", user.Id, user.Nome);
        }
    }

    void Add(string nome)
    {
        // Usiamo i parametri '?' per il binding
        using var stmt = _connection.Prepare("INSERT INTO Utenti (Nome) VALUES (?)");
        stmt.BindText(1, nome); // Gli indici dei parametri partono da 1
        stmt.Step();
    }

    List<UserRow> GetAll()
    {
        var list = new List<UserRow>();
        using var stmt = _connection.Prepare("SELECT Id, Nome FROM Utenti");

        while (stmt.Step())
        {
            list.Add(new UserRow(stmt.GetInt(0), stmt.GetString(1)));
        }
        return list;
    }

    void Delete(int id)
    {
        using var stmt = _connection.Prepare($"DELETE FROM Utenti WHERE Id = ?");
        stmt.BindInt(1, id);
        stmt.Step();
    }
}
