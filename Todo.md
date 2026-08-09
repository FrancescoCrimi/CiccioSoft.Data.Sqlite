# Todo


## CiccioSoft.Sqlite
1)	Implementare SqliteBlob come meccanismo per la gestione dei blob. (fatto)
2)	Creare overload per tutti i metodi che prendono string come parametro con equivalenti che accettano
	span<byte> direttamente in in utf8.
3)	Verificare funzionalita ed helper dell'eccezzione


## CiccioSoft.Data.Sqlite
1)	Aumentare la compatibilita a livello Api con Microsoft.Data.Sqlite
	per fare in modo che CiccioSoft.Data.Sqlite possa diventare drop-in replacement per 
	Microsoft.Data.Sqlite.
2)	Agiungere funzionalità CreateFunction e CreateAggregateCore magari copiando quello che serve 
	da Microsoft.Data.Sqlite anche perche tutti e due usano la stessa licenza MIT.
3)	Portare il livello di CiccioSoft.Data.Sqlite a livello di un provider adone di classe Enterprice
	e renderlo migliore di Microsoft.Data.Sqlite
4) 	Verificare funzionalità del SqliteConnectionPool e SqliteConnectionStringBuilder
5) 	Threading e async
