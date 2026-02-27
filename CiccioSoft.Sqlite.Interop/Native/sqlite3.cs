using System;
using System.Runtime.InteropServices;

namespace CiccioSoft.Sqlite.Interop.Native
{
    public static unsafe partial class sqlite3
    {
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_libversion();

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_sourceid();

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_libversion_number();

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_compileoption_used([NativeTypeName("const char *")] byte* zOptName);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_compileoption_get(int N);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_threadsafe();

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_close([NativeTypeName("sqlite3*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_close_v2([NativeTypeName("sqlite3*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_exec([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("const char *")] byte* sql, [NativeTypeName("int (*)(void *, int, char **, char **)")] delegate* unmanaged[Cdecl]<void*, int, byte**, byte**, int> callback, void* param3, [NativeTypeName("char **")] byte** errmsg);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_initialize();

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_shutdown();

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_os_init();

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_os_end();

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_config(int param0, __arglist);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_db_config([NativeTypeName("sqlite3*")] nint param0, int op, __arglist);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_extended_result_codes([NativeTypeName("sqlite3*")] nint param0, int onoff);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_int64")]
        public static extern long sqlite3_last_insert_rowid([NativeTypeName("sqlite3*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_set_last_insert_rowid([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("sqlite3_int64")] long param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_changes([NativeTypeName("sqlite3*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_int64")]
        public static extern long sqlite3_changes64([NativeTypeName("sqlite3*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_total_changes([NativeTypeName("sqlite3*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_int64")]
        public static extern long sqlite3_total_changes64([NativeTypeName("sqlite3*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_interrupt([NativeTypeName("sqlite3*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_is_interrupted([NativeTypeName("sqlite3*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_complete([NativeTypeName("const char *")] byte* sql);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_complete16([NativeTypeName("const void *")] void* sql);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_busy_handler([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("int (*)(void *, int)")] delegate* unmanaged[Cdecl]<void*, int, int> param1, void* param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_busy_timeout([NativeTypeName("sqlite3*")] nint param0, int ms);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_setlk_timeout([NativeTypeName("sqlite3*")] nint param0, int ms, int flags);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_get_table([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zSql, [NativeTypeName("char ***")] byte*** pazResult, int* pnRow, int* pnColumn, [NativeTypeName("char **")] byte** pzErrmsg);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_free_table([NativeTypeName("char **")] byte** result);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* sqlite3_mprintf([NativeTypeName("const char *")] byte* param0, __arglist);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* sqlite3_vmprintf([NativeTypeName("const char *")] byte* param0, [NativeTypeName("va_list")] byte* param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* sqlite3_snprintf(int param0, [NativeTypeName("char *")] byte* param1, [NativeTypeName("const char *")] byte* param2, __arglist);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* sqlite3_vsnprintf(int param0, [NativeTypeName("char *")] byte* param1, [NativeTypeName("const char *")] byte* param2, [NativeTypeName("va_list")] byte* param3);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* sqlite3_malloc(int param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* sqlite3_malloc64([NativeTypeName("sqlite3_uint64")] ulong param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* sqlite3_realloc(void* param0, int param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* sqlite3_realloc64(void* param0, [NativeTypeName("sqlite3_uint64")] ulong param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_free(void* param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_uint64")]
        public static extern ulong sqlite3_msize(void* param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_int64")]
        public static extern long sqlite3_memory_used();

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_int64")]
        public static extern long sqlite3_memory_highwater(int resetFlag);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_randomness(int N, void* P);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_set_authorizer([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("int (*)(void *, int, const char *, const char *, const char *, const char *)")] delegate* unmanaged[Cdecl]<void*, int, byte*, byte*, byte*, byte*, int> xAuth, void* pUserData);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* sqlite3_trace([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("void (*)(void *, const char *)")] delegate* unmanaged[Cdecl]<void*, byte*, void> xTrace, void* param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* sqlite3_profile([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("void (*)(void *, const char *, sqlite3_uint64)")] delegate* unmanaged[Cdecl]<void*, byte*, ulong, void> xProfile, void* param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_trace_v2([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("unsigned int")] uint uMask, [NativeTypeName("int (*)(unsigned int, void *, void *, void *)")] delegate* unmanaged[Cdecl]<uint, void*, void*, void*, int> xCallback, void* pCtx);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_progress_handler([NativeTypeName("sqlite3*")] nint param0, int param1, [NativeTypeName("int (*)(void *)")] delegate* unmanaged[Cdecl]<void*, int> param2, void* param3);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_open([NativeTypeName("const char *")] byte* filename, [NativeTypeName("sqlite3 **")] nint* ppDb);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_open16([NativeTypeName("const void *")] void* filename, [NativeTypeName("sqlite3 **")] nint* ppDb);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_open_v2([NativeTypeName("const char *")] byte* filename, [NativeTypeName("sqlite3 **")] nint* ppDb, int flags, [NativeTypeName("const char *")] byte* zVfs);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_uri_parameter([NativeTypeName("sqlite3_filename")] byte* z, [NativeTypeName("const char *")] byte* zParam);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_uri_boolean([NativeTypeName("sqlite3_filename")] byte* z, [NativeTypeName("const char *")] byte* zParam, int bDefault);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_int64")]
        public static extern long sqlite3_uri_int64([NativeTypeName("sqlite3_filename")] byte* param0, [NativeTypeName("const char *")] byte* param1, [NativeTypeName("sqlite3_int64")] long param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_uri_key([NativeTypeName("sqlite3_filename")] byte* z, int N);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_filename_database([NativeTypeName("sqlite3_filename")] byte* param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_filename_journal([NativeTypeName("sqlite3_filename")] byte* param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_filename_wal([NativeTypeName("sqlite3_filename")] byte* param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_file*")]
        public static extern nint sqlite3_database_file_object([NativeTypeName("const char *")] byte* param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_filename")]
        public static extern byte* sqlite3_create_filename([NativeTypeName("const char *")] byte* zDatabase, [NativeTypeName("const char *")] byte* zJournal, [NativeTypeName("const char *")] byte* zWal, int nParam, [NativeTypeName("const char **")] byte** azParam);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_free_filename([NativeTypeName("sqlite3_filename")] byte* param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_errcode([NativeTypeName("sqlite3*")] nint db);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_extended_errcode([NativeTypeName("sqlite3*")] nint db);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_errmsg([NativeTypeName("sqlite3*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const void *")]
        public static extern void* sqlite3_errmsg16([NativeTypeName("sqlite3*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_errstr(int param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_error_offset([NativeTypeName("sqlite3*")] nint db);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_set_errmsg([NativeTypeName("sqlite3*")] nint db, int errcode, [NativeTypeName("const char *")] byte* zErrMsg);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_limit([NativeTypeName("sqlite3*")] nint param0, int id, int newVal);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_prepare([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zSql, int nByte, [NativeTypeName("sqlite3_stmt **")] nint* ppStmt, [NativeTypeName("const char **")] byte** pzTail);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_prepare_v2([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zSql, int nByte, [NativeTypeName("sqlite3_stmt **")] nint* ppStmt, [NativeTypeName("const char **")] byte** pzTail);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_prepare_v3([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zSql, int nByte, [NativeTypeName("unsigned int")] uint prepFlags, [NativeTypeName("sqlite3_stmt **")] nint* ppStmt, [NativeTypeName("const char **")] byte** pzTail);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_prepare16([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const void *")] void* zSql, int nByte, [NativeTypeName("sqlite3_stmt **")] nint* ppStmt, [NativeTypeName("const void **")] void** pzTail);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_prepare16_v2([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const void *")] void* zSql, int nByte, [NativeTypeName("sqlite3_stmt **")] nint* ppStmt, [NativeTypeName("const void **")] void** pzTail);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_prepare16_v3([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const void *")] void* zSql, int nByte, [NativeTypeName("unsigned int")] uint prepFlags, [NativeTypeName("sqlite3_stmt **")] nint* ppStmt, [NativeTypeName("const void **")] void** pzTail);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_sql([NativeTypeName("sqlite3_stmt*")] nint pStmt);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* sqlite3_expanded_sql([NativeTypeName("sqlite3_stmt*")] nint pStmt);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_stmt_readonly([NativeTypeName("sqlite3_stmt*")] nint pStmt);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_stmt_isexplain([NativeTypeName("sqlite3_stmt*")] nint pStmt);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_stmt_explain([NativeTypeName("sqlite3_stmt*")] nint pStmt, int eMode);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_stmt_busy([NativeTypeName("sqlite3_stmt*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_bind_blob([NativeTypeName("sqlite3_stmt*")] nint param0, int param1, [NativeTypeName("const void *")] void* param2, int n, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> param4);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_bind_blob64([NativeTypeName("sqlite3_stmt*")] nint param0, int param1, [NativeTypeName("const void *")] void* param2, [NativeTypeName("sqlite3_uint64")] ulong param3, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> param4);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_bind_double([NativeTypeName("sqlite3_stmt*")] nint param0, int param1, double param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_bind_int([NativeTypeName("sqlite3_stmt*")] nint param0, int param1, int param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_bind_int64([NativeTypeName("sqlite3_stmt*")] nint param0, int param1, [NativeTypeName("sqlite3_int64")] long param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_bind_null([NativeTypeName("sqlite3_stmt*")] nint param0, int param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_bind_text([NativeTypeName("sqlite3_stmt*")] nint param0, int param1, [NativeTypeName("const char *")] byte* param2, int param3, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> param4);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_bind_text16([NativeTypeName("sqlite3_stmt*")] nint param0, int param1, [NativeTypeName("const void *")] void* param2, int param3, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> param4);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_bind_text64([NativeTypeName("sqlite3_stmt*")] nint param0, int param1, [NativeTypeName("const char *")] byte* param2, [NativeTypeName("sqlite3_uint64")] ulong param3, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> param4, [NativeTypeName("unsigned char")] byte encoding);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_bind_value([NativeTypeName("sqlite3_stmt*")] nint param0, int param1, [NativeTypeName("const sqlite3_value *")] nint param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_bind_pointer([NativeTypeName("sqlite3_stmt*")] nint param0, int param1, void* param2, [NativeTypeName("const char *")] byte* param3, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> param4);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_bind_zeroblob([NativeTypeName("sqlite3_stmt*")] nint param0, int param1, int n);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_bind_zeroblob64([NativeTypeName("sqlite3_stmt*")] nint param0, int param1, [NativeTypeName("sqlite3_uint64")] ulong param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_bind_parameter_count([NativeTypeName("sqlite3_stmt*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_bind_parameter_name([NativeTypeName("sqlite3_stmt*")] nint param0, int param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_bind_parameter_index([NativeTypeName("sqlite3_stmt*")] nint param0, [NativeTypeName("const char *")] byte* zName);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_clear_bindings([NativeTypeName("sqlite3_stmt*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_column_count([NativeTypeName("sqlite3_stmt*")] nint pStmt);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_column_name([NativeTypeName("sqlite3_stmt*")] nint param0, int N);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const void *")]
        public static extern void* sqlite3_column_name16([NativeTypeName("sqlite3_stmt*")] nint param0, int N);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_column_database_name([NativeTypeName("sqlite3_stmt*")] nint param0, int param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const void *")]
        public static extern void* sqlite3_column_database_name16([NativeTypeName("sqlite3_stmt*")] nint param0, int param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_column_table_name([NativeTypeName("sqlite3_stmt*")] nint param0, int param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const void *")]
        public static extern void* sqlite3_column_table_name16([NativeTypeName("sqlite3_stmt*")] nint param0, int param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_column_origin_name([NativeTypeName("sqlite3_stmt*")] nint param0, int param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const void *")]
        public static extern void* sqlite3_column_origin_name16([NativeTypeName("sqlite3_stmt*")] nint param0, int param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_column_decltype([NativeTypeName("sqlite3_stmt*")] nint param0, int param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const void *")]
        public static extern void* sqlite3_column_decltype16([NativeTypeName("sqlite3_stmt*")] nint param0, int param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_step([NativeTypeName("sqlite3_stmt*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_data_count([NativeTypeName("sqlite3_stmt*")] nint pStmt);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const void *")]
        public static extern void* sqlite3_column_blob([NativeTypeName("sqlite3_stmt*")] nint param0, int iCol);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern double sqlite3_column_double([NativeTypeName("sqlite3_stmt*")] nint param0, int iCol);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_column_int([NativeTypeName("sqlite3_stmt*")] nint param0, int iCol);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_int64")]
        public static extern long sqlite3_column_int64([NativeTypeName("sqlite3_stmt*")] nint param0, int iCol);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const unsigned char *")]
        public static extern byte* sqlite3_column_text([NativeTypeName("sqlite3_stmt*")] nint param0, int iCol);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const void *")]
        public static extern void* sqlite3_column_text16([NativeTypeName("sqlite3_stmt*")] nint param0, int iCol);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_value*")]
        public static extern nint sqlite3_column_value([NativeTypeName("sqlite3_stmt*")] nint param0, int iCol);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_column_bytes([NativeTypeName("sqlite3_stmt*")] nint param0, int iCol);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_column_bytes16([NativeTypeName("sqlite3_stmt*")] nint param0, int iCol);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_column_type([NativeTypeName("sqlite3_stmt*")] nint param0, int iCol);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_finalize([NativeTypeName("sqlite3_stmt*")] nint pStmt);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_reset([NativeTypeName("sqlite3_stmt*")] nint pStmt);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_create_function([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zFunctionName, int nArg, int eTextRep, void* pApp, [NativeTypeName("void (*)(sqlite3_context *, int, sqlite3_value **)")] delegate* unmanaged[Cdecl]<nint, int, nint*, void> xFunc, [NativeTypeName("void (*)(sqlite3_context *, int, sqlite3_value **)")] delegate* unmanaged[Cdecl]<nint, int, nint*, void> xStep, [NativeTypeName("void (*)(sqlite3_context *)")] delegate* unmanaged[Cdecl]<nint, void> xFinal);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_create_function16([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const void *")] void* zFunctionName, int nArg, int eTextRep, void* pApp, [NativeTypeName("void (*)(sqlite3_context *, int, sqlite3_value **)")] delegate* unmanaged[Cdecl]<nint, int, nint*, void> xFunc, [NativeTypeName("void (*)(sqlite3_context *, int, sqlite3_value **)")] delegate* unmanaged[Cdecl]<nint, int, nint*, void> xStep, [NativeTypeName("void (*)(sqlite3_context *)")] delegate* unmanaged[Cdecl]<nint, void> xFinal);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_create_function_v2([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zFunctionName, int nArg, int eTextRep, void* pApp, [NativeTypeName("void (*)(sqlite3_context *, int, sqlite3_value **)")] delegate* unmanaged[Cdecl]<nint, int, nint*, void> xFunc, [NativeTypeName("void (*)(sqlite3_context *, int, sqlite3_value **)")] delegate* unmanaged[Cdecl]<nint, int, nint*, void> xStep, [NativeTypeName("void (*)(sqlite3_context *)")] delegate* unmanaged[Cdecl]<nint, void> xFinal, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> xDestroy);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_create_window_function([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zFunctionName, int nArg, int eTextRep, void* pApp, [NativeTypeName("void (*)(sqlite3_context *, int, sqlite3_value **)")] delegate* unmanaged[Cdecl]<nint, int, nint*, void> xStep, [NativeTypeName("void (*)(sqlite3_context *)")] delegate* unmanaged[Cdecl]<nint, void> xFinal, [NativeTypeName("void (*)(sqlite3_context *)")] delegate* unmanaged[Cdecl]<nint, void> xValue, [NativeTypeName("void (*)(sqlite3_context *, int, sqlite3_value **)")] delegate* unmanaged[Cdecl]<nint, int, nint*, void> xInverse, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> xDestroy);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_aggregate_count([NativeTypeName("sqlite3_context*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_expired([NativeTypeName("sqlite3_stmt*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_transfer_bindings([NativeTypeName("sqlite3_stmt*")] nint param0, [NativeTypeName("sqlite3_stmt*")] nint param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_global_recover();

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_thread_cleanup();

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_memory_alarm([NativeTypeName("void (*)(void *, sqlite3_int64, int)")] delegate* unmanaged[Cdecl]<void*, long, int, void> param0, void* param1, [NativeTypeName("sqlite3_int64")] long param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const void *")]
        public static extern void* sqlite3_value_blob([NativeTypeName("sqlite3_value*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern double sqlite3_value_double([NativeTypeName("sqlite3_value*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_value_int([NativeTypeName("sqlite3_value*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_int64")]
        public static extern long sqlite3_value_int64([NativeTypeName("sqlite3_value*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* sqlite3_value_pointer([NativeTypeName("sqlite3_value*")] nint param0, [NativeTypeName("const char *")] byte* param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const unsigned char *")]
        public static extern byte* sqlite3_value_text([NativeTypeName("sqlite3_value*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const void *")]
        public static extern void* sqlite3_value_text16([NativeTypeName("sqlite3_value*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const void *")]
        public static extern void* sqlite3_value_text16le([NativeTypeName("sqlite3_value*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const void *")]
        public static extern void* sqlite3_value_text16be([NativeTypeName("sqlite3_value*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_value_bytes([NativeTypeName("sqlite3_value*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_value_bytes16([NativeTypeName("sqlite3_value*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_value_type([NativeTypeName("sqlite3_value*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_value_numeric_type([NativeTypeName("sqlite3_value*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_value_nochange([NativeTypeName("sqlite3_value*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_value_frombind([NativeTypeName("sqlite3_value*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_value_encoding([NativeTypeName("sqlite3_value*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint sqlite3_value_subtype([NativeTypeName("sqlite3_value*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_value*")]
        public static extern nint sqlite3_value_dup([NativeTypeName("const sqlite3_value *")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_value_free([NativeTypeName("sqlite3_value*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* sqlite3_aggregate_context([NativeTypeName("sqlite3_context*")] nint param0, int nBytes);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* sqlite3_user_data([NativeTypeName("sqlite3_context*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3*")]
        public static extern nint sqlite3_context_db_handle([NativeTypeName("sqlite3_context*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* sqlite3_get_auxdata([NativeTypeName("sqlite3_context*")] nint param0, int N);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_set_auxdata([NativeTypeName("sqlite3_context*")] nint param0, int N, void* param2, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> param3);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* sqlite3_get_clientdata([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("const char *")] byte* param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_set_clientdata([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("const char *")] byte* param1, void* param2, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> param3);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_blob([NativeTypeName("sqlite3_context*")] nint param0, [NativeTypeName("const void *")] void* param1, int param2, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> param3);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_blob64([NativeTypeName("sqlite3_context*")] nint param0, [NativeTypeName("const void *")] void* param1, [NativeTypeName("sqlite3_uint64")] ulong param2, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> param3);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_double([NativeTypeName("sqlite3_context*")] nint param0, double param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_error([NativeTypeName("sqlite3_context*")] nint param0, [NativeTypeName("const char *")] byte* param1, int param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_error16([NativeTypeName("sqlite3_context*")] nint param0, [NativeTypeName("const void *")] void* param1, int param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_error_toobig([NativeTypeName("sqlite3_context*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_error_nomem([NativeTypeName("sqlite3_context*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_error_code([NativeTypeName("sqlite3_context*")] nint param0, int param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_int([NativeTypeName("sqlite3_context*")] nint param0, int param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_int64([NativeTypeName("sqlite3_context*")] nint param0, [NativeTypeName("sqlite3_int64")] long param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_null([NativeTypeName("sqlite3_context*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_text([NativeTypeName("sqlite3_context*")] nint param0, [NativeTypeName("const char *")] byte* param1, int param2, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> param3);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_text64([NativeTypeName("sqlite3_context*")] nint param0, [NativeTypeName("const char *")] byte* param1, [NativeTypeName("sqlite3_uint64")] ulong param2, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> param3, [NativeTypeName("unsigned char")] byte encoding);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_text16([NativeTypeName("sqlite3_context*")] nint param0, [NativeTypeName("const void *")] void* param1, int param2, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> param3);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_text16le([NativeTypeName("sqlite3_context*")] nint param0, [NativeTypeName("const void *")] void* param1, int param2, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> param3);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_text16be([NativeTypeName("sqlite3_context*")] nint param0, [NativeTypeName("const void *")] void* param1, int param2, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> param3);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_value([NativeTypeName("sqlite3_context*")] nint param0, [NativeTypeName("sqlite3_value*")] nint param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_pointer([NativeTypeName("sqlite3_context*")] nint param0, void* param1, [NativeTypeName("const char *")] byte* param2, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> param3);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_zeroblob([NativeTypeName("sqlite3_context*")] nint param0, int n);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_result_zeroblob64([NativeTypeName("sqlite3_context*")] nint param0, [NativeTypeName("sqlite3_uint64")] ulong n);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_result_subtype([NativeTypeName("sqlite3_context*")] nint param0, [NativeTypeName("unsigned int")] uint param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_create_collation([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("const char *")] byte* zName, int eTextRep, void* pArg, [NativeTypeName("int (*)(void *, int, const void *, int, const void *)")] delegate* unmanaged[Cdecl]<void*, int, void*, int, void*, int> xCompare);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_create_collation_v2([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("const char *")] byte* zName, int eTextRep, void* pArg, [NativeTypeName("int (*)(void *, int, const void *, int, const void *)")] delegate* unmanaged[Cdecl]<void*, int, void*, int, void*, int> xCompare, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> xDestroy);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_create_collation16([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("const void *")] void* zName, int eTextRep, void* pArg, [NativeTypeName("int (*)(void *, int, const void *, int, const void *)")] delegate* unmanaged[Cdecl]<void*, int, void*, int, void*, int> xCompare);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_collation_needed([NativeTypeName("sqlite3*")] nint param0, void* param1, [NativeTypeName("void (*)(void *, sqlite3 *, int, const char *)")] delegate* unmanaged[Cdecl]<void*, nint, int, byte*, void> param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_collation_needed16([NativeTypeName("sqlite3*")] nint param0, void* param1, [NativeTypeName("void (*)(void *, sqlite3 *, int, const void *)")] delegate* unmanaged[Cdecl]<void*, nint, int, void*, void> param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_sleep(int param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_win32_set_directory([NativeTypeName("unsigned long")] uint type, void* zValue);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_win32_set_directory8([NativeTypeName("unsigned long")] uint type, [NativeTypeName("const char *")] byte* zValue);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_win32_set_directory16([NativeTypeName("unsigned long")] uint type, [NativeTypeName("const void *")] void* zValue);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_get_autocommit([NativeTypeName("sqlite3*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3*")]
        public static extern nint sqlite3_db_handle([NativeTypeName("sqlite3_stmt*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_db_name([NativeTypeName("sqlite3*")] nint db, int N);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_filename")]
        public static extern byte* sqlite3_db_filename([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zDbName);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_db_readonly([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zDbName);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_txn_state([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("const char *")] byte* zSchema);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_stmt*")]
        public static extern nint sqlite3_next_stmt([NativeTypeName("sqlite3*")] nint pDb, [NativeTypeName("sqlite3_stmt*")] nint pStmt);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* sqlite3_commit_hook([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("int (*)(void *)")] delegate* unmanaged[Cdecl]<void*, int> param1, void* param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* sqlite3_rollback_hook([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> param1, void* param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_autovacuum_pages([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("unsigned int (*)(void *, const char *, unsigned int, unsigned int, unsigned int)")] delegate* unmanaged[Cdecl]<void*, byte*, uint, uint, uint, uint> param1, void* param2, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> param3);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* sqlite3_update_hook([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("void (*)(void *, int, const char *, const char *, sqlite3_int64)")] delegate* unmanaged[Cdecl]<void*, int, byte*, byte*, long, void> param1, void* param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_enable_shared_cache(int param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_release_memory(int param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_db_release_memory([NativeTypeName("sqlite3*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_int64")]
        public static extern long sqlite3_soft_heap_limit64([NativeTypeName("sqlite3_int64")] long N);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_int64")]
        public static extern long sqlite3_hard_heap_limit64([NativeTypeName("sqlite3_int64")] long N);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_soft_heap_limit(int N);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_table_column_metadata([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zDbName, [NativeTypeName("const char *")] byte* zTableName, [NativeTypeName("const char *")] byte* zColumnName, [NativeTypeName("const char **")] byte** pzDataType, [NativeTypeName("const char **")] byte** pzCollSeq, int* pNotNull, int* pPrimaryKey, int* pAutoinc);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_load_extension([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zFile, [NativeTypeName("const char *")] byte* zProc, [NativeTypeName("char **")] byte** pzErrMsg);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_enable_load_extension([NativeTypeName("sqlite3*")] nint db, int onoff);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_auto_extension([NativeTypeName("void (*)(void)")] delegate* unmanaged[Cdecl]<void> xEntryPoint);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_cancel_auto_extension([NativeTypeName("void (*)(void)")] delegate* unmanaged[Cdecl]<void> xEntryPoint);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_reset_auto_extension();

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_create_module([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zName, [NativeTypeName("const sqlite3_module *")] nint p, void* pClientData);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_create_module_v2([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zName, [NativeTypeName("const sqlite3_module *")] nint p, void* pClientData, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> xDestroy);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_drop_modules([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char **")] byte** azKeep);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_declare_vtab([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("const char *")] byte* zSQL);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_overload_function([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("const char *")] byte* zFuncName, int nArg);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_blob_open([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("const char *")] byte* zDb, [NativeTypeName("const char *")] byte* zTable, [NativeTypeName("const char *")] byte* zColumn, [NativeTypeName("sqlite3_int64")] long iRow, int flags, [NativeTypeName("sqlite3_blob **")] nint* ppBlob);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_blob_reopen([NativeTypeName("sqlite3_blob*")] nint param0, [NativeTypeName("sqlite3_int64")] long param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_blob_close([NativeTypeName("sqlite3_blob*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_blob_bytes([NativeTypeName("sqlite3_blob*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_blob_read([NativeTypeName("sqlite3_blob*")] nint param0, void* Z, int N, int iOffset);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_blob_write([NativeTypeName("sqlite3_blob*")] nint param0, [NativeTypeName("const void *")] void* z, int n, int iOffset);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_vfs*")]
        public static extern nint sqlite3_vfs_find([NativeTypeName("const char *")] byte* zVfsName);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_vfs_register([NativeTypeName("sqlite3_vfs*")] nint param0, int makeDflt);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_vfs_unregister([NativeTypeName("sqlite3_vfs*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_mutex*")]
        public static extern nint sqlite3_mutex_alloc(int param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_mutex_free([NativeTypeName("sqlite3_mutex*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_mutex_enter([NativeTypeName("sqlite3_mutex*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_mutex_try([NativeTypeName("sqlite3_mutex*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_mutex_leave([NativeTypeName("sqlite3_mutex*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_mutex_held([NativeTypeName("sqlite3_mutex*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_mutex_notheld([NativeTypeName("sqlite3_mutex*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_mutex*")]
        public static extern nint sqlite3_db_mutex([NativeTypeName("sqlite3*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_file_control([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("const char *")] byte* zDbName, int op, void* param3);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_test_control(int op, __arglist);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_keyword_count();

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_keyword_name(int param0, [NativeTypeName("const char **")] byte** param1, int* param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_keyword_check([NativeTypeName("const char *")] byte* param0, int param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_str*")]
        public static extern nint sqlite3_str_new([NativeTypeName("sqlite3*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* sqlite3_str_finish([NativeTypeName("sqlite3_str*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_str_appendf([NativeTypeName("sqlite3_str*")] nint param0, [NativeTypeName("const char *")] byte* zFormat, __arglist);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_str_vappendf([NativeTypeName("sqlite3_str*")] nint param0, [NativeTypeName("const char *")] byte* zFormat, [NativeTypeName("va_list")] byte* param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_str_append([NativeTypeName("sqlite3_str*")] nint param0, [NativeTypeName("const char *")] byte* zIn, int N);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_str_appendall([NativeTypeName("sqlite3_str*")] nint param0, [NativeTypeName("const char *")] byte* zIn);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_str_appendchar([NativeTypeName("sqlite3_str*")] nint param0, int N, [NativeTypeName("char")] byte C);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_str_reset([NativeTypeName("sqlite3_str*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_str_errcode([NativeTypeName("sqlite3_str*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_str_length([NativeTypeName("sqlite3_str*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* sqlite3_str_value([NativeTypeName("sqlite3_str*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_status(int op, int* pCurrent, int* pHighwater, int resetFlag);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_status64(int op, [NativeTypeName("sqlite3_int64 *")] long* pCurrent, [NativeTypeName("sqlite3_int64 *")] long* pHighwater, int resetFlag);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_db_status([NativeTypeName("sqlite3*")] nint param0, int op, int* pCur, int* pHiwtr, int resetFlg);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_db_status64([NativeTypeName("sqlite3*")] nint param0, int param1, [NativeTypeName("sqlite3_int64 *")] long* param2, [NativeTypeName("sqlite3_int64 *")] long* param3, int param4);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_stmt_status([NativeTypeName("sqlite3_stmt*")] nint param0, int op, int resetFlg);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("sqlite3_backup*")]
        public static extern nint sqlite3_backup_init([NativeTypeName("sqlite3*")] nint pDest, [NativeTypeName("const char *")] byte* zDestName, [NativeTypeName("sqlite3*")] nint pSource, [NativeTypeName("const char *")] byte* zSourceName);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_backup_step([NativeTypeName("sqlite3_backup*")] nint p, int nPage);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_backup_finish([NativeTypeName("sqlite3_backup*")] nint p);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_backup_remaining([NativeTypeName("sqlite3_backup*")] nint p);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_backup_pagecount([NativeTypeName("sqlite3_backup*")] nint p);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_unlock_notify([NativeTypeName("sqlite3*")] nint pBlocked, [NativeTypeName("void (*)(void **, int)")] delegate* unmanaged[Cdecl]<void**, int, void> xNotify, void* pNotifyArg);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_stricmp([NativeTypeName("const char *")] byte* param0, [NativeTypeName("const char *")] byte* param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_strnicmp([NativeTypeName("const char *")] byte* param0, [NativeTypeName("const char *")] byte* param1, int param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_strglob([NativeTypeName("const char *")] byte* zGlob, [NativeTypeName("const char *")] byte* zStr);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_strlike([NativeTypeName("const char *")] byte* zGlob, [NativeTypeName("const char *")] byte* zStr, [NativeTypeName("unsigned int")] uint cEsc);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_log(int iErrCode, [NativeTypeName("const char *")] byte* zFormat, __arglist);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* sqlite3_wal_hook([NativeTypeName("sqlite3*")] nint param0, [NativeTypeName("int (*)(void *, sqlite3 *, const char *, int)")] delegate* unmanaged[Cdecl]<void*, nint, byte*, int, int> param1, void* param2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_wal_autocheckpoint([NativeTypeName("sqlite3*")] nint db, int N);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_wal_checkpoint([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zDb);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_wal_checkpoint_v2([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zDb, int eMode, int* pnLog, int* pnCkpt);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_vtab_config([NativeTypeName("sqlite3*")] nint param0, int op, __arglist);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_vtab_on_conflict([NativeTypeName("sqlite3*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_vtab_nochange([NativeTypeName("sqlite3_context*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* sqlite3_vtab_collation([NativeTypeName("sqlite3_index_info*")] nint param0, int param1);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_vtab_distinct([NativeTypeName("sqlite3_index_info*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_vtab_in([NativeTypeName("sqlite3_index_info*")] nint param0, int iCons, int bHandle);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_vtab_in_first([NativeTypeName("sqlite3_value*")] nint pVal, [NativeTypeName("sqlite3_value **")] nint* ppOut);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_vtab_in_next([NativeTypeName("sqlite3_value*")] nint pVal, [NativeTypeName("sqlite3_value **")] nint* ppOut);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_vtab_rhs_value([NativeTypeName("sqlite3_index_info*")] nint param0, int param1, [NativeTypeName("sqlite3_value **")] nint* ppVal);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_stmt_scanstatus([NativeTypeName("sqlite3_stmt*")] nint pStmt, int idx, int iScanStatusOp, void* pOut);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_stmt_scanstatus_v2([NativeTypeName("sqlite3_stmt*")] nint pStmt, int idx, int iScanStatusOp, int flags, void* pOut);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_stmt_scanstatus_reset([NativeTypeName("sqlite3_stmt*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_db_cacheflush([NativeTypeName("sqlite3*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_system_errno([NativeTypeName("sqlite3*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_snapshot_get([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zSchema, [NativeTypeName("sqlite3_snapshot **")] nint* ppSnapshot);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_snapshot_open([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zSchema, [NativeTypeName("sqlite3_snapshot*")] nint pSnapshot);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void sqlite3_snapshot_free([NativeTypeName("sqlite3_snapshot*")] nint param0);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_snapshot_cmp([NativeTypeName("sqlite3_snapshot*")] nint p1, [NativeTypeName("sqlite3_snapshot*")] nint p2);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_snapshot_recover([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zDb);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned char *")]
        public static extern byte* sqlite3_serialize([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zSchema, [NativeTypeName("sqlite3_int64 *")] long* piSize, [NativeTypeName("unsigned int")] uint mFlags);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_deserialize([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zSchema, [NativeTypeName("unsigned char *")] byte* pData, [NativeTypeName("sqlite3_int64")] long szDb, [NativeTypeName("sqlite3_int64")] long szBuf, [NativeTypeName("unsigned int")] uint mFlags);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_carray_bind([NativeTypeName("sqlite3_stmt*")] nint pStmt, int i, void* aData, int nData, int mFlags, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> xDel);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_rtree_geometry_callback([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zGeom, [NativeTypeName("int (*)(sqlite3_rtree_geometry *, int, sqlite3_rtree_dbl *, int *)")] delegate* unmanaged[Cdecl]<nint, int, double*, int*, int> xGeom, void* pContext);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int sqlite3_rtree_query_callback([NativeTypeName("sqlite3*")] nint db, [NativeTypeName("const char *")] byte* zQueryFunc, [NativeTypeName("int (*)(sqlite3_rtree_query_info *)")] delegate* unmanaged[Cdecl]<nint, int> xQueryFunc, void* pContext, [NativeTypeName("void (*)(void *)")] delegate* unmanaged[Cdecl]<void*, void> xDestructor);

        [NativeTypeName("#define SQLITE_VERSION \"3.51.2\"")]
        public static ReadOnlySpan<byte> SQLITE_VERSION => new byte[] { 0x33, 0x2E, 0x35, 0x31, 0x2E, 0x32, 0x00 };

        [NativeTypeName("#define SQLITE_VERSION_NUMBER 3051002")]
        public const int SQLITE_VERSION_NUMBER = 3051002;

        [NativeTypeName("#define SQLITE_SOURCE_ID \"2026-01-09 17:27:48 b270f8339eb13b504d0b2ba154ebca966b7dde08e40c3ed7d559749818cb2075\"")]
        public static ReadOnlySpan<byte> SQLITE_SOURCE_ID => new byte[] { 0x32, 0x30, 0x32, 0x36, 0x2D, 0x30, 0x31, 0x2D, 0x30, 0x39, 0x20, 0x31, 0x37, 0x3A, 0x32, 0x37, 0x3A, 0x34, 0x38, 0x20, 0x62, 0x32, 0x37, 0x30, 0x66, 0x38, 0x33, 0x33, 0x39, 0x65, 0x62, 0x31, 0x33, 0x62, 0x35, 0x30, 0x34, 0x64, 0x30, 0x62, 0x32, 0x62, 0x61, 0x31, 0x35, 0x34, 0x65, 0x62, 0x63, 0x61, 0x39, 0x36, 0x36, 0x62, 0x37, 0x64, 0x64, 0x65, 0x30, 0x38, 0x65, 0x34, 0x30, 0x63, 0x33, 0x65, 0x64, 0x37, 0x64, 0x35, 0x35, 0x39, 0x37, 0x34, 0x39, 0x38, 0x31, 0x38, 0x63, 0x62, 0x32, 0x30, 0x37, 0x35, 0x00 };

        [NativeTypeName("#define SQLITE_SCM_BRANCH \"branch-3.51\"")]
        public static ReadOnlySpan<byte> SQLITE_SCM_BRANCH => new byte[] { 0x62, 0x72, 0x61, 0x6E, 0x63, 0x68, 0x2D, 0x33, 0x2E, 0x35, 0x31, 0x00 };

        [NativeTypeName("#define SQLITE_SCM_TAGS \"release version-3.51.2\"")]
        public static ReadOnlySpan<byte> SQLITE_SCM_TAGS => new byte[] { 0x72, 0x65, 0x6C, 0x65, 0x61, 0x73, 0x65, 0x20, 0x76, 0x65, 0x72, 0x73, 0x69, 0x6F, 0x6E, 0x2D, 0x33, 0x2E, 0x35, 0x31, 0x2E, 0x32, 0x00 };

        [NativeTypeName("#define SQLITE_SCM_DATETIME \"2026-01-09T17:27:48.405Z\"")]
        public static ReadOnlySpan<byte> SQLITE_SCM_DATETIME => new byte[] { 0x32, 0x30, 0x32, 0x36, 0x2D, 0x30, 0x31, 0x2D, 0x30, 0x39, 0x54, 0x31, 0x37, 0x3A, 0x32, 0x37, 0x3A, 0x34, 0x38, 0x2E, 0x34, 0x30, 0x35, 0x5A, 0x00 };

        [NativeTypeName("#define SQLITE_OK 0")]
        public const int SQLITE_OK = 0;

        [NativeTypeName("#define SQLITE_ERROR 1")]
        public const int SQLITE_ERROR = 1;

        [NativeTypeName("#define SQLITE_INTERNAL 2")]
        public const int SQLITE_INTERNAL = 2;

        [NativeTypeName("#define SQLITE_PERM 3")]
        public const int SQLITE_PERM = 3;

        [NativeTypeName("#define SQLITE_ABORT 4")]
        public const int SQLITE_ABORT = 4;

        [NativeTypeName("#define SQLITE_BUSY 5")]
        public const int SQLITE_BUSY = 5;

        [NativeTypeName("#define SQLITE_LOCKED 6")]
        public const int SQLITE_LOCKED = 6;

        [NativeTypeName("#define SQLITE_NOMEM 7")]
        public const int SQLITE_NOMEM = 7;

        [NativeTypeName("#define SQLITE_READONLY 8")]
        public const int SQLITE_READONLY = 8;

        [NativeTypeName("#define SQLITE_INTERRUPT 9")]
        public const int SQLITE_INTERRUPT = 9;

        [NativeTypeName("#define SQLITE_IOERR 10")]
        public const int SQLITE_IOERR = 10;

        [NativeTypeName("#define SQLITE_CORRUPT 11")]
        public const int SQLITE_CORRUPT = 11;

        [NativeTypeName("#define SQLITE_NOTFOUND 12")]
        public const int SQLITE_NOTFOUND = 12;

        [NativeTypeName("#define SQLITE_FULL 13")]
        public const int SQLITE_FULL = 13;

        [NativeTypeName("#define SQLITE_CANTOPEN 14")]
        public const int SQLITE_CANTOPEN = 14;

        [NativeTypeName("#define SQLITE_PROTOCOL 15")]
        public const int SQLITE_PROTOCOL = 15;

        [NativeTypeName("#define SQLITE_EMPTY 16")]
        public const int SQLITE_EMPTY = 16;

        [NativeTypeName("#define SQLITE_SCHEMA 17")]
        public const int SQLITE_SCHEMA = 17;

        [NativeTypeName("#define SQLITE_TOOBIG 18")]
        public const int SQLITE_TOOBIG = 18;

        [NativeTypeName("#define SQLITE_CONSTRAINT 19")]
        public const int SQLITE_CONSTRAINT = 19;

        [NativeTypeName("#define SQLITE_MISMATCH 20")]
        public const int SQLITE_MISMATCH = 20;

        [NativeTypeName("#define SQLITE_MISUSE 21")]
        public const int SQLITE_MISUSE = 21;

        [NativeTypeName("#define SQLITE_NOLFS 22")]
        public const int SQLITE_NOLFS = 22;

        [NativeTypeName("#define SQLITE_AUTH 23")]
        public const int SQLITE_AUTH = 23;

        [NativeTypeName("#define SQLITE_FORMAT 24")]
        public const int SQLITE_FORMAT = 24;

        [NativeTypeName("#define SQLITE_RANGE 25")]
        public const int SQLITE_RANGE = 25;

        [NativeTypeName("#define SQLITE_NOTADB 26")]
        public const int SQLITE_NOTADB = 26;

        [NativeTypeName("#define SQLITE_NOTICE 27")]
        public const int SQLITE_NOTICE = 27;

        [NativeTypeName("#define SQLITE_WARNING 28")]
        public const int SQLITE_WARNING = 28;

        [NativeTypeName("#define SQLITE_ROW 100")]
        public const int SQLITE_ROW = 100;

        [NativeTypeName("#define SQLITE_DONE 101")]
        public const int SQLITE_DONE = 101;

        [NativeTypeName("#define SQLITE_ERROR_MISSING_COLLSEQ (SQLITE_ERROR | (1<<8))")]
        public const int SQLITE_ERROR_MISSING_COLLSEQ = (1 | (1 << 8));

        [NativeTypeName("#define SQLITE_ERROR_RETRY (SQLITE_ERROR | (2<<8))")]
        public const int SQLITE_ERROR_RETRY = (1 | (2 << 8));

        [NativeTypeName("#define SQLITE_ERROR_SNAPSHOT (SQLITE_ERROR | (3<<8))")]
        public const int SQLITE_ERROR_SNAPSHOT = (1 | (3 << 8));

        [NativeTypeName("#define SQLITE_ERROR_RESERVESIZE (SQLITE_ERROR | (4<<8))")]
        public const int SQLITE_ERROR_RESERVESIZE = (1 | (4 << 8));

        [NativeTypeName("#define SQLITE_ERROR_KEY (SQLITE_ERROR | (5<<8))")]
        public const int SQLITE_ERROR_KEY = (1 | (5 << 8));

        [NativeTypeName("#define SQLITE_ERROR_UNABLE (SQLITE_ERROR | (6<<8))")]
        public const int SQLITE_ERROR_UNABLE = (1 | (6 << 8));

        [NativeTypeName("#define SQLITE_IOERR_READ (SQLITE_IOERR | (1<<8))")]
        public const int SQLITE_IOERR_READ = (10 | (1 << 8));

        [NativeTypeName("#define SQLITE_IOERR_SHORT_READ (SQLITE_IOERR | (2<<8))")]
        public const int SQLITE_IOERR_SHORT_READ = (10 | (2 << 8));

        [NativeTypeName("#define SQLITE_IOERR_WRITE (SQLITE_IOERR | (3<<8))")]
        public const int SQLITE_IOERR_WRITE = (10 | (3 << 8));

        [NativeTypeName("#define SQLITE_IOERR_FSYNC (SQLITE_IOERR | (4<<8))")]
        public const int SQLITE_IOERR_FSYNC = (10 | (4 << 8));

        [NativeTypeName("#define SQLITE_IOERR_DIR_FSYNC (SQLITE_IOERR | (5<<8))")]
        public const int SQLITE_IOERR_DIR_FSYNC = (10 | (5 << 8));

        [NativeTypeName("#define SQLITE_IOERR_TRUNCATE (SQLITE_IOERR | (6<<8))")]
        public const int SQLITE_IOERR_TRUNCATE = (10 | (6 << 8));

        [NativeTypeName("#define SQLITE_IOERR_FSTAT (SQLITE_IOERR | (7<<8))")]
        public const int SQLITE_IOERR_FSTAT = (10 | (7 << 8));

        [NativeTypeName("#define SQLITE_IOERR_UNLOCK (SQLITE_IOERR | (8<<8))")]
        public const int SQLITE_IOERR_UNLOCK = (10 | (8 << 8));

        [NativeTypeName("#define SQLITE_IOERR_RDLOCK (SQLITE_IOERR | (9<<8))")]
        public const int SQLITE_IOERR_RDLOCK = (10 | (9 << 8));

        [NativeTypeName("#define SQLITE_IOERR_DELETE (SQLITE_IOERR | (10<<8))")]
        public const int SQLITE_IOERR_DELETE = (10 | (10 << 8));

        [NativeTypeName("#define SQLITE_IOERR_BLOCKED (SQLITE_IOERR | (11<<8))")]
        public const int SQLITE_IOERR_BLOCKED = (10 | (11 << 8));

        [NativeTypeName("#define SQLITE_IOERR_NOMEM (SQLITE_IOERR | (12<<8))")]
        public const int SQLITE_IOERR_NOMEM = (10 | (12 << 8));

        [NativeTypeName("#define SQLITE_IOERR_ACCESS (SQLITE_IOERR | (13<<8))")]
        public const int SQLITE_IOERR_ACCESS = (10 | (13 << 8));

        [NativeTypeName("#define SQLITE_IOERR_CHECKRESERVEDLOCK (SQLITE_IOERR | (14<<8))")]
        public const int SQLITE_IOERR_CHECKRESERVEDLOCK = (10 | (14 << 8));

        [NativeTypeName("#define SQLITE_IOERR_LOCK (SQLITE_IOERR | (15<<8))")]
        public const int SQLITE_IOERR_LOCK = (10 | (15 << 8));

        [NativeTypeName("#define SQLITE_IOERR_CLOSE (SQLITE_IOERR | (16<<8))")]
        public const int SQLITE_IOERR_CLOSE = (10 | (16 << 8));

        [NativeTypeName("#define SQLITE_IOERR_DIR_CLOSE (SQLITE_IOERR | (17<<8))")]
        public const int SQLITE_IOERR_DIR_CLOSE = (10 | (17 << 8));

        [NativeTypeName("#define SQLITE_IOERR_SHMOPEN (SQLITE_IOERR | (18<<8))")]
        public const int SQLITE_IOERR_SHMOPEN = (10 | (18 << 8));

        [NativeTypeName("#define SQLITE_IOERR_SHMSIZE (SQLITE_IOERR | (19<<8))")]
        public const int SQLITE_IOERR_SHMSIZE = (10 | (19 << 8));

        [NativeTypeName("#define SQLITE_IOERR_SHMLOCK (SQLITE_IOERR | (20<<8))")]
        public const int SQLITE_IOERR_SHMLOCK = (10 | (20 << 8));

        [NativeTypeName("#define SQLITE_IOERR_SHMMAP (SQLITE_IOERR | (21<<8))")]
        public const int SQLITE_IOERR_SHMMAP = (10 | (21 << 8));

        [NativeTypeName("#define SQLITE_IOERR_SEEK (SQLITE_IOERR | (22<<8))")]
        public const int SQLITE_IOERR_SEEK = (10 | (22 << 8));

        [NativeTypeName("#define SQLITE_IOERR_DELETE_NOENT (SQLITE_IOERR | (23<<8))")]
        public const int SQLITE_IOERR_DELETE_NOENT = (10 | (23 << 8));

        [NativeTypeName("#define SQLITE_IOERR_MMAP (SQLITE_IOERR | (24<<8))")]
        public const int SQLITE_IOERR_MMAP = (10 | (24 << 8));

        [NativeTypeName("#define SQLITE_IOERR_GETTEMPPATH (SQLITE_IOERR | (25<<8))")]
        public const int SQLITE_IOERR_GETTEMPPATH = (10 | (25 << 8));

        [NativeTypeName("#define SQLITE_IOERR_CONVPATH (SQLITE_IOERR | (26<<8))")]
        public const int SQLITE_IOERR_CONVPATH = (10 | (26 << 8));

        [NativeTypeName("#define SQLITE_IOERR_VNODE (SQLITE_IOERR | (27<<8))")]
        public const int SQLITE_IOERR_VNODE = (10 | (27 << 8));

        [NativeTypeName("#define SQLITE_IOERR_AUTH (SQLITE_IOERR | (28<<8))")]
        public const int SQLITE_IOERR_AUTH = (10 | (28 << 8));

        [NativeTypeName("#define SQLITE_IOERR_BEGIN_ATOMIC (SQLITE_IOERR | (29<<8))")]
        public const int SQLITE_IOERR_BEGIN_ATOMIC = (10 | (29 << 8));

        [NativeTypeName("#define SQLITE_IOERR_COMMIT_ATOMIC (SQLITE_IOERR | (30<<8))")]
        public const int SQLITE_IOERR_COMMIT_ATOMIC = (10 | (30 << 8));

        [NativeTypeName("#define SQLITE_IOERR_ROLLBACK_ATOMIC (SQLITE_IOERR | (31<<8))")]
        public const int SQLITE_IOERR_ROLLBACK_ATOMIC = (10 | (31 << 8));

        [NativeTypeName("#define SQLITE_IOERR_DATA (SQLITE_IOERR | (32<<8))")]
        public const int SQLITE_IOERR_DATA = (10 | (32 << 8));

        [NativeTypeName("#define SQLITE_IOERR_CORRUPTFS (SQLITE_IOERR | (33<<8))")]
        public const int SQLITE_IOERR_CORRUPTFS = (10 | (33 << 8));

        [NativeTypeName("#define SQLITE_IOERR_IN_PAGE (SQLITE_IOERR | (34<<8))")]
        public const int SQLITE_IOERR_IN_PAGE = (10 | (34 << 8));

        [NativeTypeName("#define SQLITE_IOERR_BADKEY (SQLITE_IOERR | (35<<8))")]
        public const int SQLITE_IOERR_BADKEY = (10 | (35 << 8));

        [NativeTypeName("#define SQLITE_IOERR_CODEC (SQLITE_IOERR | (36<<8))")]
        public const int SQLITE_IOERR_CODEC = (10 | (36 << 8));

        [NativeTypeName("#define SQLITE_LOCKED_SHAREDCACHE (SQLITE_LOCKED |  (1<<8))")]
        public const int SQLITE_LOCKED_SHAREDCACHE = (6 | (1 << 8));

        [NativeTypeName("#define SQLITE_LOCKED_VTAB (SQLITE_LOCKED |  (2<<8))")]
        public const int SQLITE_LOCKED_VTAB = (6 | (2 << 8));

        [NativeTypeName("#define SQLITE_BUSY_RECOVERY (SQLITE_BUSY   |  (1<<8))")]
        public const int SQLITE_BUSY_RECOVERY = (5 | (1 << 8));

        [NativeTypeName("#define SQLITE_BUSY_SNAPSHOT (SQLITE_BUSY   |  (2<<8))")]
        public const int SQLITE_BUSY_SNAPSHOT = (5 | (2 << 8));

        [NativeTypeName("#define SQLITE_BUSY_TIMEOUT (SQLITE_BUSY   |  (3<<8))")]
        public const int SQLITE_BUSY_TIMEOUT = (5 | (3 << 8));

        [NativeTypeName("#define SQLITE_CANTOPEN_NOTEMPDIR (SQLITE_CANTOPEN | (1<<8))")]
        public const int SQLITE_CANTOPEN_NOTEMPDIR = (14 | (1 << 8));

        [NativeTypeName("#define SQLITE_CANTOPEN_ISDIR (SQLITE_CANTOPEN | (2<<8))")]
        public const int SQLITE_CANTOPEN_ISDIR = (14 | (2 << 8));

        [NativeTypeName("#define SQLITE_CANTOPEN_FULLPATH (SQLITE_CANTOPEN | (3<<8))")]
        public const int SQLITE_CANTOPEN_FULLPATH = (14 | (3 << 8));

        [NativeTypeName("#define SQLITE_CANTOPEN_CONVPATH (SQLITE_CANTOPEN | (4<<8))")]
        public const int SQLITE_CANTOPEN_CONVPATH = (14 | (4 << 8));

        [NativeTypeName("#define SQLITE_CANTOPEN_DIRTYWAL (SQLITE_CANTOPEN | (5<<8))")]
        public const int SQLITE_CANTOPEN_DIRTYWAL = (14 | (5 << 8));

        [NativeTypeName("#define SQLITE_CANTOPEN_SYMLINK (SQLITE_CANTOPEN | (6<<8))")]
        public const int SQLITE_CANTOPEN_SYMLINK = (14 | (6 << 8));

        [NativeTypeName("#define SQLITE_CORRUPT_VTAB (SQLITE_CORRUPT | (1<<8))")]
        public const int SQLITE_CORRUPT_VTAB = (11 | (1 << 8));

        [NativeTypeName("#define SQLITE_CORRUPT_SEQUENCE (SQLITE_CORRUPT | (2<<8))")]
        public const int SQLITE_CORRUPT_SEQUENCE = (11 | (2 << 8));

        [NativeTypeName("#define SQLITE_CORRUPT_INDEX (SQLITE_CORRUPT | (3<<8))")]
        public const int SQLITE_CORRUPT_INDEX = (11 | (3 << 8));

        [NativeTypeName("#define SQLITE_READONLY_RECOVERY (SQLITE_READONLY | (1<<8))")]
        public const int SQLITE_READONLY_RECOVERY = (8 | (1 << 8));

        [NativeTypeName("#define SQLITE_READONLY_CANTLOCK (SQLITE_READONLY | (2<<8))")]
        public const int SQLITE_READONLY_CANTLOCK = (8 | (2 << 8));

        [NativeTypeName("#define SQLITE_READONLY_ROLLBACK (SQLITE_READONLY | (3<<8))")]
        public const int SQLITE_READONLY_ROLLBACK = (8 | (3 << 8));

        [NativeTypeName("#define SQLITE_READONLY_DBMOVED (SQLITE_READONLY | (4<<8))")]
        public const int SQLITE_READONLY_DBMOVED = (8 | (4 << 8));

        [NativeTypeName("#define SQLITE_READONLY_CANTINIT (SQLITE_READONLY | (5<<8))")]
        public const int SQLITE_READONLY_CANTINIT = (8 | (5 << 8));

        [NativeTypeName("#define SQLITE_READONLY_DIRECTORY (SQLITE_READONLY | (6<<8))")]
        public const int SQLITE_READONLY_DIRECTORY = (8 | (6 << 8));

        [NativeTypeName("#define SQLITE_ABORT_ROLLBACK (SQLITE_ABORT | (2<<8))")]
        public const int SQLITE_ABORT_ROLLBACK = (4 | (2 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_CHECK (SQLITE_CONSTRAINT | (1<<8))")]
        public const int SQLITE_CONSTRAINT_CHECK = (19 | (1 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_COMMITHOOK (SQLITE_CONSTRAINT | (2<<8))")]
        public const int SQLITE_CONSTRAINT_COMMITHOOK = (19 | (2 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_FOREIGNKEY (SQLITE_CONSTRAINT | (3<<8))")]
        public const int SQLITE_CONSTRAINT_FOREIGNKEY = (19 | (3 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_FUNCTION (SQLITE_CONSTRAINT | (4<<8))")]
        public const int SQLITE_CONSTRAINT_FUNCTION = (19 | (4 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_NOTNULL (SQLITE_CONSTRAINT | (5<<8))")]
        public const int SQLITE_CONSTRAINT_NOTNULL = (19 | (5 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_PRIMARYKEY (SQLITE_CONSTRAINT | (6<<8))")]
        public const int SQLITE_CONSTRAINT_PRIMARYKEY = (19 | (6 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_TRIGGER (SQLITE_CONSTRAINT | (7<<8))")]
        public const int SQLITE_CONSTRAINT_TRIGGER = (19 | (7 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_UNIQUE (SQLITE_CONSTRAINT | (8<<8))")]
        public const int SQLITE_CONSTRAINT_UNIQUE = (19 | (8 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_VTAB (SQLITE_CONSTRAINT | (9<<8))")]
        public const int SQLITE_CONSTRAINT_VTAB = (19 | (9 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_ROWID (SQLITE_CONSTRAINT |(10<<8))")]
        public const int SQLITE_CONSTRAINT_ROWID = (19 | (10 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_PINNED (SQLITE_CONSTRAINT |(11<<8))")]
        public const int SQLITE_CONSTRAINT_PINNED = (19 | (11 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_DATATYPE (SQLITE_CONSTRAINT |(12<<8))")]
        public const int SQLITE_CONSTRAINT_DATATYPE = (19 | (12 << 8));

        [NativeTypeName("#define SQLITE_NOTICE_RECOVER_WAL (SQLITE_NOTICE | (1<<8))")]
        public const int SQLITE_NOTICE_RECOVER_WAL = (27 | (1 << 8));

        [NativeTypeName("#define SQLITE_NOTICE_RECOVER_ROLLBACK (SQLITE_NOTICE | (2<<8))")]
        public const int SQLITE_NOTICE_RECOVER_ROLLBACK = (27 | (2 << 8));

        [NativeTypeName("#define SQLITE_NOTICE_RBU (SQLITE_NOTICE | (3<<8))")]
        public const int SQLITE_NOTICE_RBU = (27 | (3 << 8));

        [NativeTypeName("#define SQLITE_WARNING_AUTOINDEX (SQLITE_WARNING | (1<<8))")]
        public const int SQLITE_WARNING_AUTOINDEX = (28 | (1 << 8));

        [NativeTypeName("#define SQLITE_AUTH_USER (SQLITE_AUTH | (1<<8))")]
        public const int SQLITE_AUTH_USER = (23 | (1 << 8));

        [NativeTypeName("#define SQLITE_OK_LOAD_PERMANENTLY (SQLITE_OK | (1<<8))")]
        public const int SQLITE_OK_LOAD_PERMANENTLY = (0 | (1 << 8));

        [NativeTypeName("#define SQLITE_OK_SYMLINK (SQLITE_OK | (2<<8))")]
        public const int SQLITE_OK_SYMLINK = (0 | (2 << 8));

        [NativeTypeName("#define SQLITE_OPEN_READONLY 0x00000001")]
        public const int SQLITE_OPEN_READONLY = 0x00000001;

        [NativeTypeName("#define SQLITE_OPEN_READWRITE 0x00000002")]
        public const int SQLITE_OPEN_READWRITE = 0x00000002;

        [NativeTypeName("#define SQLITE_OPEN_CREATE 0x00000004")]
        public const int SQLITE_OPEN_CREATE = 0x00000004;

        [NativeTypeName("#define SQLITE_OPEN_DELETEONCLOSE 0x00000008")]
        public const int SQLITE_OPEN_DELETEONCLOSE = 0x00000008;

        [NativeTypeName("#define SQLITE_OPEN_EXCLUSIVE 0x00000010")]
        public const int SQLITE_OPEN_EXCLUSIVE = 0x00000010;

        [NativeTypeName("#define SQLITE_OPEN_AUTOPROXY 0x00000020")]
        public const int SQLITE_OPEN_AUTOPROXY = 0x00000020;

        [NativeTypeName("#define SQLITE_OPEN_URI 0x00000040")]
        public const int SQLITE_OPEN_URI = 0x00000040;

        [NativeTypeName("#define SQLITE_OPEN_MEMORY 0x00000080")]
        public const int SQLITE_OPEN_MEMORY = 0x00000080;

        [NativeTypeName("#define SQLITE_OPEN_MAIN_DB 0x00000100")]
        public const int SQLITE_OPEN_MAIN_DB = 0x00000100;

        [NativeTypeName("#define SQLITE_OPEN_TEMP_DB 0x00000200")]
        public const int SQLITE_OPEN_TEMP_DB = 0x00000200;

        [NativeTypeName("#define SQLITE_OPEN_TRANSIENT_DB 0x00000400")]
        public const int SQLITE_OPEN_TRANSIENT_DB = 0x00000400;

        [NativeTypeName("#define SQLITE_OPEN_MAIN_JOURNAL 0x00000800")]
        public const int SQLITE_OPEN_MAIN_JOURNAL = 0x00000800;

        [NativeTypeName("#define SQLITE_OPEN_TEMP_JOURNAL 0x00001000")]
        public const int SQLITE_OPEN_TEMP_JOURNAL = 0x00001000;

        [NativeTypeName("#define SQLITE_OPEN_SUBJOURNAL 0x00002000")]
        public const int SQLITE_OPEN_SUBJOURNAL = 0x00002000;

        [NativeTypeName("#define SQLITE_OPEN_SUPER_JOURNAL 0x00004000")]
        public const int SQLITE_OPEN_SUPER_JOURNAL = 0x00004000;

        [NativeTypeName("#define SQLITE_OPEN_NOMUTEX 0x00008000")]
        public const int SQLITE_OPEN_NOMUTEX = 0x00008000;

        [NativeTypeName("#define SQLITE_OPEN_FULLMUTEX 0x00010000")]
        public const int SQLITE_OPEN_FULLMUTEX = 0x00010000;

        [NativeTypeName("#define SQLITE_OPEN_SHAREDCACHE 0x00020000")]
        public const int SQLITE_OPEN_SHAREDCACHE = 0x00020000;

        [NativeTypeName("#define SQLITE_OPEN_PRIVATECACHE 0x00040000")]
        public const int SQLITE_OPEN_PRIVATECACHE = 0x00040000;

        [NativeTypeName("#define SQLITE_OPEN_WAL 0x00080000")]
        public const int SQLITE_OPEN_WAL = 0x00080000;

        [NativeTypeName("#define SQLITE_OPEN_NOFOLLOW 0x01000000")]
        public const int SQLITE_OPEN_NOFOLLOW = 0x01000000;

        [NativeTypeName("#define SQLITE_OPEN_EXRESCODE 0x02000000")]
        public const int SQLITE_OPEN_EXRESCODE = 0x02000000;

        [NativeTypeName("#define SQLITE_OPEN_MASTER_JOURNAL 0x00004000")]
        public const int SQLITE_OPEN_MASTER_JOURNAL = 0x00004000;

        [NativeTypeName("#define SQLITE_IOCAP_ATOMIC 0x00000001")]
        public const int SQLITE_IOCAP_ATOMIC = 0x00000001;

        [NativeTypeName("#define SQLITE_IOCAP_ATOMIC512 0x00000002")]
        public const int SQLITE_IOCAP_ATOMIC512 = 0x00000002;

        [NativeTypeName("#define SQLITE_IOCAP_ATOMIC1K 0x00000004")]
        public const int SQLITE_IOCAP_ATOMIC1K = 0x00000004;

        [NativeTypeName("#define SQLITE_IOCAP_ATOMIC2K 0x00000008")]
        public const int SQLITE_IOCAP_ATOMIC2K = 0x00000008;

        [NativeTypeName("#define SQLITE_IOCAP_ATOMIC4K 0x00000010")]
        public const int SQLITE_IOCAP_ATOMIC4K = 0x00000010;

        [NativeTypeName("#define SQLITE_IOCAP_ATOMIC8K 0x00000020")]
        public const int SQLITE_IOCAP_ATOMIC8K = 0x00000020;

        [NativeTypeName("#define SQLITE_IOCAP_ATOMIC16K 0x00000040")]
        public const int SQLITE_IOCAP_ATOMIC16K = 0x00000040;

        [NativeTypeName("#define SQLITE_IOCAP_ATOMIC32K 0x00000080")]
        public const int SQLITE_IOCAP_ATOMIC32K = 0x00000080;

        [NativeTypeName("#define SQLITE_IOCAP_ATOMIC64K 0x00000100")]
        public const int SQLITE_IOCAP_ATOMIC64K = 0x00000100;

        [NativeTypeName("#define SQLITE_IOCAP_SAFE_APPEND 0x00000200")]
        public const int SQLITE_IOCAP_SAFE_APPEND = 0x00000200;

        [NativeTypeName("#define SQLITE_IOCAP_SEQUENTIAL 0x00000400")]
        public const int SQLITE_IOCAP_SEQUENTIAL = 0x00000400;

        [NativeTypeName("#define SQLITE_IOCAP_UNDELETABLE_WHEN_OPEN 0x00000800")]
        public const int SQLITE_IOCAP_UNDELETABLE_WHEN_OPEN = 0x00000800;

        [NativeTypeName("#define SQLITE_IOCAP_POWERSAFE_OVERWRITE 0x00001000")]
        public const int SQLITE_IOCAP_POWERSAFE_OVERWRITE = 0x00001000;

        [NativeTypeName("#define SQLITE_IOCAP_IMMUTABLE 0x00002000")]
        public const int SQLITE_IOCAP_IMMUTABLE = 0x00002000;

        [NativeTypeName("#define SQLITE_IOCAP_BATCH_ATOMIC 0x00004000")]
        public const int SQLITE_IOCAP_BATCH_ATOMIC = 0x00004000;

        [NativeTypeName("#define SQLITE_IOCAP_SUBPAGE_READ 0x00008000")]
        public const int SQLITE_IOCAP_SUBPAGE_READ = 0x00008000;

        [NativeTypeName("#define SQLITE_LOCK_NONE 0")]
        public const int SQLITE_LOCK_NONE = 0;

        [NativeTypeName("#define SQLITE_LOCK_SHARED 1")]
        public const int SQLITE_LOCK_SHARED = 1;

        [NativeTypeName("#define SQLITE_LOCK_RESERVED 2")]
        public const int SQLITE_LOCK_RESERVED = 2;

        [NativeTypeName("#define SQLITE_LOCK_PENDING 3")]
        public const int SQLITE_LOCK_PENDING = 3;

        [NativeTypeName("#define SQLITE_LOCK_EXCLUSIVE 4")]
        public const int SQLITE_LOCK_EXCLUSIVE = 4;

        [NativeTypeName("#define SQLITE_SYNC_NORMAL 0x00002")]
        public const int SQLITE_SYNC_NORMAL = 0x00002;

        [NativeTypeName("#define SQLITE_SYNC_FULL 0x00003")]
        public const int SQLITE_SYNC_FULL = 0x00003;

        [NativeTypeName("#define SQLITE_SYNC_DATAONLY 0x00010")]
        public const int SQLITE_SYNC_DATAONLY = 0x00010;

        [NativeTypeName("#define SQLITE_FCNTL_LOCKSTATE 1")]
        public const int SQLITE_FCNTL_LOCKSTATE = 1;

        [NativeTypeName("#define SQLITE_FCNTL_GET_LOCKPROXYFILE 2")]
        public const int SQLITE_FCNTL_GET_LOCKPROXYFILE = 2;

        [NativeTypeName("#define SQLITE_FCNTL_SET_LOCKPROXYFILE 3")]
        public const int SQLITE_FCNTL_SET_LOCKPROXYFILE = 3;

        [NativeTypeName("#define SQLITE_FCNTL_LAST_ERRNO 4")]
        public const int SQLITE_FCNTL_LAST_ERRNO = 4;

        [NativeTypeName("#define SQLITE_FCNTL_SIZE_HINT 5")]
        public const int SQLITE_FCNTL_SIZE_HINT = 5;

        [NativeTypeName("#define SQLITE_FCNTL_CHUNK_SIZE 6")]
        public const int SQLITE_FCNTL_CHUNK_SIZE = 6;

        [NativeTypeName("#define SQLITE_FCNTL_FILE_POINTER 7")]
        public const int SQLITE_FCNTL_FILE_POINTER = 7;

        [NativeTypeName("#define SQLITE_FCNTL_SYNC_OMITTED 8")]
        public const int SQLITE_FCNTL_SYNC_OMITTED = 8;

        [NativeTypeName("#define SQLITE_FCNTL_WIN32_AV_RETRY 9")]
        public const int SQLITE_FCNTL_WIN32_AV_RETRY = 9;

        [NativeTypeName("#define SQLITE_FCNTL_PERSIST_WAL 10")]
        public const int SQLITE_FCNTL_PERSIST_WAL = 10;

        [NativeTypeName("#define SQLITE_FCNTL_OVERWRITE 11")]
        public const int SQLITE_FCNTL_OVERWRITE = 11;

        [NativeTypeName("#define SQLITE_FCNTL_VFSNAME 12")]
        public const int SQLITE_FCNTL_VFSNAME = 12;

        [NativeTypeName("#define SQLITE_FCNTL_POWERSAFE_OVERWRITE 13")]
        public const int SQLITE_FCNTL_POWERSAFE_OVERWRITE = 13;

        [NativeTypeName("#define SQLITE_FCNTL_PRAGMA 14")]
        public const int SQLITE_FCNTL_PRAGMA = 14;

        [NativeTypeName("#define SQLITE_FCNTL_BUSYHANDLER 15")]
        public const int SQLITE_FCNTL_BUSYHANDLER = 15;

        [NativeTypeName("#define SQLITE_FCNTL_TEMPFILENAME 16")]
        public const int SQLITE_FCNTL_TEMPFILENAME = 16;

        [NativeTypeName("#define SQLITE_FCNTL_MMAP_SIZE 18")]
        public const int SQLITE_FCNTL_MMAP_SIZE = 18;

        [NativeTypeName("#define SQLITE_FCNTL_TRACE 19")]
        public const int SQLITE_FCNTL_TRACE = 19;

        [NativeTypeName("#define SQLITE_FCNTL_HAS_MOVED 20")]
        public const int SQLITE_FCNTL_HAS_MOVED = 20;

        [NativeTypeName("#define SQLITE_FCNTL_SYNC 21")]
        public const int SQLITE_FCNTL_SYNC = 21;

        [NativeTypeName("#define SQLITE_FCNTL_COMMIT_PHASETWO 22")]
        public const int SQLITE_FCNTL_COMMIT_PHASETWO = 22;

        [NativeTypeName("#define SQLITE_FCNTL_WIN32_SET_HANDLE 23")]
        public const int SQLITE_FCNTL_WIN32_SET_HANDLE = 23;

        [NativeTypeName("#define SQLITE_FCNTL_WAL_BLOCK 24")]
        public const int SQLITE_FCNTL_WAL_BLOCK = 24;

        [NativeTypeName("#define SQLITE_FCNTL_ZIPVFS 25")]
        public const int SQLITE_FCNTL_ZIPVFS = 25;

        [NativeTypeName("#define SQLITE_FCNTL_RBU 26")]
        public const int SQLITE_FCNTL_RBU = 26;

        [NativeTypeName("#define SQLITE_FCNTL_VFS_POINTER 27")]
        public const int SQLITE_FCNTL_VFS_POINTER = 27;

        [NativeTypeName("#define SQLITE_FCNTL_JOURNAL_POINTER 28")]
        public const int SQLITE_FCNTL_JOURNAL_POINTER = 28;

        [NativeTypeName("#define SQLITE_FCNTL_WIN32_GET_HANDLE 29")]
        public const int SQLITE_FCNTL_WIN32_GET_HANDLE = 29;

        [NativeTypeName("#define SQLITE_FCNTL_PDB 30")]
        public const int SQLITE_FCNTL_PDB = 30;

        [NativeTypeName("#define SQLITE_FCNTL_BEGIN_ATOMIC_WRITE 31")]
        public const int SQLITE_FCNTL_BEGIN_ATOMIC_WRITE = 31;

        [NativeTypeName("#define SQLITE_FCNTL_COMMIT_ATOMIC_WRITE 32")]
        public const int SQLITE_FCNTL_COMMIT_ATOMIC_WRITE = 32;

        [NativeTypeName("#define SQLITE_FCNTL_ROLLBACK_ATOMIC_WRITE 33")]
        public const int SQLITE_FCNTL_ROLLBACK_ATOMIC_WRITE = 33;

        [NativeTypeName("#define SQLITE_FCNTL_LOCK_TIMEOUT 34")]
        public const int SQLITE_FCNTL_LOCK_TIMEOUT = 34;

        [NativeTypeName("#define SQLITE_FCNTL_DATA_VERSION 35")]
        public const int SQLITE_FCNTL_DATA_VERSION = 35;

        [NativeTypeName("#define SQLITE_FCNTL_SIZE_LIMIT 36")]
        public const int SQLITE_FCNTL_SIZE_LIMIT = 36;

        [NativeTypeName("#define SQLITE_FCNTL_CKPT_DONE 37")]
        public const int SQLITE_FCNTL_CKPT_DONE = 37;

        [NativeTypeName("#define SQLITE_FCNTL_RESERVE_BYTES 38")]
        public const int SQLITE_FCNTL_RESERVE_BYTES = 38;

        [NativeTypeName("#define SQLITE_FCNTL_CKPT_START 39")]
        public const int SQLITE_FCNTL_CKPT_START = 39;

        [NativeTypeName("#define SQLITE_FCNTL_EXTERNAL_READER 40")]
        public const int SQLITE_FCNTL_EXTERNAL_READER = 40;

        [NativeTypeName("#define SQLITE_FCNTL_CKSM_FILE 41")]
        public const int SQLITE_FCNTL_CKSM_FILE = 41;

        [NativeTypeName("#define SQLITE_FCNTL_RESET_CACHE 42")]
        public const int SQLITE_FCNTL_RESET_CACHE = 42;

        [NativeTypeName("#define SQLITE_FCNTL_NULL_IO 43")]
        public const int SQLITE_FCNTL_NULL_IO = 43;

        [NativeTypeName("#define SQLITE_FCNTL_BLOCK_ON_CONNECT 44")]
        public const int SQLITE_FCNTL_BLOCK_ON_CONNECT = 44;

        [NativeTypeName("#define SQLITE_FCNTL_FILESTAT 45")]
        public const int SQLITE_FCNTL_FILESTAT = 45;

        [NativeTypeName("#define SQLITE_GET_LOCKPROXYFILE SQLITE_FCNTL_GET_LOCKPROXYFILE")]
        public const int SQLITE_GET_LOCKPROXYFILE = 2;

        [NativeTypeName("#define SQLITE_SET_LOCKPROXYFILE SQLITE_FCNTL_SET_LOCKPROXYFILE")]
        public const int SQLITE_SET_LOCKPROXYFILE = 3;

        [NativeTypeName("#define SQLITE_LAST_ERRNO SQLITE_FCNTL_LAST_ERRNO")]
        public const int SQLITE_LAST_ERRNO = 4;

        [NativeTypeName("#define SQLITE_ACCESS_EXISTS 0")]
        public const int SQLITE_ACCESS_EXISTS = 0;

        [NativeTypeName("#define SQLITE_ACCESS_READWRITE 1")]
        public const int SQLITE_ACCESS_READWRITE = 1;

        [NativeTypeName("#define SQLITE_ACCESS_READ 2")]
        public const int SQLITE_ACCESS_READ = 2;

        [NativeTypeName("#define SQLITE_SHM_UNLOCK 1")]
        public const int SQLITE_SHM_UNLOCK = 1;

        [NativeTypeName("#define SQLITE_SHM_LOCK 2")]
        public const int SQLITE_SHM_LOCK = 2;

        [NativeTypeName("#define SQLITE_SHM_SHARED 4")]
        public const int SQLITE_SHM_SHARED = 4;

        [NativeTypeName("#define SQLITE_SHM_EXCLUSIVE 8")]
        public const int SQLITE_SHM_EXCLUSIVE = 8;

        [NativeTypeName("#define SQLITE_SHM_NLOCK 8")]
        public const int SQLITE_SHM_NLOCK = 8;

        [NativeTypeName("#define SQLITE_CONFIG_SINGLETHREAD 1")]
        public const int SQLITE_CONFIG_SINGLETHREAD = 1;

        [NativeTypeName("#define SQLITE_CONFIG_MULTITHREAD 2")]
        public const int SQLITE_CONFIG_MULTITHREAD = 2;

        [NativeTypeName("#define SQLITE_CONFIG_SERIALIZED 3")]
        public const int SQLITE_CONFIG_SERIALIZED = 3;

        [NativeTypeName("#define SQLITE_CONFIG_MALLOC 4")]
        public const int SQLITE_CONFIG_MALLOC = 4;

        [NativeTypeName("#define SQLITE_CONFIG_GETMALLOC 5")]
        public const int SQLITE_CONFIG_GETMALLOC = 5;

        [NativeTypeName("#define SQLITE_CONFIG_SCRATCH 6")]
        public const int SQLITE_CONFIG_SCRATCH = 6;

        [NativeTypeName("#define SQLITE_CONFIG_PAGECACHE 7")]
        public const int SQLITE_CONFIG_PAGECACHE = 7;

        [NativeTypeName("#define SQLITE_CONFIG_HEAP 8")]
        public const int SQLITE_CONFIG_HEAP = 8;

        [NativeTypeName("#define SQLITE_CONFIG_MEMSTATUS 9")]
        public const int SQLITE_CONFIG_MEMSTATUS = 9;

        [NativeTypeName("#define SQLITE_CONFIG_MUTEX 10")]
        public const int SQLITE_CONFIG_MUTEX = 10;

        [NativeTypeName("#define SQLITE_CONFIG_GETMUTEX 11")]
        public const int SQLITE_CONFIG_GETMUTEX = 11;

        [NativeTypeName("#define SQLITE_CONFIG_LOOKASIDE 13")]
        public const int SQLITE_CONFIG_LOOKASIDE = 13;

        [NativeTypeName("#define SQLITE_CONFIG_PCACHE 14")]
        public const int SQLITE_CONFIG_PCACHE = 14;

        [NativeTypeName("#define SQLITE_CONFIG_GETPCACHE 15")]
        public const int SQLITE_CONFIG_GETPCACHE = 15;

        [NativeTypeName("#define SQLITE_CONFIG_LOG 16")]
        public const int SQLITE_CONFIG_LOG = 16;

        [NativeTypeName("#define SQLITE_CONFIG_URI 17")]
        public const int SQLITE_CONFIG_URI = 17;

        [NativeTypeName("#define SQLITE_CONFIG_PCACHE2 18")]
        public const int SQLITE_CONFIG_PCACHE2 = 18;

        [NativeTypeName("#define SQLITE_CONFIG_GETPCACHE2 19")]
        public const int SQLITE_CONFIG_GETPCACHE2 = 19;

        [NativeTypeName("#define SQLITE_CONFIG_COVERING_INDEX_SCAN 20")]
        public const int SQLITE_CONFIG_COVERING_INDEX_SCAN = 20;

        [NativeTypeName("#define SQLITE_CONFIG_SQLLOG 21")]
        public const int SQLITE_CONFIG_SQLLOG = 21;

        [NativeTypeName("#define SQLITE_CONFIG_MMAP_SIZE 22")]
        public const int SQLITE_CONFIG_MMAP_SIZE = 22;

        [NativeTypeName("#define SQLITE_CONFIG_WIN32_HEAPSIZE 23")]
        public const int SQLITE_CONFIG_WIN32_HEAPSIZE = 23;

        [NativeTypeName("#define SQLITE_CONFIG_PCACHE_HDRSZ 24")]
        public const int SQLITE_CONFIG_PCACHE_HDRSZ = 24;

        [NativeTypeName("#define SQLITE_CONFIG_PMASZ 25")]
        public const int SQLITE_CONFIG_PMASZ = 25;

        [NativeTypeName("#define SQLITE_CONFIG_STMTJRNL_SPILL 26")]
        public const int SQLITE_CONFIG_STMTJRNL_SPILL = 26;

        [NativeTypeName("#define SQLITE_CONFIG_SMALL_MALLOC 27")]
        public const int SQLITE_CONFIG_SMALL_MALLOC = 27;

        [NativeTypeName("#define SQLITE_CONFIG_SORTERREF_SIZE 28")]
        public const int SQLITE_CONFIG_SORTERREF_SIZE = 28;

        [NativeTypeName("#define SQLITE_CONFIG_MEMDB_MAXSIZE 29")]
        public const int SQLITE_CONFIG_MEMDB_MAXSIZE = 29;

        [NativeTypeName("#define SQLITE_CONFIG_ROWID_IN_VIEW 30")]
        public const int SQLITE_CONFIG_ROWID_IN_VIEW = 30;

        [NativeTypeName("#define SQLITE_DBCONFIG_MAINDBNAME 1000")]
        public const int SQLITE_DBCONFIG_MAINDBNAME = 1000;

        [NativeTypeName("#define SQLITE_DBCONFIG_LOOKASIDE 1001")]
        public const int SQLITE_DBCONFIG_LOOKASIDE = 1001;

        [NativeTypeName("#define SQLITE_DBCONFIG_ENABLE_FKEY 1002")]
        public const int SQLITE_DBCONFIG_ENABLE_FKEY = 1002;

        [NativeTypeName("#define SQLITE_DBCONFIG_ENABLE_TRIGGER 1003")]
        public const int SQLITE_DBCONFIG_ENABLE_TRIGGER = 1003;

        [NativeTypeName("#define SQLITE_DBCONFIG_ENABLE_FTS3_TOKENIZER 1004")]
        public const int SQLITE_DBCONFIG_ENABLE_FTS3_TOKENIZER = 1004;

        [NativeTypeName("#define SQLITE_DBCONFIG_ENABLE_LOAD_EXTENSION 1005")]
        public const int SQLITE_DBCONFIG_ENABLE_LOAD_EXTENSION = 1005;

        [NativeTypeName("#define SQLITE_DBCONFIG_NO_CKPT_ON_CLOSE 1006")]
        public const int SQLITE_DBCONFIG_NO_CKPT_ON_CLOSE = 1006;

        [NativeTypeName("#define SQLITE_DBCONFIG_ENABLE_QPSG 1007")]
        public const int SQLITE_DBCONFIG_ENABLE_QPSG = 1007;

        [NativeTypeName("#define SQLITE_DBCONFIG_TRIGGER_EQP 1008")]
        public const int SQLITE_DBCONFIG_TRIGGER_EQP = 1008;

        [NativeTypeName("#define SQLITE_DBCONFIG_RESET_DATABASE 1009")]
        public const int SQLITE_DBCONFIG_RESET_DATABASE = 1009;

        [NativeTypeName("#define SQLITE_DBCONFIG_DEFENSIVE 1010")]
        public const int SQLITE_DBCONFIG_DEFENSIVE = 1010;

        [NativeTypeName("#define SQLITE_DBCONFIG_WRITABLE_SCHEMA 1011")]
        public const int SQLITE_DBCONFIG_WRITABLE_SCHEMA = 1011;

        [NativeTypeName("#define SQLITE_DBCONFIG_LEGACY_ALTER_TABLE 1012")]
        public const int SQLITE_DBCONFIG_LEGACY_ALTER_TABLE = 1012;

        [NativeTypeName("#define SQLITE_DBCONFIG_DQS_DML 1013")]
        public const int SQLITE_DBCONFIG_DQS_DML = 1013;

        [NativeTypeName("#define SQLITE_DBCONFIG_DQS_DDL 1014")]
        public const int SQLITE_DBCONFIG_DQS_DDL = 1014;

        [NativeTypeName("#define SQLITE_DBCONFIG_ENABLE_VIEW 1015")]
        public const int SQLITE_DBCONFIG_ENABLE_VIEW = 1015;

        [NativeTypeName("#define SQLITE_DBCONFIG_LEGACY_FILE_FORMAT 1016")]
        public const int SQLITE_DBCONFIG_LEGACY_FILE_FORMAT = 1016;

        [NativeTypeName("#define SQLITE_DBCONFIG_TRUSTED_SCHEMA 1017")]
        public const int SQLITE_DBCONFIG_TRUSTED_SCHEMA = 1017;

        [NativeTypeName("#define SQLITE_DBCONFIG_STMT_SCANSTATUS 1018")]
        public const int SQLITE_DBCONFIG_STMT_SCANSTATUS = 1018;

        [NativeTypeName("#define SQLITE_DBCONFIG_REVERSE_SCANORDER 1019")]
        public const int SQLITE_DBCONFIG_REVERSE_SCANORDER = 1019;

        [NativeTypeName("#define SQLITE_DBCONFIG_ENABLE_ATTACH_CREATE 1020")]
        public const int SQLITE_DBCONFIG_ENABLE_ATTACH_CREATE = 1020;

        [NativeTypeName("#define SQLITE_DBCONFIG_ENABLE_ATTACH_WRITE 1021")]
        public const int SQLITE_DBCONFIG_ENABLE_ATTACH_WRITE = 1021;

        [NativeTypeName("#define SQLITE_DBCONFIG_ENABLE_COMMENTS 1022")]
        public const int SQLITE_DBCONFIG_ENABLE_COMMENTS = 1022;

        [NativeTypeName("#define SQLITE_DBCONFIG_MAX 1022")]
        public const int SQLITE_DBCONFIG_MAX = 1022;

        [NativeTypeName("#define SQLITE_SETLK_BLOCK_ON_CONNECT 0x01")]
        public const int SQLITE_SETLK_BLOCK_ON_CONNECT = 0x01;

        [NativeTypeName("#define SQLITE_DENY 1")]
        public const int SQLITE_DENY = 1;

        [NativeTypeName("#define SQLITE_IGNORE 2")]
        public const int SQLITE_IGNORE = 2;

        [NativeTypeName("#define SQLITE_CREATE_INDEX 1")]
        public const int SQLITE_CREATE_INDEX = 1;

        [NativeTypeName("#define SQLITE_CREATE_TABLE 2")]
        public const int SQLITE_CREATE_TABLE = 2;

        [NativeTypeName("#define SQLITE_CREATE_TEMP_INDEX 3")]
        public const int SQLITE_CREATE_TEMP_INDEX = 3;

        [NativeTypeName("#define SQLITE_CREATE_TEMP_TABLE 4")]
        public const int SQLITE_CREATE_TEMP_TABLE = 4;

        [NativeTypeName("#define SQLITE_CREATE_TEMP_TRIGGER 5")]
        public const int SQLITE_CREATE_TEMP_TRIGGER = 5;

        [NativeTypeName("#define SQLITE_CREATE_TEMP_VIEW 6")]
        public const int SQLITE_CREATE_TEMP_VIEW = 6;

        [NativeTypeName("#define SQLITE_CREATE_TRIGGER 7")]
        public const int SQLITE_CREATE_TRIGGER = 7;

        [NativeTypeName("#define SQLITE_CREATE_VIEW 8")]
        public const int SQLITE_CREATE_VIEW = 8;

        [NativeTypeName("#define SQLITE_DELETE 9")]
        public const int SQLITE_DELETE = 9;

        [NativeTypeName("#define SQLITE_DROP_INDEX 10")]
        public const int SQLITE_DROP_INDEX = 10;

        [NativeTypeName("#define SQLITE_DROP_TABLE 11")]
        public const int SQLITE_DROP_TABLE = 11;

        [NativeTypeName("#define SQLITE_DROP_TEMP_INDEX 12")]
        public const int SQLITE_DROP_TEMP_INDEX = 12;

        [NativeTypeName("#define SQLITE_DROP_TEMP_TABLE 13")]
        public const int SQLITE_DROP_TEMP_TABLE = 13;

        [NativeTypeName("#define SQLITE_DROP_TEMP_TRIGGER 14")]
        public const int SQLITE_DROP_TEMP_TRIGGER = 14;

        [NativeTypeName("#define SQLITE_DROP_TEMP_VIEW 15")]
        public const int SQLITE_DROP_TEMP_VIEW = 15;

        [NativeTypeName("#define SQLITE_DROP_TRIGGER 16")]
        public const int SQLITE_DROP_TRIGGER = 16;

        [NativeTypeName("#define SQLITE_DROP_VIEW 17")]
        public const int SQLITE_DROP_VIEW = 17;

        [NativeTypeName("#define SQLITE_INSERT 18")]
        public const int SQLITE_INSERT = 18;

        [NativeTypeName("#define SQLITE_PRAGMA 19")]
        public const int SQLITE_PRAGMA = 19;

        [NativeTypeName("#define SQLITE_READ 20")]
        public const int SQLITE_READ = 20;

        [NativeTypeName("#define SQLITE_SELECT 21")]
        public const int SQLITE_SELECT = 21;

        [NativeTypeName("#define SQLITE_TRANSACTION 22")]
        public const int SQLITE_TRANSACTION = 22;

        [NativeTypeName("#define SQLITE_UPDATE 23")]
        public const int SQLITE_UPDATE = 23;

        [NativeTypeName("#define SQLITE_ATTACH 24")]
        public const int SQLITE_ATTACH = 24;

        [NativeTypeName("#define SQLITE_DETACH 25")]
        public const int SQLITE_DETACH = 25;

        [NativeTypeName("#define SQLITE_ALTER_TABLE 26")]
        public const int SQLITE_ALTER_TABLE = 26;

        [NativeTypeName("#define SQLITE_REINDEX 27")]
        public const int SQLITE_REINDEX = 27;

        [NativeTypeName("#define SQLITE_ANALYZE 28")]
        public const int SQLITE_ANALYZE = 28;

        [NativeTypeName("#define SQLITE_CREATE_VTABLE 29")]
        public const int SQLITE_CREATE_VTABLE = 29;

        [NativeTypeName("#define SQLITE_DROP_VTABLE 30")]
        public const int SQLITE_DROP_VTABLE = 30;

        [NativeTypeName("#define SQLITE_FUNCTION 31")]
        public const int SQLITE_FUNCTION = 31;

        [NativeTypeName("#define SQLITE_SAVEPOINT 32")]
        public const int SQLITE_SAVEPOINT = 32;

        [NativeTypeName("#define SQLITE_COPY 0")]
        public const int SQLITE_COPY = 0;

        [NativeTypeName("#define SQLITE_RECURSIVE 33")]
        public const int SQLITE_RECURSIVE = 33;

        [NativeTypeName("#define SQLITE_TRACE_STMT 0x01")]
        public const int SQLITE_TRACE_STMT = 0x01;

        [NativeTypeName("#define SQLITE_TRACE_PROFILE 0x02")]
        public const int SQLITE_TRACE_PROFILE = 0x02;

        [NativeTypeName("#define SQLITE_TRACE_ROW 0x04")]
        public const int SQLITE_TRACE_ROW = 0x04;

        [NativeTypeName("#define SQLITE_TRACE_CLOSE 0x08")]
        public const int SQLITE_TRACE_CLOSE = 0x08;

        [NativeTypeName("#define SQLITE_LIMIT_LENGTH 0")]
        public const int SQLITE_LIMIT_LENGTH = 0;

        [NativeTypeName("#define SQLITE_LIMIT_SQL_LENGTH 1")]
        public const int SQLITE_LIMIT_SQL_LENGTH = 1;

        [NativeTypeName("#define SQLITE_LIMIT_COLUMN 2")]
        public const int SQLITE_LIMIT_COLUMN = 2;

        [NativeTypeName("#define SQLITE_LIMIT_EXPR_DEPTH 3")]
        public const int SQLITE_LIMIT_EXPR_DEPTH = 3;

        [NativeTypeName("#define SQLITE_LIMIT_COMPOUND_SELECT 4")]
        public const int SQLITE_LIMIT_COMPOUND_SELECT = 4;

        [NativeTypeName("#define SQLITE_LIMIT_VDBE_OP 5")]
        public const int SQLITE_LIMIT_VDBE_OP = 5;

        [NativeTypeName("#define SQLITE_LIMIT_FUNCTION_ARG 6")]
        public const int SQLITE_LIMIT_FUNCTION_ARG = 6;

        [NativeTypeName("#define SQLITE_LIMIT_ATTACHED 7")]
        public const int SQLITE_LIMIT_ATTACHED = 7;

        [NativeTypeName("#define SQLITE_LIMIT_LIKE_PATTERN_LENGTH 8")]
        public const int SQLITE_LIMIT_LIKE_PATTERN_LENGTH = 8;

        [NativeTypeName("#define SQLITE_LIMIT_VARIABLE_NUMBER 9")]
        public const int SQLITE_LIMIT_VARIABLE_NUMBER = 9;

        [NativeTypeName("#define SQLITE_LIMIT_TRIGGER_DEPTH 10")]
        public const int SQLITE_LIMIT_TRIGGER_DEPTH = 10;

        [NativeTypeName("#define SQLITE_LIMIT_WORKER_THREADS 11")]
        public const int SQLITE_LIMIT_WORKER_THREADS = 11;

        [NativeTypeName("#define SQLITE_PREPARE_PERSISTENT 0x01")]
        public const int SQLITE_PREPARE_PERSISTENT = 0x01;

        [NativeTypeName("#define SQLITE_PREPARE_NORMALIZE 0x02")]
        public const int SQLITE_PREPARE_NORMALIZE = 0x02;

        [NativeTypeName("#define SQLITE_PREPARE_NO_VTAB 0x04")]
        public const int SQLITE_PREPARE_NO_VTAB = 0x04;

        [NativeTypeName("#define SQLITE_PREPARE_DONT_LOG 0x10")]
        public const int SQLITE_PREPARE_DONT_LOG = 0x10;

        [NativeTypeName("#define SQLITE_INTEGER 1")]
        public const int SQLITE_INTEGER = 1;

        [NativeTypeName("#define SQLITE_FLOAT 2")]
        public const int SQLITE_FLOAT = 2;

        [NativeTypeName("#define SQLITE_BLOB 4")]
        public const int SQLITE_BLOB = 4;

        [NativeTypeName("#define SQLITE_NULL 5")]
        public const int SQLITE_NULL = 5;

        [NativeTypeName("#define SQLITE_TEXT 3")]
        public const int SQLITE_TEXT = 3;

        [NativeTypeName("#define SQLITE3_TEXT 3")]
        public const int SQLITE3_TEXT = 3;

        [NativeTypeName("#define SQLITE_UTF8 1")]
        public const int SQLITE_UTF8 = 1;

        [NativeTypeName("#define SQLITE_UTF16LE 2")]
        public const int SQLITE_UTF16LE = 2;

        [NativeTypeName("#define SQLITE_UTF16BE 3")]
        public const int SQLITE_UTF16BE = 3;

        [NativeTypeName("#define SQLITE_UTF16 4")]
        public const int SQLITE_UTF16 = 4;

        [NativeTypeName("#define SQLITE_ANY 5")]
        public const int SQLITE_ANY = 5;

        [NativeTypeName("#define SQLITE_UTF16_ALIGNED 8")]
        public const int SQLITE_UTF16_ALIGNED = 8;

        [NativeTypeName("#define SQLITE_DETERMINISTIC 0x000000800")]
        public const int SQLITE_DETERMINISTIC = 0x000000800;

        [NativeTypeName("#define SQLITE_DIRECTONLY 0x000080000")]
        public const int SQLITE_DIRECTONLY = 0x000080000;

        [NativeTypeName("#define SQLITE_SUBTYPE 0x000100000")]
        public const int SQLITE_SUBTYPE = 0x000100000;

        [NativeTypeName("#define SQLITE_INNOCUOUS 0x000200000")]
        public const int SQLITE_INNOCUOUS = 0x000200000;

        [NativeTypeName("#define SQLITE_RESULT_SUBTYPE 0x001000000")]
        public const int SQLITE_RESULT_SUBTYPE = 0x001000000;

        [NativeTypeName("#define SQLITE_SELFORDER1 0x002000000")]
        public const int SQLITE_SELFORDER1 = 0x002000000;

        [NativeTypeName("#define SQLITE_STATIC ((sqlite3_destructor_type)0)")]
        public static readonly delegate* unmanaged[Cdecl]<void*, void> SQLITE_STATIC = ((delegate* unmanaged[Cdecl]<void*, void>)(0));

        [NativeTypeName("#define SQLITE_TRANSIENT ((sqlite3_destructor_type)-1)")]
        public static readonly delegate* unmanaged[Cdecl]<void*, void> SQLITE_TRANSIENT = ((delegate* unmanaged[Cdecl]<void*, void>)(-1));

        [NativeTypeName("#define SQLITE_WIN32_DATA_DIRECTORY_TYPE 1")]
        public const int SQLITE_WIN32_DATA_DIRECTORY_TYPE = 1;

        [NativeTypeName("#define SQLITE_WIN32_TEMP_DIRECTORY_TYPE 2")]
        public const int SQLITE_WIN32_TEMP_DIRECTORY_TYPE = 2;

        [NativeTypeName("#define SQLITE_TXN_NONE 0")]
        public const int SQLITE_TXN_NONE = 0;

        [NativeTypeName("#define SQLITE_TXN_READ 1")]
        public const int SQLITE_TXN_READ = 1;

        [NativeTypeName("#define SQLITE_TXN_WRITE 2")]
        public const int SQLITE_TXN_WRITE = 2;

        [NativeTypeName("#define SQLITE_INDEX_SCAN_UNIQUE 0x00000001")]
        public const int SQLITE_INDEX_SCAN_UNIQUE = 0x00000001;

        [NativeTypeName("#define SQLITE_INDEX_SCAN_HEX 0x00000002")]
        public const int SQLITE_INDEX_SCAN_HEX = 0x00000002;

        [NativeTypeName("#define SQLITE_INDEX_CONSTRAINT_EQ 2")]
        public const int SQLITE_INDEX_CONSTRAINT_EQ = 2;

        [NativeTypeName("#define SQLITE_INDEX_CONSTRAINT_GT 4")]
        public const int SQLITE_INDEX_CONSTRAINT_GT = 4;

        [NativeTypeName("#define SQLITE_INDEX_CONSTRAINT_LE 8")]
        public const int SQLITE_INDEX_CONSTRAINT_LE = 8;

        [NativeTypeName("#define SQLITE_INDEX_CONSTRAINT_LT 16")]
        public const int SQLITE_INDEX_CONSTRAINT_LT = 16;

        [NativeTypeName("#define SQLITE_INDEX_CONSTRAINT_GE 32")]
        public const int SQLITE_INDEX_CONSTRAINT_GE = 32;

        [NativeTypeName("#define SQLITE_INDEX_CONSTRAINT_MATCH 64")]
        public const int SQLITE_INDEX_CONSTRAINT_MATCH = 64;

        [NativeTypeName("#define SQLITE_INDEX_CONSTRAINT_LIKE 65")]
        public const int SQLITE_INDEX_CONSTRAINT_LIKE = 65;

        [NativeTypeName("#define SQLITE_INDEX_CONSTRAINT_GLOB 66")]
        public const int SQLITE_INDEX_CONSTRAINT_GLOB = 66;

        [NativeTypeName("#define SQLITE_INDEX_CONSTRAINT_REGEXP 67")]
        public const int SQLITE_INDEX_CONSTRAINT_REGEXP = 67;

        [NativeTypeName("#define SQLITE_INDEX_CONSTRAINT_NE 68")]
        public const int SQLITE_INDEX_CONSTRAINT_NE = 68;

        [NativeTypeName("#define SQLITE_INDEX_CONSTRAINT_ISNOT 69")]
        public const int SQLITE_INDEX_CONSTRAINT_ISNOT = 69;

        [NativeTypeName("#define SQLITE_INDEX_CONSTRAINT_ISNOTNULL 70")]
        public const int SQLITE_INDEX_CONSTRAINT_ISNOTNULL = 70;

        [NativeTypeName("#define SQLITE_INDEX_CONSTRAINT_ISNULL 71")]
        public const int SQLITE_INDEX_CONSTRAINT_ISNULL = 71;

        [NativeTypeName("#define SQLITE_INDEX_CONSTRAINT_IS 72")]
        public const int SQLITE_INDEX_CONSTRAINT_IS = 72;

        [NativeTypeName("#define SQLITE_INDEX_CONSTRAINT_LIMIT 73")]
        public const int SQLITE_INDEX_CONSTRAINT_LIMIT = 73;

        [NativeTypeName("#define SQLITE_INDEX_CONSTRAINT_OFFSET 74")]
        public const int SQLITE_INDEX_CONSTRAINT_OFFSET = 74;

        [NativeTypeName("#define SQLITE_INDEX_CONSTRAINT_FUNCTION 150")]
        public const int SQLITE_INDEX_CONSTRAINT_FUNCTION = 150;

        [NativeTypeName("#define SQLITE_MUTEX_FAST 0")]
        public const int SQLITE_MUTEX_FAST = 0;

        [NativeTypeName("#define SQLITE_MUTEX_RECURSIVE 1")]
        public const int SQLITE_MUTEX_RECURSIVE = 1;

        [NativeTypeName("#define SQLITE_MUTEX_STATIC_MAIN 2")]
        public const int SQLITE_MUTEX_STATIC_MAIN = 2;

        [NativeTypeName("#define SQLITE_MUTEX_STATIC_MEM 3")]
        public const int SQLITE_MUTEX_STATIC_MEM = 3;

        [NativeTypeName("#define SQLITE_MUTEX_STATIC_MEM2 4")]
        public const int SQLITE_MUTEX_STATIC_MEM2 = 4;

        [NativeTypeName("#define SQLITE_MUTEX_STATIC_OPEN 4")]
        public const int SQLITE_MUTEX_STATIC_OPEN = 4;

        [NativeTypeName("#define SQLITE_MUTEX_STATIC_PRNG 5")]
        public const int SQLITE_MUTEX_STATIC_PRNG = 5;

        [NativeTypeName("#define SQLITE_MUTEX_STATIC_LRU 6")]
        public const int SQLITE_MUTEX_STATIC_LRU = 6;

        [NativeTypeName("#define SQLITE_MUTEX_STATIC_LRU2 7")]
        public const int SQLITE_MUTEX_STATIC_LRU2 = 7;

        [NativeTypeName("#define SQLITE_MUTEX_STATIC_PMEM 7")]
        public const int SQLITE_MUTEX_STATIC_PMEM = 7;

        [NativeTypeName("#define SQLITE_MUTEX_STATIC_APP1 8")]
        public const int SQLITE_MUTEX_STATIC_APP1 = 8;

        [NativeTypeName("#define SQLITE_MUTEX_STATIC_APP2 9")]
        public const int SQLITE_MUTEX_STATIC_APP2 = 9;

        [NativeTypeName("#define SQLITE_MUTEX_STATIC_APP3 10")]
        public const int SQLITE_MUTEX_STATIC_APP3 = 10;

        [NativeTypeName("#define SQLITE_MUTEX_STATIC_VFS1 11")]
        public const int SQLITE_MUTEX_STATIC_VFS1 = 11;

        [NativeTypeName("#define SQLITE_MUTEX_STATIC_VFS2 12")]
        public const int SQLITE_MUTEX_STATIC_VFS2 = 12;

        [NativeTypeName("#define SQLITE_MUTEX_STATIC_VFS3 13")]
        public const int SQLITE_MUTEX_STATIC_VFS3 = 13;

        [NativeTypeName("#define SQLITE_MUTEX_STATIC_MASTER 2")]
        public const int SQLITE_MUTEX_STATIC_MASTER = 2;

        [NativeTypeName("#define SQLITE_TESTCTRL_FIRST 5")]
        public const int SQLITE_TESTCTRL_FIRST = 5;

        [NativeTypeName("#define SQLITE_TESTCTRL_PRNG_SAVE 5")]
        public const int SQLITE_TESTCTRL_PRNG_SAVE = 5;

        [NativeTypeName("#define SQLITE_TESTCTRL_PRNG_RESTORE 6")]
        public const int SQLITE_TESTCTRL_PRNG_RESTORE = 6;

        [NativeTypeName("#define SQLITE_TESTCTRL_PRNG_RESET 7")]
        public const int SQLITE_TESTCTRL_PRNG_RESET = 7;

        [NativeTypeName("#define SQLITE_TESTCTRL_FK_NO_ACTION 7")]
        public const int SQLITE_TESTCTRL_FK_NO_ACTION = 7;

        [NativeTypeName("#define SQLITE_TESTCTRL_BITVEC_TEST 8")]
        public const int SQLITE_TESTCTRL_BITVEC_TEST = 8;

        [NativeTypeName("#define SQLITE_TESTCTRL_FAULT_INSTALL 9")]
        public const int SQLITE_TESTCTRL_FAULT_INSTALL = 9;

        [NativeTypeName("#define SQLITE_TESTCTRL_BENIGN_MALLOC_HOOKS 10")]
        public const int SQLITE_TESTCTRL_BENIGN_MALLOC_HOOKS = 10;

        [NativeTypeName("#define SQLITE_TESTCTRL_PENDING_BYTE 11")]
        public const int SQLITE_TESTCTRL_PENDING_BYTE = 11;

        [NativeTypeName("#define SQLITE_TESTCTRL_ASSERT 12")]
        public const int SQLITE_TESTCTRL_ASSERT = 12;

        [NativeTypeName("#define SQLITE_TESTCTRL_ALWAYS 13")]
        public const int SQLITE_TESTCTRL_ALWAYS = 13;

        [NativeTypeName("#define SQLITE_TESTCTRL_RESERVE 14")]
        public const int SQLITE_TESTCTRL_RESERVE = 14;

        [NativeTypeName("#define SQLITE_TESTCTRL_JSON_SELFCHECK 14")]
        public const int SQLITE_TESTCTRL_JSON_SELFCHECK = 14;

        [NativeTypeName("#define SQLITE_TESTCTRL_OPTIMIZATIONS 15")]
        public const int SQLITE_TESTCTRL_OPTIMIZATIONS = 15;

        [NativeTypeName("#define SQLITE_TESTCTRL_ISKEYWORD 16")]
        public const int SQLITE_TESTCTRL_ISKEYWORD = 16;

        [NativeTypeName("#define SQLITE_TESTCTRL_GETOPT 16")]
        public const int SQLITE_TESTCTRL_GETOPT = 16;

        [NativeTypeName("#define SQLITE_TESTCTRL_SCRATCHMALLOC 17")]
        public const int SQLITE_TESTCTRL_SCRATCHMALLOC = 17;

        [NativeTypeName("#define SQLITE_TESTCTRL_INTERNAL_FUNCTIONS 17")]
        public const int SQLITE_TESTCTRL_INTERNAL_FUNCTIONS = 17;

        [NativeTypeName("#define SQLITE_TESTCTRL_LOCALTIME_FAULT 18")]
        public const int SQLITE_TESTCTRL_LOCALTIME_FAULT = 18;

        [NativeTypeName("#define SQLITE_TESTCTRL_EXPLAIN_STMT 19")]
        public const int SQLITE_TESTCTRL_EXPLAIN_STMT = 19;

        [NativeTypeName("#define SQLITE_TESTCTRL_ONCE_RESET_THRESHOLD 19")]
        public const int SQLITE_TESTCTRL_ONCE_RESET_THRESHOLD = 19;

        [NativeTypeName("#define SQLITE_TESTCTRL_NEVER_CORRUPT 20")]
        public const int SQLITE_TESTCTRL_NEVER_CORRUPT = 20;

        [NativeTypeName("#define SQLITE_TESTCTRL_VDBE_COVERAGE 21")]
        public const int SQLITE_TESTCTRL_VDBE_COVERAGE = 21;

        [NativeTypeName("#define SQLITE_TESTCTRL_BYTEORDER 22")]
        public const int SQLITE_TESTCTRL_BYTEORDER = 22;

        [NativeTypeName("#define SQLITE_TESTCTRL_ISINIT 23")]
        public const int SQLITE_TESTCTRL_ISINIT = 23;

        [NativeTypeName("#define SQLITE_TESTCTRL_SORTER_MMAP 24")]
        public const int SQLITE_TESTCTRL_SORTER_MMAP = 24;

        [NativeTypeName("#define SQLITE_TESTCTRL_IMPOSTER 25")]
        public const int SQLITE_TESTCTRL_IMPOSTER = 25;

        [NativeTypeName("#define SQLITE_TESTCTRL_PARSER_COVERAGE 26")]
        public const int SQLITE_TESTCTRL_PARSER_COVERAGE = 26;

        [NativeTypeName("#define SQLITE_TESTCTRL_RESULT_INTREAL 27")]
        public const int SQLITE_TESTCTRL_RESULT_INTREAL = 27;

        [NativeTypeName("#define SQLITE_TESTCTRL_PRNG_SEED 28")]
        public const int SQLITE_TESTCTRL_PRNG_SEED = 28;

        [NativeTypeName("#define SQLITE_TESTCTRL_EXTRA_SCHEMA_CHECKS 29")]
        public const int SQLITE_TESTCTRL_EXTRA_SCHEMA_CHECKS = 29;

        [NativeTypeName("#define SQLITE_TESTCTRL_SEEK_COUNT 30")]
        public const int SQLITE_TESTCTRL_SEEK_COUNT = 30;

        [NativeTypeName("#define SQLITE_TESTCTRL_TRACEFLAGS 31")]
        public const int SQLITE_TESTCTRL_TRACEFLAGS = 31;

        [NativeTypeName("#define SQLITE_TESTCTRL_TUNE 32")]
        public const int SQLITE_TESTCTRL_TUNE = 32;

        [NativeTypeName("#define SQLITE_TESTCTRL_LOGEST 33")]
        public const int SQLITE_TESTCTRL_LOGEST = 33;

        [NativeTypeName("#define SQLITE_TESTCTRL_USELONGDOUBLE 34")]
        public const int SQLITE_TESTCTRL_USELONGDOUBLE = 34;

        [NativeTypeName("#define SQLITE_TESTCTRL_LAST 34")]
        public const int SQLITE_TESTCTRL_LAST = 34;

        [NativeTypeName("#define SQLITE_STATUS_MEMORY_USED 0")]
        public const int SQLITE_STATUS_MEMORY_USED = 0;

        [NativeTypeName("#define SQLITE_STATUS_PAGECACHE_USED 1")]
        public const int SQLITE_STATUS_PAGECACHE_USED = 1;

        [NativeTypeName("#define SQLITE_STATUS_PAGECACHE_OVERFLOW 2")]
        public const int SQLITE_STATUS_PAGECACHE_OVERFLOW = 2;

        [NativeTypeName("#define SQLITE_STATUS_SCRATCH_USED 3")]
        public const int SQLITE_STATUS_SCRATCH_USED = 3;

        [NativeTypeName("#define SQLITE_STATUS_SCRATCH_OVERFLOW 4")]
        public const int SQLITE_STATUS_SCRATCH_OVERFLOW = 4;

        [NativeTypeName("#define SQLITE_STATUS_MALLOC_SIZE 5")]
        public const int SQLITE_STATUS_MALLOC_SIZE = 5;

        [NativeTypeName("#define SQLITE_STATUS_PARSER_STACK 6")]
        public const int SQLITE_STATUS_PARSER_STACK = 6;

        [NativeTypeName("#define SQLITE_STATUS_PAGECACHE_SIZE 7")]
        public const int SQLITE_STATUS_PAGECACHE_SIZE = 7;

        [NativeTypeName("#define SQLITE_STATUS_SCRATCH_SIZE 8")]
        public const int SQLITE_STATUS_SCRATCH_SIZE = 8;

        [NativeTypeName("#define SQLITE_STATUS_MALLOC_COUNT 9")]
        public const int SQLITE_STATUS_MALLOC_COUNT = 9;

        [NativeTypeName("#define SQLITE_DBSTATUS_LOOKASIDE_USED 0")]
        public const int SQLITE_DBSTATUS_LOOKASIDE_USED = 0;

        [NativeTypeName("#define SQLITE_DBSTATUS_CACHE_USED 1")]
        public const int SQLITE_DBSTATUS_CACHE_USED = 1;

        [NativeTypeName("#define SQLITE_DBSTATUS_SCHEMA_USED 2")]
        public const int SQLITE_DBSTATUS_SCHEMA_USED = 2;

        [NativeTypeName("#define SQLITE_DBSTATUS_STMT_USED 3")]
        public const int SQLITE_DBSTATUS_STMT_USED = 3;

        [NativeTypeName("#define SQLITE_DBSTATUS_LOOKASIDE_HIT 4")]
        public const int SQLITE_DBSTATUS_LOOKASIDE_HIT = 4;

        [NativeTypeName("#define SQLITE_DBSTATUS_LOOKASIDE_MISS_SIZE 5")]
        public const int SQLITE_DBSTATUS_LOOKASIDE_MISS_SIZE = 5;

        [NativeTypeName("#define SQLITE_DBSTATUS_LOOKASIDE_MISS_FULL 6")]
        public const int SQLITE_DBSTATUS_LOOKASIDE_MISS_FULL = 6;

        [NativeTypeName("#define SQLITE_DBSTATUS_CACHE_HIT 7")]
        public const int SQLITE_DBSTATUS_CACHE_HIT = 7;

        [NativeTypeName("#define SQLITE_DBSTATUS_CACHE_MISS 8")]
        public const int SQLITE_DBSTATUS_CACHE_MISS = 8;

        [NativeTypeName("#define SQLITE_DBSTATUS_CACHE_WRITE 9")]
        public const int SQLITE_DBSTATUS_CACHE_WRITE = 9;

        [NativeTypeName("#define SQLITE_DBSTATUS_DEFERRED_FKS 10")]
        public const int SQLITE_DBSTATUS_DEFERRED_FKS = 10;

        [NativeTypeName("#define SQLITE_DBSTATUS_CACHE_USED_SHARED 11")]
        public const int SQLITE_DBSTATUS_CACHE_USED_SHARED = 11;

        [NativeTypeName("#define SQLITE_DBSTATUS_CACHE_SPILL 12")]
        public const int SQLITE_DBSTATUS_CACHE_SPILL = 12;

        [NativeTypeName("#define SQLITE_DBSTATUS_TEMPBUF_SPILL 13")]
        public const int SQLITE_DBSTATUS_TEMPBUF_SPILL = 13;

        [NativeTypeName("#define SQLITE_DBSTATUS_MAX 13")]
        public const int SQLITE_DBSTATUS_MAX = 13;

        [NativeTypeName("#define SQLITE_STMTSTATUS_FULLSCAN_STEP 1")]
        public const int SQLITE_STMTSTATUS_FULLSCAN_STEP = 1;

        [NativeTypeName("#define SQLITE_STMTSTATUS_SORT 2")]
        public const int SQLITE_STMTSTATUS_SORT = 2;

        [NativeTypeName("#define SQLITE_STMTSTATUS_AUTOINDEX 3")]
        public const int SQLITE_STMTSTATUS_AUTOINDEX = 3;

        [NativeTypeName("#define SQLITE_STMTSTATUS_VM_STEP 4")]
        public const int SQLITE_STMTSTATUS_VM_STEP = 4;

        [NativeTypeName("#define SQLITE_STMTSTATUS_REPREPARE 5")]
        public const int SQLITE_STMTSTATUS_REPREPARE = 5;

        [NativeTypeName("#define SQLITE_STMTSTATUS_RUN 6")]
        public const int SQLITE_STMTSTATUS_RUN = 6;

        [NativeTypeName("#define SQLITE_STMTSTATUS_FILTER_MISS 7")]
        public const int SQLITE_STMTSTATUS_FILTER_MISS = 7;

        [NativeTypeName("#define SQLITE_STMTSTATUS_FILTER_HIT 8")]
        public const int SQLITE_STMTSTATUS_FILTER_HIT = 8;

        [NativeTypeName("#define SQLITE_STMTSTATUS_MEMUSED 99")]
        public const int SQLITE_STMTSTATUS_MEMUSED = 99;

        [NativeTypeName("#define SQLITE_CHECKPOINT_NOOP -1")]
        public const int SQLITE_CHECKPOINT_NOOP = -1;

        [NativeTypeName("#define SQLITE_CHECKPOINT_PASSIVE 0")]
        public const int SQLITE_CHECKPOINT_PASSIVE = 0;

        [NativeTypeName("#define SQLITE_CHECKPOINT_FULL 1")]
        public const int SQLITE_CHECKPOINT_FULL = 1;

        [NativeTypeName("#define SQLITE_CHECKPOINT_RESTART 2")]
        public const int SQLITE_CHECKPOINT_RESTART = 2;

        [NativeTypeName("#define SQLITE_CHECKPOINT_TRUNCATE 3")]
        public const int SQLITE_CHECKPOINT_TRUNCATE = 3;

        [NativeTypeName("#define SQLITE_VTAB_CONSTRAINT_SUPPORT 1")]
        public const int SQLITE_VTAB_CONSTRAINT_SUPPORT = 1;

        [NativeTypeName("#define SQLITE_VTAB_INNOCUOUS 2")]
        public const int SQLITE_VTAB_INNOCUOUS = 2;

        [NativeTypeName("#define SQLITE_VTAB_DIRECTONLY 3")]
        public const int SQLITE_VTAB_DIRECTONLY = 3;

        [NativeTypeName("#define SQLITE_VTAB_USES_ALL_SCHEMAS 4")]
        public const int SQLITE_VTAB_USES_ALL_SCHEMAS = 4;

        [NativeTypeName("#define SQLITE_ROLLBACK 1")]
        public const int SQLITE_ROLLBACK = 1;

        [NativeTypeName("#define SQLITE_FAIL 3")]
        public const int SQLITE_FAIL = 3;

        [NativeTypeName("#define SQLITE_REPLACE 5")]
        public const int SQLITE_REPLACE = 5;

        [NativeTypeName("#define SQLITE_SCANSTAT_NLOOP 0")]
        public const int SQLITE_SCANSTAT_NLOOP = 0;

        [NativeTypeName("#define SQLITE_SCANSTAT_NVISIT 1")]
        public const int SQLITE_SCANSTAT_NVISIT = 1;

        [NativeTypeName("#define SQLITE_SCANSTAT_EST 2")]
        public const int SQLITE_SCANSTAT_EST = 2;

        [NativeTypeName("#define SQLITE_SCANSTAT_NAME 3")]
        public const int SQLITE_SCANSTAT_NAME = 3;

        [NativeTypeName("#define SQLITE_SCANSTAT_EXPLAIN 4")]
        public const int SQLITE_SCANSTAT_EXPLAIN = 4;

        [NativeTypeName("#define SQLITE_SCANSTAT_SELECTID 5")]
        public const int SQLITE_SCANSTAT_SELECTID = 5;

        [NativeTypeName("#define SQLITE_SCANSTAT_PARENTID 6")]
        public const int SQLITE_SCANSTAT_PARENTID = 6;

        [NativeTypeName("#define SQLITE_SCANSTAT_NCYCLE 7")]
        public const int SQLITE_SCANSTAT_NCYCLE = 7;

        [NativeTypeName("#define SQLITE_SCANSTAT_COMPLEX 0x0001")]
        public const int SQLITE_SCANSTAT_COMPLEX = 0x0001;

        [NativeTypeName("#define SQLITE_SERIALIZE_NOCOPY 0x001")]
        public const int SQLITE_SERIALIZE_NOCOPY = 0x001;

        [NativeTypeName("#define SQLITE_DESERIALIZE_FREEONCLOSE 1")]
        public const int SQLITE_DESERIALIZE_FREEONCLOSE = 1;

        [NativeTypeName("#define SQLITE_DESERIALIZE_RESIZEABLE 2")]
        public const int SQLITE_DESERIALIZE_RESIZEABLE = 2;

        [NativeTypeName("#define SQLITE_DESERIALIZE_READONLY 4")]
        public const int SQLITE_DESERIALIZE_READONLY = 4;

        [NativeTypeName("#define SQLITE_CARRAY_INT32 0")]
        public const int SQLITE_CARRAY_INT32 = 0;

        [NativeTypeName("#define SQLITE_CARRAY_INT64 1")]
        public const int SQLITE_CARRAY_INT64 = 1;

        [NativeTypeName("#define SQLITE_CARRAY_DOUBLE 2")]
        public const int SQLITE_CARRAY_DOUBLE = 2;

        [NativeTypeName("#define SQLITE_CARRAY_TEXT 3")]
        public const int SQLITE_CARRAY_TEXT = 3;

        [NativeTypeName("#define SQLITE_CARRAY_BLOB 4")]
        public const int SQLITE_CARRAY_BLOB = 4;

        [NativeTypeName("#define CARRAY_INT32 0")]
        public const int CARRAY_INT32 = 0;

        [NativeTypeName("#define CARRAY_INT64 1")]
        public const int CARRAY_INT64 = 1;

        [NativeTypeName("#define CARRAY_DOUBLE 2")]
        public const int CARRAY_DOUBLE = 2;

        [NativeTypeName("#define CARRAY_TEXT 3")]
        public const int CARRAY_TEXT = 3;

        [NativeTypeName("#define CARRAY_BLOB 4")]
        public const int CARRAY_BLOB = 4;

        [NativeTypeName("#define NOT_WITHIN 0")]
        public const int NOT_WITHIN = 0;

        [NativeTypeName("#define PARTLY_WITHIN 1")]
        public const int PARTLY_WITHIN = 1;

        [NativeTypeName("#define FULLY_WITHIN 2")]
        public const int FULLY_WITHIN = 2;

        [NativeTypeName("#define FTS5_TOKENIZE_QUERY 0x0001")]
        public const int FTS5_TOKENIZE_QUERY = 0x0001;

        [NativeTypeName("#define FTS5_TOKENIZE_PREFIX 0x0002")]
        public const int FTS5_TOKENIZE_PREFIX = 0x0002;

        [NativeTypeName("#define FTS5_TOKENIZE_DOCUMENT 0x0004")]
        public const int FTS5_TOKENIZE_DOCUMENT = 0x0004;

        [NativeTypeName("#define FTS5_TOKENIZE_AUX 0x0008")]
        public const int FTS5_TOKENIZE_AUX = 0x0008;

        [NativeTypeName("#define FTS5_TOKEN_COLOCATED 0x0001")]
        public const int FTS5_TOKEN_COLOCATED = 0x0001;
    }
}
