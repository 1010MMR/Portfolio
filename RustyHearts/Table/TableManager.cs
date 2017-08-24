using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using MagiTable;

public class TableManager : MonoBehaviour {
    static TableManager instance_ = null;
    public static TableManager Instance {
        get {
            return instance_;
        }
    }
    public string tableUrl_;
    public delegate void LoadComplateCB(Table tabe);

    bool loadComplate_ = false;
    public bool IsLoadComplate {
        get {
            return loadComplate_;
        }
    }

    void Awake() {
        instance_ = this;
    }

    void Start() {
    }

    bool first_ = true;
    void Update() {
        if( PatchManager.Instance == null )
            return;
        if( PatchManager.Instance.IsLoadComplate == false )
            return;

        if( first_ == true ) {
            AddTableList();
            first_ = false;
        }

        if( tableList_ != null ) {
            if( hashTableName_.Count != 0 ) {
                foreach( var p in hashWaiting_ ) {
                    string tableName;
                    if( hashTableName_.TryGetValue(p.Key, out tableName) == false ) {
                        MagiDebug.Log(string.Format("table not found. {0}", p.Key));
                        continue;
                    }

                    StartCoroutine(OpenTable(p.Key, tableName, p.Value));
                }

                hashWaiting_.Clear();
            }
        }
    }

    public void GetTableList(out List<string> list) {
        list = new List<string>();
        var line = tableList_.Line;

        foreach( var id in line.Keys ) {
            string name;
            if( tableList_.GetString(out name, id, "szName", false) ) {
                list.Add(name);
            }
        }
    }

    public Table FindTable(string name) {
        string strlower = name.ToLower();

        Table table = new Table(strlower);
        if( hashTable_.TryGetValue(strlower, out table) == false ) {
            MagiDebug.Log(string.Format("Invalid TableName {0}", name));
            return null;
        }
        return table;
    }

    public string GetTableName(string name) {
        string name2, tableFile;
        foreach( var p in tableList_.Line ) {
            int nID = p.Key;
            tableList_.GetString(out name2, nID, "szName", false);
            tableList_.GetString(out tableFile, nID, "szTableFile", false);
            if( name == name2 )
                return tableFile;
        }
        return null;
    }

    public bool ReadString(out string result, BinaryReader binReader) {
        result = "";
        int nCharNum = binReader.ReadInt16();
        if( nCharNum == 0 ) {
            return true;
        }
        byte[] toByte = binReader.ReadBytes(nCharNum);
        result = System.Text.Encoding.ASCII.GetString(toByte);
        return true;
    }

    public bool ReadWString(out string result, BinaryReader binReader) {
        result = "";
        int nCharNum = binReader.ReadInt16();
        if( nCharNum == 0 ) {
            return true;
        }
        byte[] toByte = binReader.ReadBytes(nCharNum);
        result = System.Text.Encoding.Unicode.GetString(toByte);
        return true;
    }

    public bool ReadColumnName(int nColumnNum, ref Table table, BinaryReader binReader) {
        string strName = "";
        if( table == null )
            return false;

        try {
            table.ColumnName.Clear();
            for( int i = 0; i < nColumnNum; i++ ) {
                ReadString(out strName, binReader);
                table.ColumnName.Add(strName, i);
                strName = "";
            }
        } catch( Exception ex ) {
            MagiDebug.LogError(string.Format("{0}, {1}, {2}, {3}", table.Name, strName, nColumnNum, ex.Message));
        }
        return true;
    }

    public bool ReadColumnType(int nColumnNum, ref Table table, BinaryReader binReader) {
        Table.Type type;
        table.ColumnType.Clear();
        for( int i = 0; i < nColumnNum; i++ ) {
            type = (Table.Type)binReader.ReadInt32();
            table.ColumnType.Add(type);
        }
        return true;
    }

    public bool ReadData(int nColumnNum, ref Table table, BinaryReader binReader) {
        SLineTable lineTable = new SLineTable();
        try {
            foreach( var columnType in table.ColumnType ) {
                switch( columnType ) {
                    case Table.Type.INT: {
                            int nInt = binReader.ReadInt32();
                            lineTable.listIndex.Add(nInt);
                        }
                        break;
                    case Table.Type.FLOAT: {
                            float fFloat = binReader.ReadSingle();
                            lineTable.listFloat.Add(fFloat);
                            int nIdx = lineTable.listFloat.Count - 1;
                            lineTable.listIndex.Add(nIdx);
                        }
                        break;
                    case Table.Type.STRING: {
                            string strString;
                            ReadString(out strString, binReader);
                            lineTable.listString.Add(strString);
                            int nIdx = lineTable.listString.Count - 1;
                            lineTable.listIndex.Add(nIdx);
                        }
                        break;
                    case Table.Type.WSTRING: {
                            string strString;
                            ReadWString(out strString, binReader);
                            lineTable.listWString.Add(strString);
                            int nIdx = lineTable.listWString.Count - 1;
                            lineTable.listIndex.Add(nIdx);
                        }
                        break;
                    case Table.Type.INT64: {
                            long nInt64 = binReader.ReadInt64();
                            lineTable.listInt64.Add(nInt64);
                            int nIdx = lineTable.listInt64.Count - 1;
                            lineTable.listIndex.Add(nIdx);
                        }
                        break;
                };
            }

            if( lineTable.listIndex.Count == 0 ) {
                return false;
            }

            table.Line.Add(lineTable.listIndex[0], lineTable);
        } catch( Exception ex ) {
            MagiDebug.LogError(string.Format("{0}, {1}, {2}", table.Name, lineTable.listIndex[0], ex.Message));
        }
        return true;
    }

    IEnumerator OpenTable(string key, string tableName, LoadComplateCB lcCB) {
        string url = "";

        MemoryStream stream = null;
        switch( OptionManager.Instance.pathType ) {
            case OptionManager.EPathType.Develop: {
                    url = OptionManager.ROOT_PATH + "/" + tableUrl_ + "/bytes/" + tableName + ".bytes";

                    TextAsset bytes = Resources.LoadAssetAtPath(url, typeof(TextAsset)) as TextAsset;
                    if( bytes == null ) {
                        MagiDebug.Log(string.Format("{0}, bytes==null", url));
                        break;
                    }
                    stream = new MemoryStream(bytes.bytes);
                }
                break;
            case OptionManager.EPathType.Live: {
                    AssetBundle assetBundle;
                    if( PatchManager.Instance.FindAssetBundle("table", out assetBundle) ) {
                        TextAsset bytes = assetBundle.Load(key, typeof(TextAsset)) as TextAsset;
                        if( bytes == null ) {
                            MagiDebug.LogError(key + " table not found.");
                            yield break;
                        }

                        stream = new MemoryStream(bytes.bytes);
                    }
                }
                break;
        }

        if( stream == null ) {
            MagiDebug.Log(stream + "," + key + "," + tableName);
            yield break;
        }

        BinaryReader binReader = new BinaryReader(stream);
        Table table = new Table(key);

        int lineNum = binReader.ReadInt32();
        int columnNum = binReader.ReadInt32();

        if( ReadColumnName(columnNum, ref table, binReader) == false )
            yield break;
        if( ReadColumnType(columnNum, ref table, binReader) == false )
            yield break;

        for( int i = 0; i < lineNum; i++ ) {
            if( ReadData(columnNum, ref table, binReader) == false )
                yield break;
        }

        try {
            hashTable_.Add(key, table);
        } catch {
            MagiDebug.LogError(string.Format("Table key already {0}, {1}", key, tableName));
        }
        if( lcCB != null ) {
            lcCB(table);
        }
    }

    public bool AddTable(string key, LoadComplateCB lcCB) {
        if( tableList_ == null ) {
            try {
                hashWaiting_.Add(key, lcCB);
            } catch {
                MagiDebug.LogError(string.Format("Table key already {0}", key));
            }
            return true;
        }
        else {
            string tableName;
            if( hashTableName_.TryGetValue(key, out tableName) == false ) {
                if( key.Contains("weapontrail") || key.Contains("sound") ) {
                    MagiDebug.Log(string.Format("{0} table not found.", key));
                    return false;
                }

                MagiDebug.LogError(string.Format("{0} table not found.", key));
                return false;
            }
            StartCoroutine(OpenTable(key, tableName, lcCB));
        }
        return true;
    }

    public bool RemoveTable(string key) {
        if( hashTable_.ContainsKey(key) == false )
            return false;

        hashTable_.Remove(key);
        return true;
    }

    void AddTableList() {
        if( tableList_ == null ) {
            LoadComplateCB loadComplateCB = new LoadComplateCB(TableListLoadComplateCB);
            StartCoroutine(OpenTable("tablelist", "tablelist", loadComplateCB));
        }
    }

    void TableListLoadComplateCB(Table table) {
        if( table == null )
            return;
        tableList_ = table;
        string name, filename;

        foreach( var id in tableList_.Line.Keys ) {
            tableList_.GetString(out name, id, "szName", true);
            tableList_.GetString(out filename, id, "szTableFile", true);
            if( name == "" )
                continue;
            if( hashTableName_.ContainsKey(name) == true ) {
                MagiDebug.LogError(string.Format("tablelist already key = , {0}", name));
                continue;
            }
            hashTableName_.Add(name, filename);
        }
        loadComplate_ = true;
    }

    Dictionary<string, Table> hashTable_ = new Dictionary<string, Table>();
    Dictionary<string, string> hashTableName_ = new Dictionary<string, string>();
    Dictionary<string, LoadComplateCB> hashWaiting_ = new Dictionary<string, LoadComplateCB>();
    Table tableList_ = null;

    void OnDestroy() {
        instance_ = null;
        hashTable_ = null;
        hashTableName_ = null;
        hashWaiting_ = null;
        tableList_ = null;
    }
};
