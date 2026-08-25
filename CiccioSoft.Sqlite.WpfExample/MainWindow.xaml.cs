// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CiccioSoft.Sqlite.WpfExample;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private int _indiceCorrente = 0;
    private int _countImages;
    private readonly Connection? _connection;

    public MainWindow()
    {
        InitializeComponent();

        try
        {
            NativeLibrary.Configure(NativeSource.SourceGear);
            _connection = Connection.Open("test.db");
        }
        catch
        {
            TestoDescrizione.Text = "Errore caricamento db.";
        }
        AggiornaInterfaccia();
    }

    // Aggiorna l'immagine e il testo in base all'indice corrente
    private void AggiornaInterfaccia()
    {
        // Controllo di sicurezza: se la connessione è null, esci subito
        if (_connection == null)
        {
            TestoDescrizione.Text = "Database non connesso.";
            VisualizzatoreImmagine.Source = ToBitmapImage(OttieniByteBmpDiEsempio());
            return;
        }

        try
        {
            using var stmt = _connection.Prepare("SELECT COUNT(*) FROM Images");
            stmt.Step();
            _countImages = stmt.GetInt(0);

            if (_countImages > 0)
                ShowImage(1);
            else
            {
                TestoDescrizione.Text = "Database Vuoto";
                VisualizzatoreImmagine.Source = ToBitmapImage(OttieniByteBmpDiEsempio());
            }
        }
        catch (System.Exception)
        {
            TestoDescrizione.Text = "Errore caricamento immagine.";
            VisualizzatoreImmagine.Source = ToBitmapImage(OttieniByteBmpDiEsempio());
        }
    }


    private void Reload()
    {
        // Controllo di sicurezza: se la connessione è null, esci subito
        if (_connection == null)
        {
            TestoDescrizione.Text = "Database non connesso.";
            VisualizzatoreImmagine.Source = ToBitmapImage(OttieniByteBmpDiEsempio());
            return;
        }

        _connection.Execute("DROP TABLE IF EXISTS Images;");
        _connection.Execute("CREATE TABLE IF NOT EXISTS Images (Id INTEGER PRIMARY KEY, Nome TEXT, Image BLOB)");

        // Cerca una cartella chiamata "Images" posizionata accanto all'eseguibile .exe
        string percorsoRelativo = "Images";
        if (!Directory.Exists(percorsoRelativo)) return;

        // Ottiene l'elenco dei file usando il percorso relativo
        string[] fileBmp = Directory.GetFiles(percorsoRelativo, "*.bmp");
        if (fileBmp.Length == 0) return;

        // 1. Apriamo la transazione PRIMA di fare qualsiasi altra cosa
        _connection.Execute("BEGIN TRANSACTION;");

        try
        {
            using (var stmt = _connection.Prepare("INSERT INTO Images (Nome, Image) VALUES (?, ?)"))
            {
                // Insert Image in to Db
                foreach (var image in fileBmp)
                {
                    byte[] immagineRaw = File.ReadAllBytes(image);
                    stmt.BindText(1, Path.GetFileNameWithoutExtension(image)); // Gli indici dei parametri partono da 1
                    stmt.BindBlob(2, immagineRaw);
                    stmt.Step();
                    stmt.Reset();
                }
            } // Qui lo statement viene distrutto in automatico (Dispose)

            // 2. Il COMMIT va qui: se siamo arrivati a questo punto, tutto è andato bene!
            _connection.Execute("COMMIT;");

        }
        catch (System.Exception)
        {
            // 3. SE QUALCOSA FALLISCE: Annulliamo tutto e liberiamo il database immediatamente
            _connection.Execute("ROLLBACK;");
        }
    }


    private void ShowImage(int index)
    {
        // Applica il controllo di sicurezza anche qui (o usa l'operatore null-conditional _connection?)
        if (_connection == null) return;

        using var stmt = _connection.Prepare("SELECT Id, Nome, Image FROM Images WHERE Id = ?");
        stmt.BindInt(1, index);
        stmt.Step();
        _indiceCorrente = stmt.GetInt(0);
        TestoDescrizione.Text = stmt.GetTextString(1);
        VisualizzatoreImmagine.Source = ToBitmapImage(stmt.GetBlob(2).ToArray());
    }

    // Tasto Dietro
    private void BtnDietro_Click(object sender, RoutedEventArgs e)
    {
        if (_indiceCorrente > 1)
        {
            _indiceCorrente--;
            ShowImage(_indiceCorrente);
        }
    }

    // Tasto Avanti
    private void BtnAvanti_Click(object sender, RoutedEventArgs e)
    {
        if (_indiceCorrente < _countImages)
        {
            _indiceCorrente++;
            ShowImage(_indiceCorrente);
        }
    }

    // Tasto Ricarica (ripristina al primo elemento o aggiorna i dati)
    private void BtnRicarica_Click(object sender, RoutedEventArgs e)
    {
        Reload();
        AggiornaInterfaccia();
    }

    // Tasto Chiudi
    private void BtnChiudi_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private byte[] OttieniByteBmpDiEsempio()
    {
        // Questo è solo un helper per non far crashare l'app di test.
        // Restituisce un mini-array che rappresenta un file BMP valido vuoto.
        return new byte[] {
            0x42, 0x4D, 0x3A, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x28, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00
        };
    }

    // Estensione per trasformare un byte[] array direttamente in un BitmapImage pronto
    private BitmapImage ToBitmapImage(byte[] array)
    {
        using var ms = new MemoryStream(array);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad; // Carica subito i pixel
        bitmap.StreamSource = ms;
        bitmap.EndInit();
        bitmap.Freeze(); // Rende l'immagine thread-safe e veloce
        return bitmap;
    }
}
