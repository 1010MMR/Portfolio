using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MagiTable {

    public class SLineTable {
        public List<Int64> listInt64 = new List<Int64>();
        public List<string> listString = new List<string>();
        public List<string> listWString = new List<string>();
        public List<float> listFloat = new List<float>();
        public List<int> listIndex = new List<int>(); //int가 아닌경우는 index로 사용.

        ~SLineTable() {
            listInt64 = null;
            listString = null;
            listWString = null;
            listFloat = null;
            listIndex = null;
        }
    };

    public class Table {
        public enum Type {
            INT,
            FLOAT,
            STRING,
            WSTRING,
            INT64,
        };

        public Table(string name) {
            name_ = name;
        }

        public bool IsValidID(int nID) {
            return hashLine_.ContainsKey(nID);
        }

        public int GetLineNum() {
            return hashLine_.Count;
        }

        public int GetColumnNum() {
            return columnName_.Count;
        }

        public bool GetInt(out int value, int id, int index, string columnName, bool errorCheck) {
            value = 0;
            if( index >= columnType_.Count || columnType_[index] != Type.INT ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }

            SLineTable lineTable;
            if( hashLine_.TryGetValue(id, out lineTable) == false ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }

            if( index >= lineTable.listIndex.Count ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }

            value = lineTable.listIndex[index];
            return true;
        }

        public bool GetFloat(out float value, int id, int index, string columnName, bool errorCheck) {
            value = 0;
            if( index >= (int)columnType_.Count || columnType_[index] != Type.FLOAT ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }
            SLineTable lineTable;
            if( hashLine_.TryGetValue(id, out lineTable) == false ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }

            if( index >= lineTable.listIndex.Count ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }

            int nFloatIdx = lineTable.listIndex[index];
            if( nFloatIdx >= lineTable.listFloat.Count ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }

            value = lineTable.listFloat[nFloatIdx];
            return true;
        }

        public bool GetString(out string value, int id, int index, string columnName, bool errorCheck) {
            value = "";

            if( index >= columnType_.Count || columnType_[index] != Type.STRING ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }

            SLineTable lineTable;
            if( hashLine_.TryGetValue(id, out lineTable) == false ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }

            if( index >= (int)lineTable.listIndex.Count ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }

            int stringIdx = lineTable.listIndex[index];
            if( stringIdx >= (int)lineTable.listString.Count ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }

            value = lineTable.listString[stringIdx];
            return true;
        }

        public bool GetWString(out string value, int id, int index, string columnName, bool errorCheck) {
            value = "";

            if( index >= columnType_.Count || columnType_[index] != Type.WSTRING ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }

            SLineTable lineTable;
            if( hashLine_.TryGetValue(id, out lineTable) == false ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }

            if( index >= (int)lineTable.listIndex.Count ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }

            int stringIdx = lineTable.listIndex[index];
            if( stringIdx >= (int)lineTable.listWString.Count ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }

            value = lineTable.listWString[stringIdx];
            return true;
        }

        public bool GetInt64(out Int64 value, int id, int index, string columnName, bool errorCheck) {
            value = 0;

            if( index >= columnType_.Count || columnType_[index] != Type.INT64 ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }

            SLineTable lineTable;
            if( hashLine_.TryGetValue(id, out lineTable) == false ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }

            if( index >= lineTable.listIndex.Count ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }

            int stringIdx = lineTable.listIndex[index];
            if( stringIdx >= lineTable.listInt64.Count ) {
                ErrorMessageBox(id, columnName, errorCheck);
                return false;
            }

            value = lineTable.listInt64[stringIdx];
            return true;
        }

        public bool GetInt(out int value, int id, string columnName, bool errorCheck) {
            int index = GetIndex(columnName);
            return GetInt(out value, id, index, columnName, errorCheck);
        }

        public bool GetFloat(out float value, int id, string columnName, bool errorCheck) {
            int index = GetIndex(columnName);
            return GetFloat(out value, id, index, columnName, errorCheck);
        }

        public bool GetString(out string value, int id, string columnName, bool errorCheck) {
            int index = GetIndex(columnName);
            return GetString(out value, id, index, columnName, errorCheck);
        }

        public bool GetWString(out string value, int id, string columnName, bool errorCheck) {
            int index = GetIndex(columnName);
            return GetWString(out value, id, index, columnName, errorCheck);
        }

        public bool GetCharacterType(out CharacterType type, int id, string columnName, bool errorCheck) {
            string value_ = "";
            type = CharacterType.NONE;

            if( GetString(out value_, id, GetIndex(columnName), columnName, errorCheck) == false )
                return false;

            switch( value_ ) {
                case "POWER":
                    type = CharacterType.POWER;
                    break;

                case "MAGIC":
                    type = CharacterType.MAGIC;
                    break;

                case "SPEED":
                    type = CharacterType.SPEED;
                    break;

                default:
                    return false;
            }

            return true;
        }

        public bool GetStringSplit(out string[] value, int id, string columnName, bool errorCheck, params char[] separator) {
            int index = GetIndex(columnName);
            string temp;
            bool result = GetString(out temp, id, index, columnName, errorCheck);
            value = temp.Split(separator);
            return result;
        }

        public bool GetIntSplit(out int[] value, int id, string columnName, bool errorCheck, params char[] separator) {
            int index = GetIndex(columnName);
            string temp;
            bool result = GetString(out temp, id, index, columnName, errorCheck);
            if( temp == "" ) {
                value = null;
                return true;
            }
            string[] valueTemp = temp.Split(separator);
            value = new int[valueTemp.Length];
            for( int i = 0; i < valueTemp.Length; ++i ) {
                value[i] = Convert.ToInt32(valueTemp[i]);
            }
            return result;
        }

        public bool GetFloatSplit(out float[] value, int id, string columnName, bool errorCheck, params char[] separator) {
            int index = GetIndex(columnName);
            string temp;
            bool result = GetString(out temp, id, index, columnName, errorCheck);
            if( temp == "" ) {
                value = null;
                return true;
            }
            string[] valueTemp = temp.Split(separator);
            value = new float[valueTemp.Length];
            for( int i = 0; i < valueTemp.Length; ++i ) {
                try {
                    value[i] = Convert.ToSingle(valueTemp[i]);
                } catch {
                    MagiDebug.LogError(string.Format("GetFloatSplit Error - {0}, {1}", id, columnName));
                }
            }
            return result;
        }

        public bool GetInt64Split(out Int64[] value, int id, string columnName, bool errorCheck, params char[] separator) {
            int index = GetIndex(columnName);
            string temp;
            bool result = GetString(out temp, id, index, columnName, errorCheck);
            if( temp == "" ) {
                value = null;
                return true;
            }
            string[] valueTemp = temp.Split(separator);
            value = new Int64[valueTemp.Length];
            for( int i = 0; i < valueTemp.Length; ++i ) {
                value[i] = Convert.ToInt64(valueTemp[i]);
            }
            return result;
        }

        public bool GetInt64(out Int64 value, int id, string columnName, bool errorCheck) {
            int index = GetIndex(columnName);
            return GetInt64(out value, id, index, columnName, errorCheck);
        }

        void GetColumnInfo(out List<string> listColumnName, out List<int> listColumnType) {
            listColumnName = new List<string>();
            listColumnType = new List<int>();

            listColumnName.Clear();
            foreach( var key in columnName_.Keys ) {
                listColumnName.Add(key);
            }
        }

        public bool GetVectorSplit(out Vector3 vector, int id, string columnName, bool errorCheck, params char[] separator) {
            int index = GetIndex(columnName);
            string temp;
            bool result = GetString(out temp, id, index, columnName, errorCheck);

            vector = Vector3.zero;

            if( temp == "" ) {
                return true;
            }

            string[] valueTemp = temp.Split(separator);
            float[] value = new float[valueTemp.Length];

            if( value.Length > 3 ) {
                MagiDebug.LogError(string.Format("GetVectorSplit Error - more 3 vec."));
                return false;
            }

            for( int i = 0; i < valueTemp.Length; ++i ) {
                try {
                    value[i] = Convert.ToSingle(valueTemp[i]);
                } catch {
                    MagiDebug.LogError(string.Format("GetVectorSplit Error - {0}, {1}", id, columnName));
                }
            }

            Vector3 reVec = new Vector3(value[0], value[1], value[2]);
            vector = reVec;

            return result;
        }

        public int GetIndex(string columnName) {
            if( columnName_.ContainsKey(columnName) == false )
                return columnName_.Count;

            int value;
            if( columnName_.TryGetValue(columnName, out value) )
                return value;

            return columnName_.Count;
        }

        public bool GetItemShape(out ItemTableManager.ItemShape itemShape, int id, string columnName, bool errorCheck) {
            int type = 0;
            itemShape = ItemTableManager.ItemShape.Type_None;

            if( GetInt(out type, id, GetIndex(columnName), columnName, errorCheck) == false )
                return false;

            switch( type ) {
                case (int)ItemTableManager.ItemShape.Type_Lion:
                    itemShape = ItemTableManager.ItemShape.Type_Lion;
                    break;
                case (int)ItemTableManager.ItemShape.Type_Wolf:
                    itemShape = ItemTableManager.ItemShape.Type_Wolf;
                    break;
                case (int)ItemTableManager.ItemShape.Type_Bull:
                    itemShape = ItemTableManager.ItemShape.Type_Bull;
                    break;
                case (int)ItemTableManager.ItemShape.Type_Bear:
                    itemShape = ItemTableManager.ItemShape.Type_Bear;
                    break;
                case (int)ItemTableManager.ItemShape.Type_Deer:
                    itemShape = ItemTableManager.ItemShape.Type_Deer;
                    break;
                case (int)ItemTableManager.ItemShape.Type_Owl:
                    itemShape = ItemTableManager.ItemShape.Type_Owl;
                    break;
                case (int)ItemTableManager.ItemShape.Type_Eagle:
                    itemShape = ItemTableManager.ItemShape.Type_Eagle;
                    break;
                case (int)ItemTableManager.ItemShape.Type_Bat:
                    itemShape = ItemTableManager.ItemShape.Type_Bat;
                    break;
                case (int)ItemTableManager.ItemShape.Type_Scorpion:
                    itemShape = ItemTableManager.ItemShape.Type_Scorpion;
                    break;
                case (int)ItemTableManager.ItemShape.Type_Hydra:
                    itemShape = ItemTableManager.ItemShape.Type_Hydra;
                    break;
                case (int)ItemTableManager.ItemShape.Type_Unicon:
                    itemShape = ItemTableManager.ItemShape.Type_Unicon;
                    break;
                case (int)ItemTableManager.ItemShape.Type_Dragon:
                    itemShape = ItemTableManager.ItemShape.Type_Dragon;
                    break;
            }

            return true;
        }

        public bool GetItemType(out ItemTableManager.ItemType itemType, int id, string columnName, bool errorCheck) {
            int type = 0;
            itemType = ItemTableManager.ItemType.Type_None;

            if( GetInt(out type, id, GetIndex(columnName), columnName, errorCheck) == false )
                return false;

            switch( type - 1 ) {
                case (int)ItemTableManager.ItemType.Type_Light:
                    itemType = ItemTableManager.ItemType.Type_Light;
                    break;
                case (int)ItemTableManager.ItemType.Type_Fire:
                    itemType = ItemTableManager.ItemType.Type_Fire;
                    break;
                case (int)ItemTableManager.ItemType.Type_Wind:
                    itemType = ItemTableManager.ItemType.Type_Wind;
                    break;
                case (int)ItemTableManager.ItemType.Type_Dark:
                    itemType = ItemTableManager.ItemType.Type_Dark;
                    break;
                case (int)ItemTableManager.ItemType.Type_Earth:
                    itemType = ItemTableManager.ItemType.Type_Earth;
                    break;
                case (int)ItemTableManager.ItemType.Type_Water:
                    itemType = ItemTableManager.ItemType.Type_Water;
                    break;
            }

            return true;
        }

        public bool GetItemOption(out ItemOptionType itemOption, int id, string columnName, bool errorCheck) {
            int type = 0;
            itemOption = ItemOptionType.Type_None;

            if( GetInt(out type, id, GetIndex(columnName), columnName, errorCheck) == false )
                return false;

            switch( type ) {
                case (int)ItemOptionType.Type_Hp_Value:
                    itemOption = ItemOptionType.Type_Hp_Value;
                    break;
                case (int)ItemOptionType.Type_Physical_Attack_Value:
                    itemOption = ItemOptionType.Type_Physical_Attack_Value;
                    break;
                case (int)ItemOptionType.Type_Physical_Defence_Value:
                    itemOption = ItemOptionType.Type_Physical_Defence_Value;
                    break;
                case (int)ItemOptionType.Type_Magic_Attack_Value:
                    itemOption = ItemOptionType.Type_Magic_Attack_Value;
                    break;
                case (int)ItemOptionType.Type_Magic_Defence_Value:
                    itemOption = ItemOptionType.Type_Magic_Defence_Value;
                    break;
                case (int)ItemOptionType.Type_Hp_Ratio:
                    itemOption = ItemOptionType.Type_Hp_Ratio;
                    break;
                case (int)ItemOptionType.Type_Physical_Attack_Ratio:
                    itemOption = ItemOptionType.Type_Physical_Attack_Ratio;
                    break;
                case (int)ItemOptionType.Type_Magic_Attack_Ratio:
                    itemOption = ItemOptionType.Type_Magic_Attack_Ratio;
                    break;
                case (int)ItemOptionType.Type_Physical_Defence_Ratio:
                    itemOption = ItemOptionType.Type_Physical_Defence_Ratio;
                    break;
                case (int)ItemOptionType.Type_Magic_Defence_Ratio:
                    itemOption = ItemOptionType.Type_Magic_Defence_Ratio;
                    break;
                case (int)ItemOptionType.Type_Move_Speed_Ratio:
                    itemOption = ItemOptionType.Type_Move_Speed_Ratio;
                    break;
                case (int)ItemOptionType.Type_Attack_Effect_Ratio:
                    itemOption = ItemOptionType.Type_Attack_Effect_Ratio;
                    break;
                case (int)ItemOptionType.Type_BloodSucking_Ratio:
                    itemOption = ItemOptionType.Type_BloodSucking_Ratio;
                    break;
                case (int)ItemOptionType.Type_Heal_Ratio:
                    itemOption = ItemOptionType.Type_Heal_Ratio;
                    break;
                case (int)ItemOptionType.Type_Buff_Ratio:
                    itemOption = ItemOptionType.Type_Buff_Ratio;
                    break;
                case (int)ItemOptionType.Type_Debuff_Ratio:
                    itemOption = ItemOptionType.Type_Debuff_Ratio;
                    break;
                case (int)ItemOptionType.Type_SuperArmor:
                    itemOption = ItemOptionType.Type_SuperArmor;
                    break;
                case (int)ItemOptionType.Type_Attribute_Damage_Ratio:
                    itemOption = ItemOptionType.Type_Attribute_Damage_Ratio;
                    break;
                case (int)ItemOptionType.Type_Critical_Ratio:
                    itemOption = ItemOptionType.Type_Critical_Ratio;
                    break;
                case (int)ItemOptionType.Type_Avoid_Ratio:
                    itemOption = ItemOptionType.Type_Avoid_Ratio;
                    break;

            }

            return true;
        }

        public void ErrorMessageBox(int id, string columnName, bool errorCheck) {
            if( errorCheck == false )
                return;
            MagiDebug.LogError(string.Format("Table = {0}, ID = {1}, Column = {2}", name_, id, columnName));
        }

        public Dictionary<int, SLineTable> Line {
            get {
                return hashLine_;
            }
        }

        public string Name {
            get {
                return name_;
            }
        }

        public Dictionary<string, int> ColumnName {
            get {
                return columnName_;
            }
        }

        public List<Type> ColumnType {
            get {
                return columnType_;
            }
        }

        string name_ = "";
        Dictionary<string, int> columnName_ = new Dictionary<string, int>();
        //List<int> hashTable_ = new List<int>();
        List<Type> columnType_ = new List<Type>();
        Dictionary<int, SLineTable> hashLine_ = new Dictionary<int, SLineTable>();

        ~Table() {
            columnName_ = null;
            columnType_ = null;
            hashLine_ = null;
        }
    };
};
