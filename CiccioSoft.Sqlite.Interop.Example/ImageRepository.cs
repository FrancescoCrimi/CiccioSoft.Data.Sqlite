using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CiccioSoft.Sqlite.Interop.Example;

public record ImageRow(int Id, string? Nome, byte[] Image);

public class ImageRepository
{

    static List<string> images = new List<string>
    {
        "Beatles-Abbey-Road.bmp",
        "Beatles-Sgt.-Pepper.bmp",
        "Blur-Parklife.bmp",
        "Led-Zeppelin-IV.bmp",
        "Nirvana-Nevermind.bmp",
        "Pink-Floyd-Wish.bmp",
        "The-Clash-London-Calling.bmp"
    };

    private readonly Sqlite3 _connection;

    public ImageRepository()
    {
        _connection = Sqlite3.Open("test.db");
        _connection.Execute("CREATE TABLE IF NOT EXISTS Images (Id INTEGER PRIMARY KEY, Nome TEXT, Image BLOB)");

        // // Insert Image in to Db
        // foreach (var image in images)
        // {
        //     byte[] immagineRaw = File.ReadAllBytes("Images//" + image);
        //     Add(image, immagineRaw);
        //     // DisegnaInConsoleAltaRisoluzione(immagineRaw, maxWidth: 160);
        // }

        var list = GetAll();
        foreach (var raw in list)
        {
            Console.WriteLine("Immaggine {0}: {1}", raw.Id, raw.Nome);
            DisegnaInConsoleAltaRisoluzione(raw.Image, maxWidth: 160);
        }

    }

    public void Add(string nome, byte[] image)
    {
        // Usiamo i parametri '?' per il binding
        using var stmt = _connection.Prepare("INSERT INTO Images (Nome, Image) VALUES (?, ?)");
        stmt.BindText(1, nome); // Gli indici dei parametri partono da 1
        stmt.BindBlob(2, image);
        stmt.Step();
    }

    public List<ImageRow> GetAll()
    {
        var list = new List<ImageRow>();
        using var stmt = _connection.Prepare("SELECT Id, Nome, Image FROM Images");

        while (stmt.Step())
        {
            list.Add(new ImageRow(stmt.GetInt(0), stmt.GetString(1), stmt.GetBlob(2).ToArray()));
        }
        return list;
    }

    public void Delete(int id)
    {
        using var stmt = _connection.Prepare($"DELETE FROM Images WHERE Id = ?");
        stmt.BindInt(1, id);
        stmt.Step();
    }

    static void DisegnaInConsole(byte[] blobData, int maxWidth = 80)
    {
        // Carica l'immagine dal BLOB
        using Image<Rgb24> image = Image.Load<Rgb24>(blobData);

        // Ridimensiona mantenendo le proporzioni
        // Nota: dividiamo l'altezza per 2 perché i caratteri della console sono rettangolari (alti)
        int height = (int)((double)image.Height / image.Width * maxWidth * 0.5);
        image.Mutate(x => x.Resize(maxWidth, height));

        for (int y = 0; y < image.Height; y++)
        {
            // Otteniamo la riga di pixel corrente
            Span<Rgb24> row = image.DangerousGetPixelRowMemory(y).Span;

            for (int x = 0; x < row.Length; x++)
            {
                Rgb24 pixel = row[x];
                // Sequenza ANSI TrueColor per lo sfondo
                Console.Write($"\u001b[48;2;{pixel.R};{pixel.G};{pixel.B}m ");
            }
            // Resetta il colore e vai a capo
            Console.WriteLine("\u001b[0m");
        }
    }

    static void DisegnaInConsoleAltaRisoluzione(byte[] blobData, int maxWidth = 80)
    {
        using Image<Rgb24> image = Image.Load<Rgb24>(blobData);

        // Ridimensioniamo l'immagine. 
        // L'altezza deve essere pari perché ogni riga di testo contiene 2 pixel verticali.
        int height = (int)((double)image.Height / image.Width * maxWidth);
        if (height % 2 != 0) height--;

        image.Mutate(x => x.Resize(maxWidth, height));

        for (int y = 0; y < image.Height; y += 2)
        {
            Span<Rgb24> topRow = image.DangerousGetPixelRowMemory(y).Span;
            Span<Rgb24> bottomRow = image.DangerousGetPixelRowMemory(y + 1).Span;

            for (int x = 0; x < topRow.Length; x++)
            {
                var top = topRow[x];
                var bottom = bottomRow[x];

                // \u001b[48;2;R;G;Bm -> Colore Sfondo (pixel sopra)
                // \u001b[38;2;R;G;Bm -> Colore Testo (pixel sotto)
                // \u2584 -> Carattere mezzoblocco inferiore '▄'
                Console.Write($"\u001b[48;2;{top.R};{top.G};{top.B}m\u001b[38;2;{bottom.R};{bottom.G};{bottom.B}m▄");
            }
            Console.WriteLine("\u001b[0m"); // Reset colore
        }
    }
}