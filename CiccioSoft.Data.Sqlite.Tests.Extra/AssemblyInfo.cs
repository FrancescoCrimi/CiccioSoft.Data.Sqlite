// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using Xunit;
using CiccioSoft.Data.Sqlite;

// Tutti i test di questo assembly gireranno in sequenza (uno dopo l'altro)
[assembly: CollectionBehavior(DisableTestParallelization = true)]

// Questo dice a xUnit v3 di applicare la fixture a tutto l'assembly automaticamente
[assembly: AssemblyFixture(typeof(SessionConfigurationFixture))]
