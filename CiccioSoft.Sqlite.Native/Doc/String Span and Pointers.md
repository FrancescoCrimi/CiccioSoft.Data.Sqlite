# Todo


## String, Span e puntatori

### Wrapping da puntatore byte* a Span **senza copiare** i dati.

Se non si conosce la lunghezza
```
var span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ptr);
```

Se si conosce la lunghezza
```
var span = new ReadOnlySpan<byte>(ptr, length);
```


### Conversione da puntatore byte* a Span **copiando** i dati

Se non si conosce la lunghezza
```
var temp = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ptr);
var copiaSpan = new byte[temp.Length].AsSpan();
temp.CopyTo(copiaSpan);
```

Se si conosce la lunghezza
```
var copiaSpan = new byte[length].AsSpan();
new ReadOnlySpan<byte>(ptr, length).CopyTo(copiaSpan);
```


### Conversione da puntatore byte* a String (é sempre una copia, non puo essere altrimenti, c'é una conversione utf8->utf16).

Se non si conosce la lunghezza
```
string testo = Marshal.PtrToStringUTF8((IntPtr)ptr);
```

Se si conosce la lunghezza
```
var span = new ReadOnlySpan<byte>(ptr, length);
var string = new String(span);
```
