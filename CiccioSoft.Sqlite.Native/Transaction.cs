// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;

namespace CiccioSoft.Sqlite.Native;

public sealed class Transaction : IDisposable
{
    private bool disposedValue;
    private readonly Connection _connection;

    public Transaction(Connection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
    }

    public void Commit()
    {
    	try
    	{

    	}
    	catch
    	{

    	}
    }

    public void Rollback()
    {
    	try
    	{

    	}
    	catch
    	{
    		
    	}
    }

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: eliminare lo stato gestito (oggetti gestiti)
            }

            // TODO: liberare risorse non gestite (oggetti non gestiti) ed eseguire l'override del finalizzatore
            // TODO: impostare campi di grandi dimensioni su Null
            disposedValue = true;
        }
    }

    // // TODO: eseguire l'override del finalizzatore solo se 'Dispose(bool disposing)' contiene codice per liberare risorse non gestite
    // ~Transaction()
    // {
    //     // Non modificare questo codice. Inserire il codice di pulizia nel metodo 'Dispose(bool disposing)'
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Non modificare questo codice. Inserire il codice di pulizia nel metodo 'Dispose(bool disposing)'
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
