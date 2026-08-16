// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite;

/// <summary>
/// Mappatura 1:1 sulle tre forme native di <c>BEGIN</c> (Tier 0 §16). Nessuna
/// derivazione da <c>System.Data.IsolationLevel</c> (Tier 1 §1.4): i tre valori sono la
/// proiezione idiomatica del vocabolario nativo, non un adattamento di un vocabolario
/// estraneo.
/// </summary>
public enum SqliteTransactionMode
{
    /// <summary>
    /// <c>BEGIN DEFERRED</c>: nessun lock preso finché non avviene il primo accesso.
    /// In modalità Coordinated, il writer lease non è acquisito qui — solo alla prima
    /// scrittura effettiva della transazione (upgrade lazy, vedi <see cref="SqliteTransaction"/>).
    /// </summary>
    Deferred,

    /// <summary>
    /// <c>BEGIN IMMEDIATE</c>: dichiara l'intenzione di scrivere fin da subito (lock
    /// RESERVED immediato). In modalità Coordinated, il writer lease è acquisito prima
    /// di questo comando — stessa semantica nativa, senza ritardo (default).
    /// </summary>
    Immediate,

    /// <summary>
    /// <c>BEGIN EXCLUSIVE</c>: lock esclusivo immediato. In modalità Coordinated, il
    /// writer lease è acquisito prima di questo comando, come per <see cref="Immediate"/>.
    /// </summary>
    Exclusive
}
